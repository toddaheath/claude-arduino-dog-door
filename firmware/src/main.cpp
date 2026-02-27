#include <Arduino.h>
#include <WiFi.h>
#include <esp_task_wdt.h>
#include <nvs_flash.h>
#include "config.h"
#include "sensors.h"
#include "camera.h"
#include "detection.h"
#include "door_control.h"
#include "wifi_manager.h"
#include "api_client.h"
#include "offline_queue.h"
#include "network_manager.h"
#include "power_monitor.h"
#include "ble_server.h"

static unsigned long last_detection_time = 0;
static unsigned long door_open_time = 0;
static bool waiting_for_close = false;

void setup() {
    Serial.begin(115200);
    Serial.println("\n=== Smart Dog Door ===");
    Serial.println("Initializing...");

    // Initialize NVS (encrypted flash storage for WiFi credentials)
    esp_err_t nvs_ret = nvs_flash_init();
    if (nvs_ret == ESP_ERR_NVS_NO_FREE_PAGES || nvs_ret == ESP_ERR_NVS_NEW_VERSION_FOUND) {
        nvs_flash_erase();
        nvs_flash_init();
    }

    // Initialize subsystems
    sensors_init();
    Serial.println("[OK] Sensors initialized");

    door_init();
    Serial.println("[OK] Door control initialized");

    if (!camera_init()) {
        Serial.println("[FAIL] Camera initialization failed!");
    } else {
        Serial.println("[OK] Camera initialized");
    }

    if (!detection_init()) {
        Serial.println("[WARN] TFLite detection init failed - will use API-only mode");
    } else {
        Serial.println("[OK] TFLite detection initialized");
    }

    // Init LittleFS and offline queue before WiFi (BLE provisioning needs it)
    offline_queue_init();

    // Start BLE early so the user can provision WiFi credentials before connecting
    ble_server_init();

    if (!wifi_connect()) {
        Serial.println("[WARN] WiFi connection failed - will retry in loop");
    } else {
        Serial.println("[OK] WiFi connected: " + wifi_get_ip());
    }

    network_manager_init();
    power_monitor_init();

    if (network_manager_is_connected() && offline_queue_size() > 0) {
        int flushed = offline_queue_flush(API_BASE_URL, API_FIRMWARE_EVENT_ENDPOINT);
        Serial.printf("[OK] Flushed %d queued events\n", flushed);
    }

    led_off();
    Serial.println("=== Ready ===\n");

    // Hardware watchdog: auto-reboot if loop() stalls for >30s
    esp_task_wdt_init(30, true);  // 30s timeout, panic on timeout
    esp_task_wdt_add(NULL);       // Add current task (loopTask)
}

void loop() {
    esp_task_wdt_reset();

    // BLE: handle commands and WiFi provisioning
    ble_server_update();

    bool openCmd;
    if (ble_server_get_command(&openCmd)) {
        if (openCmd) {
            door_open();
            api_post_firmware_event(API_KEY, "DoorOpened", nullptr, -1);
        } else {
            door_close();
            api_post_firmware_event(API_KEY, "DoorClosed", nullptr, -1);
        }
    }

    char newSsid[64], newPass[64];
    if (ble_server_get_wifi_update(newSsid, newPass, 64)) {
        Serial.printf("[BLE] New WiFi credentials: %s\n", newSsid);
        WiFi.disconnect();
        wifi_connect();
    }

    // Monitor power/battery state
    power_monitor_update(API_KEY, API_BASE_URL);

    // Ensure network connectivity
    network_manager_ensure_connected();

    // Flush queued events when network is available
    if (network_manager_is_connected() && offline_queue_size() > 0) {
        offline_queue_flush(API_BASE_URL, API_FIRMWARE_EVENT_ENDPOINT);
    }

    // Update BLE status characteristic
    ble_server_set_status(
        door_is_open(),
        "active",
        network_manager_get_transport() == NetworkTransport::WiFi,
        power_monitor_battery_percent());

    // Handle auto-close timing
    if (waiting_for_close && door_is_open()) {
        if (millis() - door_open_time > DOOR_AUTO_CLOSE_DELAY_MS) {
            if (!ir_beam_broken()) {
                if (door_close()) {
                    waiting_for_close = false;
                    Serial.println("Door auto-closed");
                    api_post_firmware_event(API_KEY, "DoorClosed", nullptr, -1);
                }
            } else {
                door_open_time = millis();
            }
        }
        return;
    }

    // Stage 1: Check radar for motion
    if (!radar_detected()) {
        delay(100);
        return;
    }

    // Stage 2: Confirm proximity with ultrasonic
    float distance = ultrasonic_distance_cm();
    if (distance < 0 || distance > ULTRASONIC_TRIGGER_DISTANCE_CM) {
        return;
    }

    // Cooldown check
    if (millis() - last_detection_time < DETECTION_COOLDOWN_MS) {
        return;
    }

    Serial.printf("Animal detected at %.1f cm\n", distance);
    led_processing();

    // Stage 3: Capture camera image and upload approach photo for all detections
    camera_fb_t* fb = camera_capture();
    if (!fb) {
        Serial.println("Camera capture failed");
        led_deny();
        delay(1000);
        led_off();
        return;
    }

    // Upload approach photo regardless of TFLite outcome so every detection
    // is visible in the admin portal log with its captured image.
    api_post_approach_photo(fb, THIS_SIDE);

    // Release framebuffer after upload to free ~100KB PSRAM during detection
    camera_release(fb);

    // Recapture fresh frame for detection and identification
    fb = camera_capture();
    if (!fb) {
        Serial.println("Camera recapture failed");
        led_deny();
        delay(1000);
        led_off();
        last_detection_time = millis();
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
    AccessResponse response = api_request_access_direct(fb, THIS_SIDE);
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
            api_post_firmware_event(API_KEY, "DoorOpened", nullptr, -1);
        } else {
            api_post_firmware_event(API_KEY, "DoorObstructed", "open", -1);
        }
    } else {
        Serial.printf("Access DENIED: %s (direction: %s)\n",
                      response.reason.c_str(), response.direction.c_str());
        led_deny();
        delay(3000);
        led_off();
    }
}
