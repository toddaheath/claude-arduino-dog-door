#include "ble_server.h"
#include "config.h"
#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>
#include <ArduinoJson.h>
#include <LittleFS.h>

static BLEServer* _server = nullptr;
static BLECharacteristic* _statusChar = nullptr;
static BLECharacteristic* _commandChar = nullptr;
static BLECharacteristic* _wifiChar = nullptr;
static bool _deviceConnected = false;

static volatile bool _pendingOpen = false;
static volatile bool _hasCommand = false;
static volatile bool _hasWifiUpdate = false;
static char _pendingSsid[64] = {0};
static char _pendingPass[64] = {0};

class ServerCallbacks : public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) override {
        _deviceConnected = true;
        Serial.println("[BLE] Client connected");
    }
    void onDisconnect(BLEServer* pServer) override {
        _deviceConnected = false;
        Serial.println("[BLE] Client disconnected");
    }
};

class CommandCallbacks : public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic* pChar) override {
        String val = pChar->getValue().c_str();
        val.trim();
        if (val.equalsIgnoreCase("open")) {
            _pendingOpen = true;
            _hasCommand = true;
        } else if (val.equalsIgnoreCase("close")) {
            _pendingOpen = false;
            _hasCommand = true;
        }
        Serial.printf("[BLE] Command: %s\n", val.c_str());
    }
};

class WifiCallbacks : public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic* pChar) override {
        String val = pChar->getValue().c_str();
        JsonDocument doc;
        if (deserializeJson(doc, val) == DeserializationError::Ok) {
            const char* ssid = doc["ssid"] | "";
            const char* pass = doc["password"] | "";
            strncpy(_pendingSsid, ssid, 63);
            strncpy(_pendingPass, pass, 63);
            _hasWifiUpdate = true;

            // Persist to LittleFS
            File f = LittleFS.open("/wifi_creds.json", "w");
            if (f) {
                serializeJson(doc, f);
                f.close();
                Serial.println("[BLE] WiFi credentials saved");
            }
        }
    }
};

void ble_server_init() {
    BLEDevice::init(BLE_DEVICE_NAME);
    _server = BLEDevice::createServer();
    _server->setCallbacks(new ServerCallbacks());

    BLEService* service = _server->createService(BLE_SERVICE_UUID);

    _statusChar = service->createCharacteristic(
        BLE_STATUS_CHAR_UUID,
        BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_NOTIFY);
    _statusChar->addDescriptor(new BLE2902());
    _statusChar->setValue("{}");

    _commandChar = service->createCharacteristic(
        BLE_COMMAND_CHAR_UUID,
        BLECharacteristic::PROPERTY_WRITE);
    _commandChar->setCallbacks(new CommandCallbacks());

    _wifiChar = service->createCharacteristic(
        BLE_WIFI_CHAR_UUID,
        BLECharacteristic::PROPERTY_WRITE);
    _wifiChar->setCallbacks(new WifiCallbacks());

    service->start();
    BLEAdvertising* advertising = BLEDevice::getAdvertising();
    advertising->addServiceUUID(BLE_SERVICE_UUID);
    advertising->setScanResponse(true);
    BLEDevice::startAdvertising();

    Serial.println("[OK] BLE server started, advertising as " BLE_DEVICE_NAME);
}

void ble_server_update() {
    if (!_deviceConnected) {
        BLEDevice::startAdvertising();
    }
}

void ble_server_set_status(bool doorOpen, const char* lastEvent, bool wifiConnected, int batteryPct) {
    if (!_statusChar) return;

    JsonDocument doc;
    doc["doorOpen"] = doorOpen;
    doc["lastEvent"] = lastEvent;
    doc["wifi"] = wifiConnected;
    doc["battery"] = batteryPct;

    String json;
    serializeJson(doc, json);
    _statusChar->setValue(json.c_str());
    if (_deviceConnected) {
        _statusChar->notify();
    }
}

bool ble_server_get_command(bool* openDoor) {
    if (!_hasCommand) return false;
    *openDoor = _pendingOpen;
    _hasCommand = false;
    return true;
}

bool ble_server_get_wifi_update(char* ssid, char* pass, size_t maxLen) {
    if (!_hasWifiUpdate) return false;
    strncpy(ssid, _pendingSsid, maxLen - 1);
    strncpy(pass, _pendingPass, maxLen - 1);
    ssid[maxLen - 1] = '\0';
    pass[maxLen - 1] = '\0';
    _hasWifiUpdate = false;
    return true;
}
