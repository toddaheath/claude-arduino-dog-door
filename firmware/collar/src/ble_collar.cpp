#include "ble_collar.h"
#include "config.h"

#ifndef UNIT_TEST
#include <Arduino.h>
#include <NimBLEDevice.h>
#include <ArduinoJson.h>
#include <mbedtls/base64.h>
#include "power_manager.h"
#include "storage.h"
#include "buzzer.h"

// Forward declarations
void enter_deep_sleep();
void geofence_reload();

static NimBLEServer* pServer = nullptr;
static NimBLECharacteristic* pLocation = nullptr;
static NimBLECharacteristic* pGeofence = nullptr;
static NimBLECharacteristic* pBattery = nullptr;
static NimBLECharacteristic* pActivity = nullptr;
static NimBLECharacteristic* pCommand = nullptr;
static NimBLECharacteristic* pConfig = nullptr;

static bool device_connected = false;

class CollarServerCallbacks : public NimBLEServerCallbacks {
    void onConnect(NimBLEServer* server) override {
        device_connected = true;
        Serial.println("[BLE] Client connected");
    }
    void onDisconnect(NimBLEServer* server) override {
        device_connected = false;
        Serial.println("[BLE] Client disconnected");
        NimBLEDevice::startAdvertising();
    }
};

class ConfigCallbacks : public NimBLECharacteristicCallbacks {
    void onWrite(NimBLECharacteristic* pChar) override {
        std::string raw = pChar->getValue();
        Serial.printf("[BLE] Config write: %d bytes\n", (int)raw.size());

        JsonDocument doc;
        DeserializationError err = deserializeJson(doc, raw.c_str());
        if (err) {
            Serial.printf("[BLE] Config JSON error: %s\n", err.c_str());
            return;
        }

        // WiFi provisioning: {"ssid":"...","pass":"...","apiKey":"..."}
        if (doc.containsKey("ssid") && doc.containsKey("pass")) {
            const char* ssid = doc["ssid"];
            const char* pass = doc["pass"];
            const char* key  = doc["apiKey"] | "";
            storage_save_wifi_creds(ssid, pass, key);
            Serial.printf("[BLE] WiFi configured: %s\n", ssid);
            buzzer_play(BUZZ_SHORT);
        }

        // Collar identity provisioning: {"collarId":"...","secret":"..."}
        if (doc.containsKey("collarId") && doc.containsKey("secret")) {
            const char* cid = doc["collarId"];
            const char* secret_b64 = doc["secret"];
            // Decode base64 shared secret
            size_t b64_len = strlen(secret_b64);
            uint8_t secret_bytes[32] = {0};
            // Simple base64 decode (ESP32 mbedtls)
            size_t olen = 0;
            mbedtls_base64_decode(secret_bytes, sizeof(secret_bytes), &olen,
                                  (const uint8_t*)secret_b64, b64_len);
            storage_save_collar_identity(cid, secret_bytes);
            Serial.printf("[BLE] Collar identity set: %s\n", cid);
            buzzer_play(BUZZ_SHORT);
        }
    }

    void onRead(NimBLECharacteristic* pChar) override {
        // Return current provisioning status
        JsonDocument doc;
        doc["collarId"] = storage_get_collar_id();
        doc["wifiConfigured"] = (strlen(storage_get_wifi_ssid()) > 0);
        doc["fwVersion"] = COLLAR_FW_VERSION;

        String json;
        serializeJson(doc, json);
        pChar->setValue(json.c_str());
    }
};

class CommandCallbacks : public NimBLECharacteristicCallbacks {
    void onWrite(NimBLECharacteristic* pChar) override {
        std::string value = pChar->getValue();
        Serial.printf("[BLE] Command received: %s\n", value.c_str());

        if (value == "buzz") {
            buzzer_play(BUZZ_LONG);
        } else if (value == "locate") {
            buzzer_play_continuous(30000);
        } else if (value == "sleep") {
            enter_deep_sleep();
        } else if (value.length() > 14 && value.substr(0, 14) == "update_fences:") {
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
    NimBLEDevice::setPower(ESP_PWR_LVL_N0);
    NimBLEDevice::setSecurityAuth(true, true, true);

    pServer = NimBLEDevice::createServer();
    pServer->setCallbacks(new CollarServerCallbacks());

    NimBLEService* pService = pServer->createService(BLE_SERVICE_UUID);

    pLocation = pService->createCharacteristic(
        BLE_CHAR_LOCATION_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
    );

    pGeofence = pService->createCharacteristic(
        BLE_CHAR_GEOFENCE_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
    );

    pBattery = pService->createCharacteristic(
        BLE_CHAR_BATTERY_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
    );

    pActivity = pService->createCharacteristic(
        BLE_CHAR_ACTIVITY_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
    );

    pCommand = pService->createCharacteristic(
        BLE_CHAR_COMMAND_UUID,
        NIMBLE_PROPERTY::WRITE | NIMBLE_PROPERTY::WRITE_ENC
    );
    pCommand->setCallbacks(new CommandCallbacks());

    pConfig = pService->createCharacteristic(
        BLE_CHAR_CONFIG_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::WRITE |
        NIMBLE_PROPERTY::READ_ENC | NIMBLE_PROPERTY::WRITE_ENC
    );
    pConfig->setCallbacks(new ConfigCallbacks());

    pService->start();
    Serial.printf("[BLE] Service started: %s\n", device_name.c_str());
}

void ble_start_advertising() {
    NimBLEAdvertising* pAdvertising = NimBLEDevice::getAdvertising();
    pAdvertising->addServiceUUID(BLE_SERVICE_UUID);
    pAdvertising->setScanResponse(true);
    pAdvertising->setMinInterval(160);   // 100ms
    pAdvertising->setMaxInterval(800);   // 500ms
    pAdvertising->start();
    Serial.println("[BLE] Advertising started");
}

void ble_stop_advertising() {
    NimBLEDevice::getAdvertising()->stop();
}

void ble_process() {
    // NimBLE handles events via FreeRTOS task
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

    float hours = 0;
    if (!charging && percentage > 0) {
        hours = (percentage / 100.0f) * 17.0f;
    }
    doc["hrs"] = hours;

    String json;
    serializeJson(doc, json);
    pBattery->setValue(json.c_str());
    pBattery->notify();
}

void ble_update_activity(const ActivityData& act) {
    if (!device_connected) return;

    JsonDocument doc;
    doc["state"] = motion_type_name(act.type);
    doc["steps"] = act.steps_today;
    doc["dist"] = act.distance_m_today;
    doc["mins"] = act.active_minutes_today;

    String json;
    serializeJson(doc, json);
    pActivity->setValue(json.c_str());
    pActivity->notify();
}

int ble_check_door_rssi() {
    NimBLEScan* pScan = NimBLEDevice::getScan();
    pScan->setActiveScan(false);
    pScan->setInterval(100);
    pScan->setWindow(50);

    NimBLEScanResults results = pScan->start(1, false);
    pScan->stop();

    for (int i = 0; i < results.getCount(); i++) {
        NimBLEAdvertisedDevice device = results.getDevice(i);
        if (device.isAdvertisingService(NimBLEUUID(DOOR_BLE_SERVICE_UUID))) {
            return device.getRSSI();
        }
    }
    return -100;
}

#endif  // !UNIT_TEST
