#include "wifi_manager.h"
#include "config.h"
#include <WiFi.h>
#include <nvs_flash.h>
#include <nvs.h>

static unsigned long last_reconnect_attempt = 0;
static char nvs_ssid[64] = {0};
static char nvs_pass[64] = {0};

bool wifi_connect() {
    const char* ssid = WIFI_SSID;
    const char* password = WIFI_PASSWORD;

    // Check for BLE-provisioned credentials stored in NVS (encrypted partition)
    nvs_handle_t handle;
    if (nvs_open("wifi", NVS_READONLY, &handle) == ESP_OK) {
        size_t ssid_len = sizeof(nvs_ssid);
        size_t pass_len = sizeof(nvs_pass);
        if (nvs_get_str(handle, "ssid", nvs_ssid, &ssid_len) == ESP_OK &&
            nvs_get_str(handle, "pass", nvs_pass, &pass_len) == ESP_OK &&
            strlen(nvs_ssid) > 0) {
            ssid = nvs_ssid;
            password = nvs_pass;
            Serial.printf("Connecting to WiFi (from NVS): %s\n", ssid);
        }
        nvs_close(handle);
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

bool wifi_save_credentials(const char* ssid, const char* pass) {
    nvs_handle_t handle;
    if (nvs_open("wifi", NVS_READWRITE, &handle) != ESP_OK) return false;
    nvs_set_str(handle, "ssid", ssid);
    nvs_set_str(handle, "pass", pass);
    esp_err_t err = nvs_commit(handle);
    nvs_close(handle);
    return err == ESP_OK;
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
