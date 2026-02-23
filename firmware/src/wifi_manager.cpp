#include "wifi_manager.h"
#include "config.h"
#include <WiFi.h>
#include <LittleFS.h>
#include <ArduinoJson.h>

static unsigned long last_reconnect_attempt = 0;

bool wifi_connect() {
    const char* ssid = WIFI_SSID;
    const char* password = WIFI_PASSWORD;

    // Check for BLE-provisioned credentials stored in LittleFS
    if (LittleFS.exists("/wifi_creds.json")) {
        File f = LittleFS.open("/wifi_creds.json", "r");
        if (f) {
            JsonDocument doc;
            if (deserializeJson(doc, f) == DeserializationError::Ok) {
                const char* stored_ssid = doc["ssid"] | "";
                const char* stored_pass = doc["password"] | "";
                if (strlen(stored_ssid) > 0) {
                    ssid = stored_ssid;
                    password = stored_pass;
                    Serial.printf("Connecting to WiFi (from LittleFS): %s\n", ssid);
                }
            }
            f.close();
        }
    }

    if (ssid == WIFI_SSID) {
        Serial.printf("Connecting to WiFi: %s\n", ssid);
    }

    WiFi.mode(WIFI_STA);
    WiFi.begin(ssid, password);

    unsigned long start = millis();
    while (WiFi.status() != WL_CONNECTED) {
        if (millis() - start > WIFI_CONNECT_TIMEOUT_MS) {
            Serial.println("WiFi connection timeout");
            return false;
        }
        delay(500);
        Serial.print(".");
    }

    Serial.printf("\nWiFi connected. IP: %s\n", WiFi.localIP().toString().c_str());
    return true;
}

bool wifi_is_connected() {
    return WiFi.status() == WL_CONNECTED;
}

void wifi_ensure_connected() {
    if (wifi_is_connected()) return;

    unsigned long now = millis();
    if (now - last_reconnect_attempt < WIFI_RECONNECT_INTERVAL_MS) return;

    last_reconnect_attempt = now;
    Serial.println("WiFi disconnected. Reconnecting...");
    wifi_connect();
}

String wifi_get_ip() {
    return WiFi.localIP().toString();
}
