#include "wifi_manager.h"
#include "config.h"
#include <WiFi.h>

static unsigned long last_reconnect_attempt = 0;

bool wifi_connect() {
    Serial.printf("Connecting to WiFi: %s\n", WIFI_SSID);

    WiFi.mode(WIFI_STA);
    WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

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
