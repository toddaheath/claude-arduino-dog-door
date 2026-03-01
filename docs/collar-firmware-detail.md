# Collar Firmware — Detailed Implementation Guide

## Overview

Complete firmware specification for the ESP32-S3-MINI-1 collar device, covering the state machine, GPS management, IMU-based wake/sleep, BLE communication, WiFi data upload, power management, and OTA updates.

---

## 1. PlatformIO Configuration

```ini
; firmware/collar/platformio.ini

[env:collar]
platform = espressif32
board = esp32-s3-devkitc-1
framework = arduino
board_build.mcu = esp32s3
board_build.f_cpu = 240000000L
board_build.f_flash = 80000000L
board_build.flash_mode = qio
board_build.flash_size = 8MB
board_build.partitions = default_8MB.csv

; USB native for programming (no FTDI needed)
board_build.arduino.cdc_on_boot = "1"

monitor_speed = 115200

lib_deps =
    bblanchon/ArduinoJson@^7.0.0
    adafruit/Adafruit PN532@^1.3.3
    sparkfun/SparkFun u-blox GNSS v3@^3.1.1
    stm32duino/STM32duino LSM6DSO@^2.2.0
    h2zero/NimBLE-Arduino@^1.4.1
    ; mbedtls included in ESP32 SDK

build_flags =
    -DCONFIG_BT_ENABLED
    -DCONFIG_NIMBLE_ENABLED
    -DBOARD_HAS_PSRAM
    -DARDUINO_USB_MODE=1
    -DARDUINO_USB_CDC_ON_BOOT=1
    -DCOLLAR_FW_VERSION="1.0.0"

; Enable flash encryption for NVS security
; (set via espefuse on first flash)
```

---

## 2. Configuration Constants

```c
// firmware/collar/src/config.h
#pragma once

// ── Pin Assignments ──────────────────────────────────────────
#define PIN_SDA          1    // I2C SDA (PN532 + LSM6DSO)
#define PIN_SCL          2    // I2C SCL
#define PIN_GPS_TX       3    // UART1 TX → GPS RX
#define PIN_GPS_RX       4    // UART1 RX ← GPS TX
#define PIN_NFC_IRQ      5    // PN532 interrupt
#define PIN_IMU_IRQ      6    // LSM6DSO interrupt (motion wakeup)
#define PIN_BUZZER       7    // Piezo buzzer (PWM)
#define PIN_LED          8    // WS2812B RGB LED data
#define PIN_BATTERY_ADC  9    // Battery voltage divider
#define PIN_GPS_EN       10   // GPS power gate (MOSFET)
#define PIN_NFC_RST      11   // PN532 reset

// ── I2C Addresses ────────────────────────────────────────────
#define I2C_ADDR_PN532   0x24
#define I2C_ADDR_LSM6DSO 0x6A

// ── BLE Configuration ────────────────────────────────────────
#define BLE_DEVICE_NAME_PREFIX  "SDD-Collar-"
#define BLE_SERVICE_UUID        "5a6d0001-8e7f-4b3c-9d2a-1c6e3f8b7d4e"
#define BLE_CHAR_LOCATION_UUID  "5a6d0002-8e7f-4b3c-9d2a-1c6e3f8b7d4e"
#define BLE_CHAR_GEOFENCE_UUID  "5a6d0003-8e7f-4b3c-9d2a-1c6e3f8b7d4e"
#define BLE_CHAR_BATTERY_UUID   "5a6d0004-8e7f-4b3c-9d2a-1c6e3f8b7d4e"
#define BLE_CHAR_ACTIVITY_UUID  "5a6d0005-8e7f-4b3c-9d2a-1c6e3f8b7d4e"
#define BLE_CHAR_COMMAND_UUID   "5a6d0006-8e7f-4b3c-9d2a-1c6e3f8b7d4e"
#define BLE_CHAR_CONFIG_UUID    "5a6d0007-8e7f-4b3c-9d2a-1c6e3f8b7d4e"

// ── GPS Configuration ────────────────────────────────────────
#define GPS_BAUD_RATE    9600
#define GPS_FIX_RATE_MOVING_HZ    1     // 1 Hz when moving
#define GPS_FIX_RATE_STATIC_HZ    10    // 0.1 Hz when stationary (1 fix/10s)
#define GPS_COLD_START_TIMEOUT_MS 120000 // 2 min for cold start
#define GPS_HDOP_THRESHOLD        5.0   // Suppress geofence if HDOP > this

// ── Power Management ─────────────────────────────────────────
#define BATTERY_VOLTAGE_DIVIDER   2.0    // 100k/100k divider ratio
#define BATTERY_FULL_MV           4200   // Fully charged LiPo
#define BATTERY_EMPTY_MV          3200   // Empty (protect cutoff)
#define BATTERY_LOW_PCT           15     // Low battery warning
#define BATTERY_CRITICAL_PCT      5      // Critical warning
#define SLEEP_TIMEOUT_MS          300000 // 5 min idle → deep sleep
#define IMU_MOTION_THRESHOLD      200    // LSM6DSO wake threshold (mg)
#define IMU_MOTION_DURATION_MS    2000   // Sustained motion to confirm wake

// ── WiFi Upload ──────────────────────────────────────────────
#define WIFI_UPLOAD_INTERVAL_MS   30000  // Upload batch every 30s
#define WIFI_CONNECT_TIMEOUT_MS   10000  // Max 10s to connect
#define WIFI_MAX_BATCH_SIZE       100    // Max GPS points per upload
#define API_BASE_URL              "https://your-api.example.com/api/v1"

// ── Geofence ─────────────────────────────────────────────────
#define GEOFENCE_MAX_FENCES       20
#define GEOFENCE_HYSTERESIS_M     3.0
#define GEOFENCE_WARNING_DIST_M   5.0
#define GEOFENCE_BREACH_BUZZ_INTERVAL_MS 10000

// ── NFC ──────────────────────────────────────────────────────
#define NFC_HANDSHAKE_TIMEOUT_MS  500
#define NFC_AUTH_REPLAY_WINDOW_S  30
#define NFC_SHARED_SECRET_LEN     32
#define NFC_COLLAR_ID_LEN         16
```

