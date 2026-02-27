#ifndef CONFIG_H
#define CONFIG_H

// ===== Door Side Configuration =====
// Dual-board architecture: two ESP32-CAMs, one per side of the door.
// Set THIS_SIDE to the side this board is mounted on.
// Inside camera detects dogs exiting; outside camera detects dogs entering.
#define SIDE_INSIDE  "inside"
#define SIDE_OUTSIDE "outside"

#ifndef THIS_SIDE
#define THIS_SIDE SIDE_INSIDE  // Default; override with -DTHIS_SIDE=SIDE_OUTSIDE
#endif

// ===== WiFi Configuration =====
#define WIFI_SSID "YOUR_WIFI_SSID"
#define WIFI_PASSWORD "YOUR_WIFI_PASSWORD"
#define WIFI_CONNECT_TIMEOUT_MS 10000
#define WIFI_RECONNECT_INTERVAL_MS 5000

// ===== API Configuration =====
#define API_BASE_URL "https://192.168.1.100:5001"
#define API_ACCESS_ENDPOINT "/api/v1/doors/access-request"
#define API_APPROACH_ENDPOINT "/api/v1/doors/approach-photo"
#define API_KEY ""  // Set if door configuration has API key
#define API_TIMEOUT_MS 10000
// Set to 1 to skip server certificate verification (dev only).
// For production, set to 0 and provide API_CA_CERT below.
#define API_INSECURE_TLS 1
// PEM-encoded CA certificate for server verification (when API_INSECURE_TLS=0).
// Replace with your server's CA root certificate.
#define API_CA_CERT ""

// ===== Pin Definitions =====
// Radar sensor (RCWL-0516)
#define PIN_RADAR 12

// Ultrasonic sensor (HC-SR04)
#define PIN_ULTRASONIC_TRIG 13
#define PIN_ULTRASONIC_ECHO 14

// IR Break Beam sensor
#define PIN_IR_BEAM 15

// Motor driver (L298N) for linear actuator
#define PIN_MOTOR_IN1 2
#define PIN_MOTOR_IN2 4

// Reed switch (door position)
#define PIN_REED_SWITCH 16

// Status LEDs
#define PIN_LED_GREEN 33
#define PIN_LED_RED 32

// ===== Camera Configuration =====
// AI-Thinker ESP32-CAM pin definitions
#define CAMERA_MODEL_AI_THINKER
#define PWDN_GPIO_NUM     32
#define RESET_GPIO_NUM    -1
#define XCLK_GPIO_NUM      0
#define SIOD_GPIO_NUM     26
#define SIOC_GPIO_NUM     27
#define Y9_GPIO_NUM       35
#define Y8_GPIO_NUM       34
#define Y7_GPIO_NUM       39
#define Y6_GPIO_NUM       36
#define Y5_GPIO_NUM       21
#define Y4_GPIO_NUM       19
#define Y3_GPIO_NUM       18
#define Y2_GPIO_NUM        5
#define VSYNC_GPIO_NUM    25
#define HREF_GPIO_NUM     23
#define PCLK_GPIO_NUM     22

// ===== Sensor Thresholds =====
#define ULTRASONIC_TRIGGER_DISTANCE_CM 50  // Trigger when animal within 50cm
#define ULTRASONIC_MAX_DISTANCE_CM 400

// ===== Door Configuration =====
#define DOOR_OPEN_TIME_MS 500       // Time to run actuator to open
#define DOOR_CLOSE_TIME_MS 500      // Time to run actuator to close
#define DOOR_AUTO_CLOSE_DELAY_MS 10000  // Wait before auto-closing
#define DOOR_SAFETY_CHECK_INTERVAL_MS 100

// ===== Detection Configuration =====
#define DETECTION_CONFIDENCE_THRESHOLD 0.7f
#define DETECTION_COOLDOWN_MS 5000  // Min time between detection events

// ===== TFLite Configuration =====
#define TFLITE_ARENA_SIZE 96 * 1024  // 96KB tensor arena

// ===== Power Monitor (voltage divider R1=10kΩ, R2=3.3kΩ) =====
// IMPORTANT: On AI-Thinker ESP32-CAM, GPIO 34/35 are camera data lines (Y8/Y9).
// The power monitor is DISABLED by default to avoid breaking the camera.
// To enable, set POWER_MONITOR_ENABLED=1 and wire the voltage divider + detect
// to free GPIOs on your specific board/wiring (not GPIO 34/35 on AI-Thinker).
#define POWER_MONITOR_ENABLED 0
#define PIN_POWER_ADC 34           // ADC1_CH6 — reassign if conflicts with camera
#define PIN_POWER_DETECT 35        // HIGH=main power present — reassign if conflicts
#define BATTERY_LOW_THRESHOLD_PCT 20
#define BATTERY_CHARGED_THRESHOLD_PCT 95
#define BATTERY_FULL_VOLTS 12.6f
#define BATTERY_EMPTY_VOLTS 10.5f
#define POWER_VDIV_RATIO 4.03f     // (10000+3300)/3300

// ===== Cellular (A7670E on UART2) =====
#define CELLULAR_RX_PIN 16
#define CELLULAR_TX_PIN 17
#define CELLULAR_BAUD_RATE 115200
#define CELLULAR_TIMEOUT_MS 15000
#define CELLULAR_APN "your.apn.here"

// ===== BLE (ESP32 built-in) =====
#define BLE_SERVICE_UUID      "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define BLE_STATUS_CHAR_UUID  "beb5483e-36e1-4688-b7f5-ea07361b26a8"
#define BLE_COMMAND_CHAR_UUID "6e400002-b5a3-f393-e0a9-e50e24dcca9e"
#define BLE_WIFI_CHAR_UUID    "6e400003-b5a3-f393-e0a9-e50e24dcca9e"
#define BLE_DEVICE_NAME "SmartDogDoor"
#define BLE_PASSKEY 123456  // Change this! 6-digit numeric passkey for BLE pairing

// ===== Offline Queue =====
#define OFFLINE_QUEUE_MAX_EVENTS 50
#define API_FIRMWARE_EVENT_ENDPOINT "/api/v1/doors/firmware-event"

#endif // CONFIG_H
