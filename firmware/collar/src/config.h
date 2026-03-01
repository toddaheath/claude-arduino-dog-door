#pragma once

// ── Firmware Version ────────────────────────────────────────
#ifndef COLLAR_FW_VERSION
#define COLLAR_FW_VERSION "1.0.0"
#endif

// ── Pin Assignments (ESP32-S3-MINI-1) ───────────────────────
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

// ── I2C Addresses ───────────────────────────────────────────
#define I2C_ADDR_PN532   0x24
#define I2C_ADDR_LSM6DSO 0x6A

// ── BLE Configuration ───────────────────────────────────────
#define BLE_DEVICE_NAME_PREFIX  "SDD-Collar-"
#define BLE_SERVICE_UUID        "5a6d0001-8e7f-4b3c-9d2a-1c6e3f8b7d4e"
#define BLE_CHAR_LOCATION_UUID  "5a6d0002-8e7f-4b3c-9d2a-1c6e3f8b7d4e"
#define BLE_CHAR_GEOFENCE_UUID  "5a6d0003-8e7f-4b3c-9d2a-1c6e3f8b7d4e"
#define BLE_CHAR_BATTERY_UUID   "5a6d0004-8e7f-4b3c-9d2a-1c6e3f8b7d4e"
#define BLE_CHAR_ACTIVITY_UUID  "5a6d0005-8e7f-4b3c-9d2a-1c6e3f8b7d4e"
#define BLE_CHAR_COMMAND_UUID   "5a6d0006-8e7f-4b3c-9d2a-1c6e3f8b7d4e"
#define BLE_CHAR_CONFIG_UUID    "5a6d0007-8e7f-4b3c-9d2a-1c6e3f8b7d4e"

// Door BLE service UUID (for proximity detection)
#define DOOR_BLE_SERVICE_UUID   "4fafc201-1fb5-459e-8fcc-c5c9c331914b"

// ── GPS Configuration ───────────────────────────────────────
#define GPS_BAUD_RATE            9600
#define GPS_FIX_RATE_MOVING_HZ   1     // 1 Hz when moving
#define GPS_FIX_RATE_STATIC_HZ   10    // 0.1 Hz when stationary (1 fix/10s)
#define GPS_COLD_START_TIMEOUT_MS 120000 // 2 min for cold start
#define GPS_HDOP_THRESHOLD       5.0    // Suppress geofence if HDOP > this
#define GPS_MIN_SATELLITES       4      // Minimum for quality fix

// ── Power Management ────────────────────────────────────────
#define BATTERY_VOLTAGE_DIVIDER  2.0    // 100k/100k divider ratio
#define BATTERY_FULL_MV          4200   // Fully charged LiPo
#define BATTERY_EMPTY_MV         3200   // Empty (protect cutoff)
#define BATTERY_LOW_PCT          15     // Low battery warning
#define BATTERY_CRITICAL_PCT     5      // Critical warning
#define SLEEP_TIMEOUT_MS         300000 // 5 min idle → deep sleep
#define IMU_MOTION_THRESHOLD     200    // LSM6DSO wake threshold (mg)
#define IMU_MOTION_DURATION_MS   2000   // Sustained motion to confirm wake

// ── WiFi Upload ─────────────────────────────────────────────
#define WIFI_UPLOAD_INTERVAL_MS  30000  // Upload batch every 30s
#define WIFI_CONNECT_TIMEOUT_MS  10000  // Max 10s to connect
#define WIFI_MAX_BATCH_SIZE      100    // Max GPS points per upload
#define API_BASE_URL             "https://your-api.example.com/api/v1"

// ── Geofence ────────────────────────────────────────────────
#define GEOFENCE_MAX_FENCES      20
#define GEOFENCE_HYSTERESIS_M    3.0
#define GEOFENCE_WARNING_DIST_M  5.0
#define GEOFENCE_BREACH_BUZZ_INTERVAL_MS 10000

// ── NFC ─────────────────────────────────────────────────────
#define NFC_HANDSHAKE_TIMEOUT_MS 500
#define NFC_AUTH_REPLAY_WINDOW_S 30
#define NFC_SHARED_SECRET_LEN    32
#define NFC_COLLAR_ID_LEN        16

// ── Buzzer Patterns ─────────────────────────────────────────
#define BUZZER_FREQ_WARNING      2700   // Hz
#define BUZZER_FREQ_BREACH       2200   // Hz
#define BUZZER_FREQ_CONTINUOUS   2500   // Hz
#define BUZZER_MAX_DUTY          128    // PWM duty (50%)
#define WARNING_INTERVAL_MS      5000
#define BREACH_ESCALATE_1_MS     10000
#define BREACH_ESCALATE_2_MS     30000
#define BREACH_ESCALATE_3_MS     60000

// ── Watchdog ────────────────────────────────────────────────
#define WDT_TIMEOUT_S            30
