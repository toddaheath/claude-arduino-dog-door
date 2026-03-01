#include <Arduino.h>
#include <Wire.h>
#include <esp_task_wdt.h>
#include "config.h"
#include "power_manager.h"
#include "imu_manager.h"
#include "gps_tracker.h"
#include "ble_collar.h"
#include "wifi_uploader.h"
#include "geofence.h"
#include "buzzer.h"
#include "storage.h"

// ── State Machine ───────────────────────────────────────────
typedef enum {
    STATE_BOOT,
    STATE_DEEP_SLEEP,
    STATE_WAKE_CLASSIFY,
    STATE_GPS_TRACKING,
    STATE_NFC_READY,
    STATE_WIFI_UPLOAD,
    STATE_OTA_UPDATE,
    STATE_LOW_POWER
} CollarState;

static CollarState state = STATE_BOOT;
static uint32_t last_motion_time = 0;
static uint32_t last_upload_time = 0;
static uint32_t last_gps_fix_time = 0;
static bool ble_door_nearby = false;

// ── Deep Sleep ──────────────────────────────────────────────
void enter_deep_sleep() {
    Serial.println("[SLEEP] Entering deep sleep (IMU wakeup enabled)");
    Serial.flush();

    gps_power_off();
    ble_stop_advertising();

    imu_configure_wakeup(IMU_MOTION_THRESHOLD);
    esp_sleep_enable_ext0_wakeup((gpio_num_t)PIN_IMU_IRQ, 1);

    esp_deep_sleep_start();
    // MCU halted — will restart from setup() on wake
}

// ── Fence Breach Handler ────────────────────────────────────
static void handle_fence_result(const FenceEvaluation* eval, const GpsFix* fix) {
    static uint32_t last_warning_time = 0;
    static uint32_t breach_start_time = 0;
    uint32_t now = millis();

    if (eval->in_breach) {
        if (breach_start_time == 0) breach_start_time = now;
        uint32_t breach_duration = now - breach_start_time;

        // Escalating buzzer response
        if (breach_duration > BREACH_ESCALATE_3_MS) {
            buzzer_play(BUZZ_CONTINUOUS);
        } else if (breach_duration > BREACH_ESCALATE_1_MS) {
            if (!buzzer_is_playing()) buzzer_play(BUZZ_LONG);
        } else {
            if (!buzzer_is_playing()) buzzer_play(BUZZ_SHORT);
        }

        // Queue event for upload
        FenceEventRecord event;
        event.fence_id = eval->breached_fence_id;
        event.type = 2;  // breach
        event.lat = fix->lat;
        event.lng = fix->lng;
        event.timestamp = fix->timestamp;
        storage_queue_fence_event(event);

        Serial.printf("[FENCE] BREACH! fence=%d dist=%.1fm\n",
                      eval->breached_fence_id, eval->distance_to_nearest_edge);
    } else {
        breach_start_time = 0;

        if (eval->in_warning && (now - last_warning_time > WARNING_INTERVAL_MS)) {
            buzzer_play(BUZZ_SHORT);
            last_warning_time = now;
            Serial.printf("[FENCE] Warning: %.1fm from boundary\n",
                          eval->distance_to_nearest_edge);
        }
    }
}

// ── Setup ───────────────────────────────────────────────────
void setup() {
    Serial.begin(115200);
    Serial.printf("\n[BOOT] Smart Dog Door Collar v%s\n", COLLAR_FW_VERSION);

    // Initialize watchdog (30s timeout)
    esp_task_wdt_init(WDT_TIMEOUT_S, true);
    esp_task_wdt_add(NULL);

    // Initialize I2C bus
    Wire.begin(PIN_SDA, PIN_SCL, 400000);

    // GPIO setup
    pinMode(PIN_GPS_EN, OUTPUT);
    digitalWrite(PIN_GPS_EN, LOW);

    // Initialize subsystems
    power_init();
    imu_init();
    buzzer_init();
    storage_init();
    ble_init();

    // Load persisted config
    storage_load_geofences();
    storage_load_wifi_creds();
    storage_load_collar_identity();

    // Check wake reason
    esp_sleep_wakeup_cause_t wake_reason = esp_sleep_get_wakeup_cause();
    if (wake_reason == ESP_SLEEP_WAKEUP_EXT0) {
        Serial.println("[BOOT] Woke from IMU motion interrupt");
        state = STATE_WAKE_CLASSIFY;
    } else {
        Serial.println("[BOOT] Cold boot or manual reset");
        state = STATE_GPS_TRACKING;
    }

    // Start BLE advertising (low power, always on)
    ble_start_advertising();

    Serial.printf("[BOOT] Battery: %.0f%% (%.2fV)\n",
                  power_get_percentage(), power_get_voltage());
}

