#include <Arduino.h>
#include "config.h"
#include "sensors.h"
#include "camera.h"
#include "detection.h"
#include "door_control.h"
#include "wifi_manager.h"
#include "api_client.h"

static unsigned long last_detection_time = 0;
static unsigned long door_open_time = 0;
static bool waiting_for_close = false;

void setup() {
    Serial.begin(115200);
    Serial.println("\n=== Smart Dog Door ===");
    Serial.println("Initializing...");

    // Initialize subsystems
    sensors_init();
    Serial.println("[OK] Sensors initialized");

    door_init();
    Serial.println("[OK] Door control initialized");

    if (!camera_init()) {
        Serial.println("[FAIL] Camera initialization failed!");
        // Continue without camera - door will stay closed
    } else {
        Serial.println("[OK] Camera initialized");
    }

    if (!detection_init()) {
        Serial.println("[WARN] TFLite detection init failed - will use API-only mode");
    } else {
        Serial.println("[OK] TFLite detection initialized");
    }

    if (!wifi_connect()) {
        Serial.println("[WARN] WiFi connection failed - will retry in loop");
    } else {
        Serial.println("[OK] WiFi connected: " + wifi_get_ip());
    }

    led_off();
    Serial.println("=== Ready ===\n");
}

void loop() {
    // Ensure WiFi connectivity
    wifi_ensure_connected();

    // Handle auto-close timing
    if (waiting_for_close && door_is_open()) {
        if (millis() - door_open_time > DOOR_AUTO_CLOSE_DELAY_MS) {
            if (!ir_beam_broken()) {
                if (door_close()) {
                    waiting_for_close = false;
                    Serial.println("Door auto-closed");
                }
            } else {
                // Reset timer - animal still in doorway
                door_open_time = millis();
            }
        }
        return;  // Don't process new detections while door is open
    }

    // Stage 1: Check radar for motion
    if (!radar_detected()) {
        delay(100);  // No motion - sleep briefly
        return;
    }

    // Stage 2: Confirm proximity with ultrasonic
    float distance = ultrasonic_distance_cm();
    if (distance < 0 || distance > ULTRASONIC_TRIGGER_DISTANCE_CM) {
        return;  // Motion detected but nothing close enough
    }

    // Cooldown check
    if (millis() - last_detection_time < DETECTION_COOLDOWN_MS) {
        return;
    }

    Serial.printf("Animal detected at %.1f cm\n", distance);
    led_processing();

    // Stage 3: Capture camera image
    camera_fb_t* fb = camera_capture();
    if (!fb) {
        Serial.println("Camera capture failed");
        led_deny();
        delay(1000);
        led_off();
        return;
    }

    // Stage 4: On-device dog detection
    float dog_score = detection_run(fb);
    if (dog_score >= 0 && dog_score < DETECTION_CONFIDENCE_THRESHOLD) {
        Serial.printf("Not a dog (score: %.3f)\n", dog_score);
        camera_release(fb);
        led_deny();
        delay(1000);
        led_off();
        last_detection_time = millis();
        return;
    }

    // Stage 5: Send to API for dog identification
    if (!wifi_is_connected()) {
        Serial.println("WiFi not connected - cannot identify dog");
        camera_release(fb);
        led_deny();
        delay(1000);
        led_off();
        return;
    }

    AccessResponse response = api_request_access(fb, THIS_SIDE);
    camera_release(fb);
    last_detection_time = millis();

    if (!response.success) {
        Serial.println("API request failed: " + response.reason);
        led_deny();
        delay(2000);
        led_off();
        return;
    }

    // Stage 6: Open or deny
    if (response.allowed) {
        Serial.printf("Access GRANTED for %s (confidence: %.2f, direction: %s)\n",
                      response.animalName.c_str(), response.confidenceScore,
                      response.direction.c_str());
        if (door_open()) {
            door_open_time = millis();
            waiting_for_close = true;
        }
    } else {
        Serial.printf("Access DENIED: %s (direction: %s)\n",
                      response.reason.c_str(), response.direction.c_str());
        led_deny();
        delay(3000);
        led_off();
    }
}