---

## 3. Main State Machine

```c
// firmware/collar/src/main.cpp

#include <Arduino.h>
#include "config.h"
#include "power_manager.h"
#include "imu_manager.h"
#include "gps_tracker.h"
#include "ble_collar.h"
#include "wifi_uploader.h"
#include "nfc_manager.h"
#include "geofence.h"
#include "buzzer.h"
#include "storage.h"

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

void setup() {
    Serial.begin(115200);
    Serial.printf("[BOOT] Smart Dog Door Collar v%s\n", COLLAR_FW_VERSION);

    // Initialize I2C
    Wire.begin(PIN_SDA, PIN_SCL, 400000);

    // Initialize subsystems
    power_init();
    imu_init();
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
        state = STATE_GPS_TRACKING;  // Start tracking immediately
    }

    // Start BLE advertising (low power, always on)
    ble_start_advertising();

    Serial.printf("[BOOT] Battery: %.0f%% (%.2fV)\n",
                  power_get_percentage(), power_get_voltage());
}

void loop() {
    uint32_t now = millis();

    // Always: check BLE for incoming commands
    ble_process();

    // Always: check for door proximity via BLE scan
    ble_door_nearby = ble_check_door_rssi() > -60;  // RSSI threshold

    switch (state) {
        case STATE_WAKE_CLASSIFY: {
            // IMU woke us — classify the motion
            MotionType motion = imu_classify_motion(IMU_MOTION_DURATION_MS);

            if (motion == MOTION_NONE || motion == MOTION_BRIEF) {
                // False alarm or brief jostle → go back to sleep
                Serial.println("[STATE] Brief motion, returning to sleep");
                enter_deep_sleep();
                return;
            }

            // Sustained motion → dog is active, start GPS
            Serial.printf("[STATE] Motion classified: %s → starting GPS\n",
                          motion_type_name(motion));
            gps_power_on();
            state = STATE_GPS_TRACKING;
            last_motion_time = now;
            break;
        }

        case STATE_GPS_TRACKING: {
            // Update GPS
            GpsFix fix;
            bool new_fix = gps_get_fix(&fix);

            if (new_fix) {
                last_gps_fix_time = now;

                // Buffer the point for later upload
                storage_buffer_location(fix);

                // Evaluate geofences
                if (gps_quality_ok(&fix)) {
                    FenceEvaluation fence_eval = evaluate_fences(fix.lat, fix.lng);
                    handle_fence_result(&fence_eval, &fix);
                }

                // Update BLE characteristics
                ble_update_location(fix);
                ble_update_activity(imu_get_activity());
            }

            // Check for door proximity → activate NFC
            if (ble_door_nearby) {
                nfc_power_on();
                state = STATE_NFC_READY;
                Serial.println("[STATE] Door nearby → NFC ready");
                break;
            }

            // Periodic WiFi upload
            if (now - last_upload_time > WIFI_UPLOAD_INTERVAL_MS &&
                storage_get_buffered_count() > 0) {
                state = STATE_WIFI_UPLOAD;
                break;
            }

            // Check for idle → sleep
            if (imu_is_stationary() && (now - last_motion_time > SLEEP_TIMEOUT_MS)) {
                Serial.println("[STATE] Idle timeout → deep sleep");
                gps_power_off();
                enter_deep_sleep();
                return;
            }

            // Adaptive GPS rate
            if (imu_is_stationary()) {
                gps_set_rate(GPS_FIX_RATE_STATIC_HZ);
            } else {
                gps_set_rate(GPS_FIX_RATE_MOVING_HZ);
                last_motion_time = now;
            }

            // Update battery BLE characteristic
            ble_update_battery(power_get_percentage(), power_is_charging());

            break;
        }

        case STATE_NFC_READY: {
            // NFC tag emulation active — waiting for door reader
            nfc_emulate_tag();  // Blocks briefly, handles handshake

            // If door no longer nearby, power down NFC
            if (!ble_door_nearby) {
                nfc_power_off();
                state = STATE_GPS_TRACKING;
                Serial.println("[STATE] Door out of range → back to GPS tracking");
            }
            break;
        }

        case STATE_WIFI_UPLOAD: {
            Serial.printf("[WIFI] Uploading %d buffered points\n",
                          storage_get_buffered_count());

            bool connected = wifi_connect(WIFI_CONNECT_TIMEOUT_MS);
            if (connected) {
                // Upload location batch
                int uploaded = wifi_upload_locations();
                Serial.printf("[WIFI] Uploaded %d points\n", uploaded);

                // Upload any pending geofence events
                wifi_upload_geofence_events();

                // Check for fence updates
                wifi_sync_geofences();

                // Check for OTA updates
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
            buzzer_play(BUZZ_SHORT);  // Notify

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
            // Battery critically low — minimal operation
            // GPS off, WiFi off, NFC off
            // Only BLE advertising (low power) for find-my-dog
            static uint32_t last_low_power_check = 0;
            if (now - last_low_power_check > 60000) {  // Check every minute
                float pct = power_get_percentage();
                if (pct > BATTERY_CRITICAL_PCT + 5) {
                    // Battery recovered (maybe charging)
                    state = STATE_GPS_TRACKING;
                    gps_power_on();
                    break;
                }
                if (pct < 2.0) {
                    // Critically low — shut down to protect battery
                    Serial.println("[POWER] Critical shutdown");
                    ble_stop_advertising();
                    enter_deep_sleep();  // Will only wake on USB power
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
        Serial.println("[POWER] Battery critical → low power mode");
        gps_power_off();
        nfc_power_off();
        state = STATE_LOW_POWER;
    }

    delay(10);  // Small yield for RTOS
}

void enter_deep_sleep() {
    Serial.println("[SLEEP] Entering deep sleep (IMU wakeup enabled)");
    Serial.flush();

    gps_power_off();
    nfc_power_off();
    ble_stop_advertising();

    // Configure IMU as wakeup source
    imu_configure_wakeup(IMU_MOTION_THRESHOLD);

    // Configure wakeup on IMU interrupt pin
    esp_sleep_enable_ext0_wakeup((gpio_num_t)PIN_IMU_IRQ, 1);  // Wake on HIGH

    esp_deep_sleep_start();
    // MCU halted — will restart from setup() on wake
}
```

