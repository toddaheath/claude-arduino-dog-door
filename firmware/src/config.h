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
#define API_BASE_URL "http://192.168.1.100:5001"
#define API_ACCESS_ENDPOINT "/api/doors/access-request"
#define API_KEY ""  // Set if door configuration has API key
#define API_TIMEOUT_MS 10000

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

#endif // CONFIG_H
