#include "power_monitor.h"
#include "config.h"
#include "offline_queue.h"
#include <Arduino.h>
#include <HTTPClient.h>
#include <WiFiClientSecure.h>
#include <ArduinoJson.h>

#if !POWER_MONITOR_ENABLED

void power_monitor_init() {
    Serial.println("[SKIP] Power monitor disabled (GPIO conflict with camera)");
}
float power_monitor_read_voltage() { return 0.0f; }
int power_monitor_battery_percent() { return -1; }
void power_monitor_update(const char*, const char*) {}

#else // POWER_MONITOR_ENABLED

static bool _lastMainPower = true;
static bool _lastBatteryLow = false;
static bool _lastBatteryCharged = false;
static bool _initialized = false;

static void post_firmware_event(const char* apiKey, const char* baseUrl, const char* eventType, const char* notes, float voltage);

void power_monitor_init() {
    pinMode(PIN_POWER_ADC, INPUT);
    pinMode(PIN_POWER_DETECT, INPUT);
    _initialized = true;
    Serial.println("[OK] Power monitor initialized");
}

float power_monitor_read_voltage() {
    int raw = analogRead(PIN_POWER_ADC);
    float vref = 3.3f;
    float adcVolts = (raw / 4095.0f) * vref;
    return adcVolts * POWER_VDIV_RATIO;
}

int power_monitor_battery_percent() {
    float v = power_monitor_read_voltage();
    if (v >= BATTERY_FULL_VOLTS) return 100;
    if (v <= BATTERY_EMPTY_VOLTS) return 0;
    return (int)(((v - BATTERY_EMPTY_VOLTS) / (BATTERY_FULL_VOLTS - BATTERY_EMPTY_VOLTS)) * 100.0f);
}

void power_monitor_update(const char* apiKey, const char* baseUrl) {
    if (!_initialized) return;

    bool mainPower = digitalRead(PIN_POWER_DETECT) == HIGH;
    float voltage = power_monitor_read_voltage();
    int pct = power_monitor_battery_percent();

    // Main power state change
    if (mainPower != _lastMainPower) {
        _lastMainPower = mainPower;
        const char* eventType = mainPower ? "PowerRestored" : "PowerLost";
        Serial.printf("[POWER] %s\n", eventType);
        post_firmware_event(apiKey, baseUrl, eventType, nullptr, voltage);
    }

    // Battery low transition
    bool battLow = (pct <= BATTERY_LOW_THRESHOLD_PCT);
    if (battLow && !_lastBatteryLow) {
        _lastBatteryLow = true;
        Serial.printf("[POWER] BatteryLow (%d%%)\n", pct);
        post_firmware_event(apiKey, baseUrl, "BatteryLow", nullptr, voltage);
    } else if (!battLow) {
        _lastBatteryLow = false;
    }

    // Battery charged transition
    bool battCharged = (pct >= BATTERY_CHARGED_THRESHOLD_PCT);
    if (battCharged && !_lastBatteryCharged) {
        _lastBatteryCharged = true;
        Serial.printf("[POWER] BatteryCharged (%d%%)\n", pct);
        post_firmware_event(apiKey, baseUrl, "BatteryCharged", nullptr, voltage);
    } else if (!battCharged) {
        _lastBatteryCharged = false;
    }
}

static WiFiClientSecure& getPowerSecureClient() {
    static WiFiClientSecure client;
    static bool initialized = false;
    if (!initialized) {
#if API_INSECURE_TLS
        client.setInsecure();
#else
        if (strlen(API_CA_CERT) > 0) {
            client.setCACert(API_CA_CERT);
        } else {
            client.setInsecure();
        }
#endif
        initialized = true;
    }
    return client;
}

static void post_firmware_event(const char* apiKey, const char* baseUrl, const char* eventType, const char* notes, float voltage) {
    JsonDocument doc;
    doc["apiKey"] = apiKey;
    doc["eventType"] = eventType;
    doc["notes"] = notes ? notes : "";
    doc["batteryVoltage"] = voltage;

    String body;
    serializeJson(doc, body);

    String url = String(baseUrl) + String(API_FIRMWARE_EVENT_ENDPOINT);
    HTTPClient http;
    http.begin(getPowerSecureClient(), url);
    http.addHeader("Content-Type", "application/json");
    http.setTimeout(API_TIMEOUT_MS);

    int code = http.POST(body);
    http.end();

    if (code != 204 && code != 200) {
        // Queue for later if network unavailable
        QueuedEvent evt;
        evt.eventType = eventType;
        evt.notes = notes ? notes : "";
        evt.batteryVoltage = voltage;
        evt.apiKey = apiKey;
        evt.timestamp = millis();
        offline_queue_push(evt);
    }
}

#endif // POWER_MONITOR_ENABLED