---

## 4. GPS Tracker Module

```c
// firmware/collar/src/gps_tracker.h
#pragma once

#include "config.h"

typedef struct {
    double lat;
    double lng;
    float  altitude;
    float  accuracy;   // Horizontal accuracy (meters)
    float  speed;      // Ground speed (m/s)
    float  heading;    // Course over ground (degrees)
    uint8_t satellites;
    float  hdop;
    uint8_t fix_type;  // 0=none, 2=2D, 3=3D
    uint32_t age_ms;   // Time since this fix was computed
    uint32_t timestamp; // Unix epoch
} GpsFix;

void gps_power_on();
void gps_power_off();
void gps_set_rate(uint8_t rate_hz);
bool gps_get_fix(GpsFix* fix);
bool gps_quality_ok(const GpsFix* fix);
bool gps_has_fix();
```

```c
// firmware/collar/src/gps_tracker.cpp

#include "gps_tracker.h"
#include <SparkFun_u-blox_GNSS_v3.h>

static SFE_UBLOX_GNSS gnss;
static bool gps_powered = false;
static uint8_t current_rate = 1;

void gps_power_on() {
    if (gps_powered) return;

    digitalWrite(PIN_GPS_EN, HIGH);  // Enable MOSFET power gate
    delay(500);  // GPS module startup time

    Serial1.begin(GPS_BAUD_RATE, SERIAL_8N1, PIN_GPS_RX, PIN_GPS_TX);

    if (!gnss.begin(Serial1)) {
        Serial.println("[GPS] u-blox not detected, check wiring");
        return;
    }

    gnss.setUART1Output(COM_TYPE_UBX);     // UBX protocol (more efficient than NMEA)
    gnss.setNavigationFrequency(1);          // Start at 1Hz
    gnss.setAutoPVT(true);                   // Auto push position-velocity-time
    gnss.setDynamicModel(DYN_MODEL_PEDESTRIAN); // Optimized for walking speed

    // Enable SBAS for better accuracy (WAAS in US)
    gnss.setSBAS(true);

    // Power save mode: cyclic tracking (reduces current ~40%)
    gnss.powerSaveMode(true);

    gps_powered = true;
    Serial.println("[GPS] Powered on, waiting for fix...");
}

void gps_power_off() {
    if (!gps_powered) return;

    gnss.powerOff(0);  // Put module in backup mode
    digitalWrite(PIN_GPS_EN, LOW);  // Cut power via MOSFET
    Serial1.end();

    gps_powered = false;
    Serial.println("[GPS] Powered off");
}

void gps_set_rate(uint8_t rate_hz) {
    if (!gps_powered || rate_hz == current_rate) return;

    if (rate_hz == 0) {
        // Use measurement period for sub-1Hz rates
        gnss.setMeasurementRate(10000);  // 10s = 0.1 Hz
    } else {
        gnss.setNavigationFrequency(rate_hz);
    }
    current_rate = rate_hz;
}

bool gps_get_fix(GpsFix* fix) {
    if (!gps_powered) return false;

    if (!gnss.getPVT()) return false;  // No new data

    fix->fix_type = gnss.getFixType();
    if (fix->fix_type < 2) return false;  // No 2D/3D fix

    fix->lat = gnss.getLatitude() / 1e7;
    fix->lng = gnss.getLongitude() / 1e7;
    fix->altitude = gnss.getAltitudeMSL() / 1000.0;
    fix->accuracy = gnss.getHorizontalAccEst() / 1000.0;  // mm → m
    fix->speed = gnss.getGroundSpeed() / 1000.0;           // mm/s → m/s
    fix->heading = gnss.getHeading() / 1e5;                // degrees
    fix->satellites = gnss.getSIV();
    fix->hdop = gnss.getPDOP() / 100.0;  // Approximation
    fix->age_ms = 0;  // Fresh fix

    // Unix timestamp from GPS time
    struct tm t;
    t.tm_year = gnss.getYear() - 1900;
    t.tm_mon = gnss.getMonth() - 1;
    t.tm_mday = gnss.getDay();
    t.tm_hour = gnss.getHour();
    t.tm_min = gnss.getMinute();
    t.tm_sec = gnss.getSecond();
    fix->timestamp = mktime(&t);

    return true;
}

bool gps_quality_ok(const GpsFix* fix) {
    if (fix->fix_type < 2) return false;
    if (fix->hdop > GPS_HDOP_THRESHOLD) return false;
    if (fix->satellites < 4) return false;
    if (fix->age_ms > 30000) return false;
    if (fix->accuracy > 15.0) return false;
    return true;
}

bool gps_has_fix() {
    return gps_powered && gnss.getFixType() >= 2;
}
```