// ── Main Loop ───────────────────────────────────────────────
void loop() {
    uint32_t now = millis();

    // Feed watchdog
    esp_task_wdt_reset();

    // Always: process BLE events
    ble_process();

    // Always: update buzzer (async pattern playback)
    buzzer_update();

    // Always: check door proximity via BLE
    ble_door_nearby = ble_check_door_rssi() > -60;

    switch (state) {
        case STATE_WAKE_CLASSIFY: {
            MotionType motion = imu_classify_motion(IMU_MOTION_DURATION_MS);

            if (motion == MOTION_NONE || motion == MOTION_BRIEF) {
                Serial.println("[STATE] Brief motion, returning to sleep");
                enter_deep_sleep();
                return;
            }

            Serial.printf("[STATE] Motion classified: %s -> starting GPS\n",
                          motion_type_name(motion));
            gps_power_on();
            state = STATE_GPS_TRACKING;
            last_motion_time = now;
            break;
        }

        case STATE_GPS_TRACKING: {
            GpsFix fix;
            bool new_fix = gps_get_fix(&fix);

            if (new_fix) {
                last_gps_fix_time = now;

                storage_buffer_location(fix);

                if (gps_quality_ok(&fix)) {
                    FenceEvaluation fence_eval = evaluate_fences(fix.lat, fix.lng);
                    handle_fence_result(&fence_eval, &fix);
                }

                ble_update_location(fix);
                ble_update_activity(imu_get_activity());
            }

            // Check for door proximity -> activate NFC (Phase 2)
            // NFC activation will be added in Phase 2

            // Periodic WiFi upload
            if (now - last_upload_time > WIFI_UPLOAD_INTERVAL_MS &&
                storage_get_buffered_count() > 0) {
                state = STATE_WIFI_UPLOAD;
                break;
            }

            // Idle timeout -> sleep
            if (imu_is_stationary() && (now - last_motion_time > SLEEP_TIMEOUT_MS)) {
                Serial.println("[STATE] Idle timeout -> deep sleep");
                gps_power_off();
                enter_deep_sleep();
                return;
            }

            // Adaptive GPS rate based on motion
            if (imu_is_stationary()) {
                gps_set_rate(GPS_FIX_RATE_STATIC_HZ);
            } else {
                gps_set_rate(GPS_FIX_RATE_MOVING_HZ);
                last_motion_time = now;
            }

            // Update battery via BLE
            ble_update_battery(power_get_percentage(), power_is_charging());
            break;
        }

        case STATE_WIFI_UPLOAD: {
            Serial.printf("[WIFI] Uploading %d buffered points\n",
                          storage_get_buffered_count());

            bool connected = wifi_connect(WIFI_CONNECT_TIMEOUT_MS);
            if (connected) {
                int uploaded = wifi_upload_locations();
                Serial.printf("[WIFI] Uploaded %d points\n", uploaded);

                wifi_upload_geofence_events();
                wifi_sync_geofences();

                if (wifi_check_ota_available()) {
                    state = STATE_OTA_UPDATE;
                    break;
                }

                wifi_disconnect();
                last_upload_time = now;
            } else {
                Serial.println("[WIFI] Connection failed, will retry next interval");
            }

            state = STATE_GPS_TRACKING;
            break;
        }

        case STATE_OTA_UPDATE: {
            Serial.println("[OTA] Firmware update available, downloading...");
            buzzer_play(BUZZ_SHORT);

            bool success = wifi_perform_ota();
            if (success) {
                Serial.println("[OTA] Update complete, rebooting...");
                ESP.restart();
            } else {
                Serial.println("[OTA] Update failed, continuing normal operation");
                wifi_disconnect();
                state = STATE_GPS_TRACKING;
            }
            break;
        }

        case STATE_LOW_POWER: {
            static uint32_t last_low_power_check = 0;
            if (now - last_low_power_check > 60000) {
                float pct = power_get_percentage();
                if (pct > BATTERY_CRITICAL_PCT + 5) {
                    state = STATE_GPS_TRACKING;
                    gps_power_on();
                    break;
                }
                if (pct < 2.0f) {
                    Serial.println("[POWER] Critical shutdown");
                    ble_stop_advertising();
                    enter_deep_sleep();
                    return;
                }
                last_low_power_check = now;
            }
            break;
        }

        default:
            state = STATE_GPS_TRACKING;
            break;
    }

    // Battery check (all states)
    if (state != STATE_LOW_POWER && power_get_percentage() < BATTERY_CRITICAL_PCT) {
        Serial.println("[POWER] Battery critical -> low power mode");
        gps_power_off();
        state = STATE_LOW_POWER;
    }

    delay(10);  // Small yield for RTOS
}
