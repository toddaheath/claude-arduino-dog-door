#include "ble_server.h"
#include "config.h"
#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>
#include <BLESecurity.h>
#include <ArduinoJson.h>
#include "wifi_manager.h"

static BLEServer* _server = nullptr;
static BLECharacteristic* _statusChar = nullptr;
static BLECharacteristic* _commandChar = nullptr;
static BLECharacteristic* _wifiChar = nullptr;
static bool _deviceConnected = false;
static bool _isAdvertising = false;

static volatile bool _pendingOpen = false;
static volatile bool _hasCommand = false;
static volatile bool _hasWifiUpdate = false;
static char _pendingSsid[64] = {0};
static char _pendingPass[64] = {0};

class SecurityCallbacks : public BLESecurityCallbacks {
    uint32_t onPassKeyRequest() override {
        Serial.println("[BLE] Passkey requested");
        return BLE_PASSKEY;
    }

    void onPassKeyNotify(uint32_t pass_key) override {
        Serial.printf("[BLE] Passkey notify: %06d\n", pass_key);
    }

    bool onConfirmPIN(uint32_t pin) override {
        Serial.printf("[BLE] Confirm PIN: %06d\n", pin);
        return pin == BLE_PASSKEY;
    }

    bool onSecurityRequest() override {
        Serial.println("[BLE] Security request — accepting");
        return true;
    }

    void onAuthenticationComplete(esp_ble_auth_cmpl_t auth_cmpl) override {
        if (auth_cmpl.success) {
            Serial.println("[BLE] Authentication complete — paired");
        } else {
            Serial.printf("[BLE] Authentication failed, reason: 0x%x\n", auth_cmpl.fail_reason);
        }
    }
};

class ServerCallbacks : public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) override {
        _deviceConnected = true;
        _isAdvertising = false;
        Serial.println("[BLE] Client connected");
    }
    void onDisconnect(BLEServer* pServer) override {
        _deviceConnected = false;
        _isAdvertising = false;
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

            // Persist to NVS (encrypted storage)
            if (wifi_save_credentials(ssid, pass)) {
                Serial.println("[BLE] WiFi credentials saved to NVS");
            }
        }
    }
};

void ble_server_init() {
    BLEDevice::init(BLE_DEVICE_NAME);

    // Enable BLE security with passkey authentication
    BLEDevice::setEncryptionLevel(ESP_BLE_SEC_ENCRYPT_MITM);
    BLEDevice::setSecurityCallbacks(new SecurityCallbacks());

    BLESecurity* security = new BLESecurity();
    security->setAuthenticationMode(ESP_LE_AUTH_REQ_SC_MITM_BOND);
    security->setCapability(ESP_IO_CAP_OUT);
    security->setInitEncryptionKey(ESP_BLE_ENC_KEY_MASK | ESP_BLE_ID_KEY_MASK);
    security->setStaticPIN(BLE_PASSKEY);

    _server = BLEDevice::createServer();
    _server->setCallbacks(new ServerCallbacks());

    BLEService* service = _server->createService(BLE_SERVICE_UUID);

    _statusChar = service->createCharacteristic(
        BLE_STATUS_CHAR_UUID,
        BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_NOTIFY);
    _statusChar->addDescriptor(new BLE2902());
    _statusChar->setAccessPermissions(ESP_GATT_PERM_READ_ENCRYPTED);
    _statusChar->setValue("{}");

    _commandChar = service->createCharacteristic(
        BLE_COMMAND_CHAR_UUID,
        BLECharacteristic::PROPERTY_WRITE);
    _commandChar->setAccessPermissions(ESP_GATT_PERM_WRITE_ENCRYPTED);
    _commandChar->setCallbacks(new CommandCallbacks());

    _wifiChar = service->createCharacteristic(
        BLE_WIFI_CHAR_UUID,
        BLECharacteristic::PROPERTY_WRITE);
    _wifiChar->setAccessPermissions(ESP_GATT_PERM_WRITE_ENCRYPTED);
    _wifiChar->setCallbacks(new WifiCallbacks());

    service->start();
    BLEAdvertising* advertising = BLEDevice::getAdvertising();
    advertising->addServiceUUID(BLE_SERVICE_UUID);
    advertising->setScanResponse(true);
    BLEDevice::startAdvertising();
    _isAdvertising = true;

    Serial.println("[OK] BLE server started (pairing required), advertising as " BLE_DEVICE_NAME);
}

void ble_server_update() {
    if (!_deviceConnected && !_isAdvertising) {
        BLEDevice::startAdvertising();
        _isAdvertising = true;
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