---

## 5. IMU Manager (LSM6DSO)

```c
// firmware/collar/src/imu_manager.h
#pragma once

typedef enum {
    MOTION_NONE,
    MOTION_BRIEF,     // < 2s, likely a jostle
    MOTION_WALKING,   // Rhythmic, moderate acceleration
    MOTION_RUNNING,   // High acceleration, fast cadence
    MOTION_UNKNOWN    // Sustained but unclassified
} MotionType;

typedef struct {
    MotionType type;
    uint32_t steps_today;
    float    distance_m_today;
    uint16_t active_minutes_today;
} ActivityData;

void imu_init();
MotionType imu_classify_motion(uint32_t sample_duration_ms);
bool imu_is_stationary();
void imu_configure_wakeup(uint16_t threshold_mg);
ActivityData imu_get_activity();
void imu_reset_daily_stats();
```

```c
// firmware/collar/src/imu_manager.cpp

#include "imu_manager.h"
#include <LSM6DSOSensor.h>

static LSM6DSOSensor imu(&Wire, I2C_ADDR_LSM6DSO);
static ActivityData activity = { MOTION_NONE, 0, 0.0, 0 };
static uint32_t last_step_time = 0;
static bool is_stationary = true;
static uint32_t stationary_since = 0;

// Step detection state
static float step_threshold = 1.2;  // g threshold for step detection
static uint32_t min_step_interval_ms = 200;  // Max ~5 steps/sec (running)
static float stride_length_m = 0.5;  // Approximate dog stride

void imu_init() {
    imu.begin();
    imu.Enable_X();  // Accelerometer
    imu.Enable_G();  // Gyroscope

    // Set ODR (Output Data Rate) for efficient power usage
    imu.Set_X_ODR(52.0f);   // 52 Hz for step detection
    imu.Set_G_ODR(52.0f);

    // Set full scale
    imu.Set_X_FS(4);   // +/- 4g (dogs can produce high-g impacts)
    imu.Set_G_FS(500);  // +/- 500 dps

    // Enable pedometer (built-in step counter)
    imu.Enable_Pedometer();

    // Reset daily stats at boot
    activity.steps_today = 0;
    activity.distance_m_today = 0.0;
    activity.active_minutes_today = 0;

    Serial.println("[IMU] LSM6DSO initialized");
}

MotionType imu_classify_motion(uint32_t sample_duration_ms) {
    uint32_t start = millis();
    float max_accel = 0;
    int sample_count = 0;
    float accel_sum = 0;

    while (millis() - start < sample_duration_ms) {
        int32_t accel[3];
        if (imu.Get_X_Axes(accel) == LSM6DSO_OK) {
            float magnitude = sqrt(accel[0]*accel[0] +
                                   accel[1]*accel[1] +
                                   accel[2]*accel[2]) / 1000.0;  // mg → g
            float dynamic = fabs(magnitude - 1.0);  // Remove gravity
            if (dynamic > max_accel) max_accel = dynamic;
            accel_sum += dynamic;
            sample_count++;
        }
        delay(20);  // ~50 Hz sampling
    }

    if (sample_count == 0) return MOTION_NONE;

    float avg_dynamic = accel_sum / sample_count;

    if (avg_dynamic < 0.05) return MOTION_NONE;
    if (avg_dynamic < 0.1) return MOTION_BRIEF;
    if (avg_dynamic < 0.5) {
        is_stationary = false;
        return MOTION_WALKING;
    }
    is_stationary = false;
    return MOTION_RUNNING;
}

bool imu_is_stationary() {
    int32_t accel[3];
    if (imu.Get_X_Axes(accel) != LSM6DSO_OK) return is_stationary;

    float magnitude = sqrt(accel[0]*accel[0] +
                           accel[1]*accel[1] +
                           accel[2]*accel[2]) / 1000.0;
    float dynamic = fabs(magnitude - 1.0);

    if (dynamic < 0.08) {
        if (!is_stationary) {
            stationary_since = millis();
            is_stationary = true;
        }
    } else {
        is_stationary = false;
        stationary_since = millis();

        // Update step count
        uint16_t steps;
        if (imu.Get_Step_Count(&steps) == LSM6DSO_OK) {
            if (steps > activity.steps_today) {
                uint32_t new_steps = steps - activity.steps_today;
                activity.steps_today = steps;
                activity.distance_m_today += new_steps * stride_length_m;
            }
        }

        // Track active minutes
        static uint32_t last_active_minute = 0;
        uint32_t current_minute = millis() / 60000;
        if (current_minute != last_active_minute) {
            activity.active_minutes_today++;
            last_active_minute = current_minute;
        }
    }

    return is_stationary;
}

void imu_configure_wakeup(uint16_t threshold_mg) {
    // Configure wake-up interrupt on INT1 pin
    // The LSM6DSO will pull INT1 high when acceleration exceeds threshold
    imu.Enable_Wake_Up_Detection(LSM6DSO_INT1_PIN);
    imu.Set_Wake_Up_Threshold(threshold_mg);
    imu.Set_Wake_Up_Duration(2);  // 2 ODR cycles must exceed threshold

    // Set low-power mode for sleep
    imu.Set_X_ODR(12.5f);  // Reduce to 12.5 Hz (lowest for wake detection)

    Serial.printf("[IMU] Wake-up configured: %dmg threshold\n", threshold_mg);
}

ActivityData imu_get_activity() {
    // Classify current motion
    int32_t accel[3];
    if (imu.Get_X_Axes(accel) == LSM6DSO_OK) {
        float magnitude = sqrt(accel[0]*accel[0] +
                               accel[1]*accel[1] +
                               accel[2]*accel[2]) / 1000.0;
        float dynamic = fabs(magnitude - 1.0);

        if (dynamic < 0.05) activity.type = MOTION_NONE;
        else if (dynamic < 0.3) activity.type = MOTION_WALKING;
        else activity.type = MOTION_RUNNING;
    }
    return activity;
}

void imu_reset_daily_stats() {
    activity.steps_today = 0;
    activity.distance_m_today = 0.0;
    activity.active_minutes_today = 0;
    imu.Reset_Step_Count();
}

const char* motion_type_name(MotionType type) {
    switch (type) {
        case MOTION_NONE:    return "none";
        case MOTION_BRIEF:   return "brief";
        case MOTION_WALKING: return "walking";
        case MOTION_RUNNING: return "running";
        default:             return "unknown";
    }
}
```

---

## 6. Power Manager

```c
// firmware/collar/src/power_manager.h
#pragma once

void power_init();
float power_get_voltage();
float power_get_percentage();
bool power_is_charging();
bool power_is_low();
bool power_is_critical();
```

```c
// firmware/collar/src/power_manager.cpp

#include "power_manager.h"
#include "config.h"

static float smoothed_voltage = 0;
static const int ADC_SAMPLES = 16;

void power_init() {
    analogReadResolution(12);  // 12-bit ADC (0-4095)
    analogSetAttenuation(ADC_11db);  // Full 0-3.3V range

    // Initial reading
    smoothed_voltage = read_raw_voltage();
    Serial.printf("[POWER] Initial voltage: %.2fV (%.0f%%)\n",
                  smoothed_voltage, power_get_percentage());
}

static float read_raw_voltage() {
    uint32_t sum = 0;
    for (int i = 0; i < ADC_SAMPLES; i++) {
        sum += analogRead(PIN_BATTERY_ADC);
        delayMicroseconds(100);
    }
    float adc_avg = (float)sum / ADC_SAMPLES;

    // Convert ADC reading to voltage
    // ADC reference: 3.3V, 12-bit resolution, voltage divider ratio
    float voltage = (adc_avg / 4095.0) * 3.3 * BATTERY_VOLTAGE_DIVIDER;
    return voltage;
}

float power_get_voltage() {
    float raw = read_raw_voltage();
    // Exponential moving average for smooth readings
    smoothed_voltage = smoothed_voltage * 0.9 + raw * 0.1;
    return smoothed_voltage;
}

float power_get_percentage() {
    float v = power_get_voltage();
    float v_mv = v * 1000;

    if (v_mv >= BATTERY_FULL_MV) return 100.0;
    if (v_mv <= BATTERY_EMPTY_MV) return 0.0;

    // LiPo discharge curve (piecewise linear approximation)
    // 4.20V=100%, 4.10V=90%, 3.95V=70%, 3.80V=40%, 3.70V=20%, 3.50V=5%, 3.20V=0%
    float pct;
    if (v_mv > 4100) pct = 90.0 + (v_mv - 4100.0) / (4200.0 - 4100.0) * 10.0;
    else if (v_mv > 3950) pct = 70.0 + (v_mv - 3950.0) / (4100.0 - 3950.0) * 20.0;
    else if (v_mv > 3800) pct = 40.0 + (v_mv - 3800.0) / (3950.0 - 3800.0) * 30.0;
    else if (v_mv > 3700) pct = 20.0 + (v_mv - 3700.0) / (3800.0 - 3700.0) * 20.0;
    else if (v_mv > 3500) pct = 5.0 + (v_mv - 3500.0) / (3700.0 - 3500.0) * 15.0;
    else pct = (v_mv - 3200.0) / (3500.0 - 3200.0) * 5.0;

    return constrain(pct, 0.0, 100.0);
}

bool power_is_charging() {
    // Detect charging by checking if voltage is rising
    // More reliable: check USB VBUS presence or TP4056 CHRG pin
    // For now, use voltage threshold heuristic
    static float prev_voltage = 0;
    float current = power_get_voltage();
    bool rising = (current - prev_voltage) > 0.01;  // Rising > 10mV
    prev_voltage = current;
    return rising && current > 4.0;  // Rising above 4.0V suggests charging
}

bool power_is_low() {
    return power_get_percentage() < BATTERY_LOW_PCT;
}

bool power_is_critical() {
    return power_get_percentage() < BATTERY_CRITICAL_PCT;
}
```

---

## 7. WiFi Uploader

```c
// firmware/collar/src/wifi_uploader.h
#pragma once

bool wifi_connect(uint32_t timeout_ms);
void wifi_disconnect();
int wifi_upload_locations();
void wifi_upload_geofence_events();
void wifi_sync_geofences();
bool wifi_check_ota_available();
bool wifi_perform_ota();
```

```c
// firmware/collar/src/wifi_uploader.cpp

#include "wifi_uploader.h"
#include "storage.h"
#include "config.h"
#include <WiFi.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>

static String wifi_ssid;
static String wifi_password;
static String api_key;
static String collar_id_hex;

bool wifi_connect(uint32_t timeout_ms) {
    if (WiFi.status() == WL_CONNECTED) return true;

    WiFi.mode(WIFI_STA);
    WiFi.begin(wifi_ssid.c_str(), wifi_password.c_str());

    uint32_t start = millis();
    while (WiFi.status() != WL_CONNECTED && millis() - start < timeout_ms) {
        delay(100);
    }

    if (WiFi.status() == WL_CONNECTED) {
        Serial.printf("[WIFI] Connected, IP: %s\n", WiFi.localIP().toString().c_str());
        return true;
    }

    Serial.println("[WIFI] Connection failed");
    WiFi.disconnect(true);
    WiFi.mode(WIFI_OFF);
    return false;
}

void wifi_disconnect() {
    WiFi.disconnect(true);
    WiFi.mode(WIFI_OFF);
    Serial.println("[WIFI] Disconnected");
}

int wifi_upload_locations() {
    int count = storage_get_buffered_count();
    if (count == 0) return 0;

    int batch_size = min(count, (int)WIFI_MAX_BATCH_SIZE);

    // Build JSON payload with compact field names
    JsonDocument doc;
    doc["apiKey"] = api_key;
    JsonArray points = doc["points"].to<JsonArray>();

    for (int i = 0; i < batch_size; i++) {
        GpsFix fix;
        if (!storage_get_buffered_point(i, &fix)) break;

        JsonObject pt = points.add<JsonObject>();
        pt["lat"] = fix.lat;
        pt["lng"] = fix.lng;
        pt["alt"] = fix.altitude;
        pt["acc"] = fix.accuracy;
        pt["spd"] = fix.speed;
        pt["hdg"] = fix.heading;
        pt["sat"] = fix.satellites;
        pt["bat"] = power_get_voltage();
        pt["ts"] = fix.timestamp;
    }

    String payload;
    serializeJson(doc, payload);

    // POST to API
    HTTPClient http;
    String url = String(API_BASE_URL) + "/collars/" + collar_id_hex + "/locations";
    http.begin(url);
    http.addHeader("Content-Type", "application/json");

    int httpCode = http.POST(payload);
    if (httpCode == 201) {
        // Success — clear uploaded points from buffer
        storage_clear_buffered_points(batch_size);
        Serial.printf("[WIFI] Uploaded %d points successfully\n", batch_size);
        http.end();
        return batch_size;
    } else {
        Serial.printf("[WIFI] Upload failed: HTTP %d\n", httpCode);
        http.end();
        return 0;
    }
}

void wifi_sync_geofences() {
    HTTPClient http;
    String url = String(API_BASE_URL) + "/geofences/sync?collarId=" +
                 collar_id_hex + "&sinceVersion=" + String(storage_get_fence_version());
    http.begin(url);

    int httpCode = http.GET();
    if (httpCode == 200) {
        String response = http.getString();
        JsonDocument doc;
        DeserializationError err = deserializeJson(doc, response);
        if (!err) {
            uint32_t version = doc["version"];
            if (version > storage_get_fence_version()) {
                Serial.printf("[FENCE] Updating fences: v%d → v%d\n",
                              storage_get_fence_version(), version);
                storage_save_geofences(response);
                geofence_reload();
            }
        }
    } else if (httpCode == 304) {
        Serial.println("[FENCE] Fences up to date");
    }
    http.end();
}

void wifi_upload_geofence_events() {
    int count = storage_get_pending_fence_events();
    if (count == 0) return;

    JsonDocument doc;
    doc["apiKey"] = api_key;
    doc["collarId"] = collar_id_hex;
    JsonArray events = doc["events"].to<JsonArray>();

    for (int i = 0; i < count; i++) {
        FenceEventRecord rec;
        if (!storage_get_fence_event(i, &rec)) break;

        JsonObject ev = events.add<JsonObject>();
        ev["fenceId"] = rec.fence_id;
        ev["type"] = fence_event_type_name(rec.type);
        ev["lat"] = rec.lat;
        ev["lng"] = rec.lng;
        ev["ts"] = rec.timestamp;
    }

    String payload;
    serializeJson(doc, payload);

    HTTPClient http;
    String url = String(API_BASE_URL) + "/geofences/events";
    http.begin(url);
    http.addHeader("Content-Type", "application/json");

    int httpCode = http.POST(payload);
    if (httpCode == 201) {
        storage_clear_fence_events(count);
        Serial.printf("[FENCE] Uploaded %d geofence events\n", count);
    }
    http.end();
}

bool wifi_check_ota_available() {
    HTTPClient http;
    String url = String(API_BASE_URL) + "/collars/" + collar_id_hex +
                 "/firmware?current=" + String(COLLAR_FW_VERSION);
    http.begin(url);

    int httpCode = http.GET();
    bool available = (httpCode == 200);
    http.end();
    return available;
}

bool wifi_perform_ota() {
    // Use ESP32 built-in OTA library
    // Download firmware binary from API and flash
    // Implementation uses httpUpdate library
    HTTPClient http;
    String url = String(API_BASE_URL) + "/collars/" + collar_id_hex + "/firmware/download";
    // ... (standard ESP32 OTA implementation)
    return false;  // Placeholder
}
```

---

## 8. Storage Manager

```c
// firmware/collar/src/storage.h
#pragma once

#include "gps_tracker.h"
#include "geofence.h"

// ── Initialization ───────────────────────────────────────────
void storage_init();

// ── Collar Identity (NVS) ────────────────────────────────────
void storage_load_collar_identity();
void storage_save_collar_identity(const char* collar_id, const uint8_t* secret);
const char* storage_get_collar_id();
const uint8_t* storage_get_shared_secret();

// ── WiFi Credentials (NVS encrypted) ────────────────────────
void storage_load_wifi_creds();
void storage_save_wifi_creds(const char* ssid, const char* password, const char* api_key);

// ── GPS Point Buffer (SPIFFS ring buffer) ────────────────────
void storage_buffer_location(const GpsFix& fix);
int storage_get_buffered_count();
bool storage_get_buffered_point(int index, GpsFix* fix);
void storage_clear_buffered_points(int count);

// ── Geofence Storage (NVS) ──────────────────────────────────
void storage_load_geofences();
void storage_save_geofences(const String& json);
uint32_t storage_get_fence_version();

// ── Geofence Event Queue (SPIFFS) ───────────────────────────
typedef struct {
    uint16_t fence_id;
    uint8_t type;  // 0=entered, 1=exited, 2=breach
    double lat;
    double lng;
    uint32_t timestamp;
} FenceEventRecord;

void storage_queue_fence_event(const FenceEventRecord& event);
int storage_get_pending_fence_events();
bool storage_get_fence_event(int index, FenceEventRecord* event);
void storage_clear_fence_events(int count);
```

**Storage allocation:**

```
NVS (Non-Volatile Storage — encrypted):
├── "collar" namespace
│   ├── "id"          → 32-char hex string (collar_id)
│   ├── "secret"      → 32-byte blob (shared_secret)
│   └── "door_id"     → 32-char hex string (paired door)
├── "wifi" namespace
│   ├── "ssid"        → string (max 32 chars)
│   ├── "password"    → string (max 64 chars)
│   └── "api_key"     → string (max 64 chars)
└── "geofence" namespace
    ├── "version"     → uint32_t
    ├── "ref_lat"     → double
    ├── "ref_lng"     → double
    ├── "count"       → uint8_t
    └── "fence_N"     → blob (compact fence data)

SPIFFS (SPI Flash File System — 1MB partition):
├── /gps_buffer.bin   → Ring buffer of GpsFix structs (~500 points @ 52 bytes each = 26KB)
├── /fence_events.bin → Queue of FenceEventRecord structs (~100 events @ 28 bytes = 2.8KB)
└── /track_log.bin    → Extended track storage for offline periods (~900KB)
```

---

## 9. BLE Collar Service (NimBLE)

```c
// firmware/collar/src/ble_collar.cpp

#include "ble_collar.h"
#include "config.h"
#include <NimBLEDevice.h>

static NimBLEServer* pServer = nullptr;
static NimBLECharacteristic* pLocation = nullptr;
static NimBLECharacteristic* pGeofence = nullptr;
static NimBLECharacteristic* pBattery = nullptr;
static NimBLECharacteristic* pActivity = nullptr;
static NimBLECharacteristic* pCommand = nullptr;
static NimBLECharacteristic* pConfig = nullptr;

static bool device_connected = false;

class CollarServerCallbacks : public NimBLEServerCallbacks {
    void onConnect(NimBLEServer* pServer) override {
        device_connected = true;
        Serial.println("[BLE] Client connected");
    }
    void onDisconnect(NimBLEServer* pServer) override {
        device_connected = false;
        Serial.println("[BLE] Client disconnected");
        NimBLEDevice::startAdvertising();  // Resume advertising
    }
};

class CommandCallbacks : public NimBLECharacteristicCallbacks {
    void onWrite(NimBLECharacteristic* pChar) override {
        std::string value = pChar->getValue();
        Serial.printf("[BLE] Command received: %s\n", value.c_str());

        if (value == "buzz") {
            buzzer_play(BUZZ_LONG);  // Find my dog!
        } else if (value == "locate") {
            // Flash LED and buzz for 30 seconds
            buzzer_play_continuous(30000);
        } else if (value == "sleep") {
            // Force deep sleep
            enter_deep_sleep();
        } else if (value.substr(0, 14) == "update_fences:") {
            // Inline fence update via BLE
            String json = String(value.substr(14).c_str());
            storage_save_geofences(json);
            geofence_reload();
        }
    }
};

void ble_init() {
    String device_name = String(BLE_DEVICE_NAME_PREFIX) +
                         String(storage_get_collar_id()).substring(0, 8);

    NimBLEDevice::init(device_name.c_str());
    NimBLEDevice::setPower(ESP_PWR_LVL_N0);  // Low TX power to save battery
    NimBLEDevice::setSecurityAuth(true, true, true);  // Bond, MITM, secure conn

    pServer = NimBLEDevice::createServer();
    pServer->setCallbacks(new CollarServerCallbacks());

    NimBLEService* pService = pServer->createService(BLE_SERVICE_UUID);

    // Location characteristic (READ + NOTIFY)
    pLocation = pService->createCharacteristic(
        BLE_CHAR_LOCATION_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
    );

    // Geofence status (READ + NOTIFY)
    pGeofence = pService->createCharacteristic(
        BLE_CHAR_GEOFENCE_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
    );

    // Battery (READ + NOTIFY)
    pBattery = pService->createCharacteristic(
        BLE_CHAR_BATTERY_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
    );

    // Activity (READ + NOTIFY)
    pActivity = pService->createCharacteristic(
        BLE_CHAR_ACTIVITY_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
    );

    // Command (WRITE, encrypted)
    pCommand = pService->createCharacteristic(
        BLE_CHAR_COMMAND_UUID,
        NIMBLE_PROPERTY::WRITE | NIMBLE_PROPERTY::WRITE_ENC
    );
    pCommand->setCallbacks(new CommandCallbacks());

    // Config (READ + WRITE, encrypted)
    pConfig = pService->createCharacteristic(
        BLE_CHAR_CONFIG_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::WRITE |
        NIMBLE_PROPERTY::READ_ENC | NIMBLE_PROPERTY::WRITE_ENC
    );

    pService->start();
    Serial.printf("[BLE] Service started: %s\n", device_name.c_str());
}

void ble_start_advertising() {
    NimBLEAdvertising* pAdvertising = NimBLEDevice::getAdvertising();
    pAdvertising->addServiceUUID(BLE_SERVICE_UUID);
    pAdvertising->setScanResponse(true);
    pAdvertising->setMinInterval(160);   // 100ms (low power)
    pAdvertising->setMaxInterval(800);   // 500ms
    pAdvertising->start();
    Serial.println("[BLE] Advertising started");
}

void ble_stop_advertising() {
    NimBLEDevice::getAdvertising()->stop();
}

void ble_process() {
    // NimBLE handles events internally via FreeRTOS task
    // This function is for any periodic BLE housekeeping
}

void ble_update_location(const GpsFix& fix) {
    if (!device_connected) return;

    JsonDocument doc;
    doc["lat"] = fix.lat;
    doc["lng"] = fix.lng;
    doc["alt"] = fix.altitude;
    doc["acc"] = fix.accuracy;
    doc["spd"] = fix.speed;
    doc["hdg"] = fix.heading;
    doc["sat"] = fix.satellites;
    doc["age"] = fix.age_ms;

    String json;
    serializeJson(doc, json);
    pLocation->setValue(json.c_str());
    pLocation->notify();
}

void ble_update_battery(float percentage, bool charging) {
    if (!device_connected) return;

    JsonDocument doc;
    doc["pct"] = percentage;
    doc["v"] = power_get_voltage();
    doc["chg"] = charging;

    // Estimate hours remaining
    float hours = 0;
    if (!charging && percentage > 0) {
        // Very rough: 500mAh / ~30mA active = ~17h at 100%
        hours = (percentage / 100.0) * 17.0;
    }
    doc["hrs"] = hours;

    String json;
    serializeJson(doc, json);
    pBattery->setValue(json.c_str());
    pBattery->notify();
}

void ble_update_activity(const ActivityData& activity) {
    if (!device_connected) return;

    JsonDocument doc;
    doc["state"] = motion_type_name(activity.type);
    doc["steps"] = activity.steps_today;
    doc["dist"] = activity.distance_m_today;
    doc["mins"] = activity.active_minutes_today;

    String json;
    serializeJson(doc, json);
    pActivity->setValue(json.c_str());
    pActivity->notify();
}

int ble_check_door_rssi() {
    // Scan for door BLE advertisement
    // The door advertises with service UUID from config.h (door's BLE_SERVICE_UUID)
    NimBLEScan* pScan = NimBLEDevice::getScan();
    pScan->setActiveScan(false);  // Passive scan (lower power)
    pScan->setInterval(100);
    pScan->setWindow(50);

    NimBLEScanResults results = pScan->start(1, false);  // 1 second scan
    pScan->stop();

    for (int i = 0; i < results.getCount(); i++) {
        NimBLEAdvertisedDevice device = results.getDevice(i);
        if (device.isAdvertisingService(NimBLEUUID(DOOR_BLE_SERVICE_UUID))) {
            return device.getRSSI();
        }
    }
    return -100;  // Not found
}
```

---

## 10. Watchdog & Error Recovery

```c
// In main.cpp setup():

#include <esp_task_wdt.h>

// 30-second watchdog (same as door firmware)
esp_task_wdt_init(30, true);  // 30s timeout, panic on timeout
esp_task_wdt_add(NULL);       // Add current task

// In loop():
esp_task_wdt_reset();  // Feed the dog (pet the watchdog)

// If any operation hangs for >30s, ESP32 auto-reboots
// On reboot: checks wake reason, resumes appropriate state
```

---

## 11. Memory Budget

```
Flash (8MB):
├── Bootloader:    32KB
├── Partition table: 4KB
├── App (OTA_0):   2MB    (firmware binary)
├── App (OTA_1):   2MB    (OTA update slot)
├── NVS:           64KB   (config, credentials, geofences)
├── SPIFFS:        1MB    (GPS buffer, event queue, track log)
└── Reserved:      ~2.9MB

RAM (512KB SRAM + 8MB PSRAM):
├── Static:        ~80KB  (globals, BLE stack, WiFi stack)
├── Stack:         ~16KB  (main task + BLE task)
├── Heap:          ~200KB (JSON parsing, HTTP buffers)
├── NimBLE:        ~40KB  (connection management)
└── Free:          ~176KB

PSRAM (8MB):
├── GPS track buffer (extended): ~4MB (can buffer ~24 hours of 1Hz data)
└── Available for future use:    ~4MB
```
