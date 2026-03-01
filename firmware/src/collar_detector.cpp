#include "collar_detector.h"
#include "config.h"

#ifndef UNIT_TEST

#if COLLAR_BLE_SCAN_ENABLED

#include <NimBLEDevice.h>

static NimBLEScan* pScan = nullptr;
static NimBLEUUID collarServiceUUID(COLLAR_BLE_SERVICE_UUID);

// Scan callback that collects collar advertisements
class CollarScanCallbacks : public NimBLEScanCallbacks {
public:
    bool found = false;
    char bestCollarId[33] = {0};
    int bestRssi = -999;

    void onResult(const NimBLEAdvertisedDevice* device) override {
        if (!device->isAdvertisingService(collarServiceUUID)) return;

        int rssi = device->getRSSI();
        if (rssi < COLLAR_RSSI_MIN) return;

        if (rssi > bestRssi) {
            bestRssi = rssi;
            found = true;
            // Collar ID is in manufacturer data (first 32 bytes as hex)
            if (device->haveManufacturerData()) {
                auto mfgData = device->getManufacturerData();
                // First 16 bytes of manufacturer data = collar ID (as raw bytes)
                size_t len = mfgData.length() < 16 ? mfgData.length() : 16;
                for (size_t i = 0; i < len; i++) {
                    snprintf(&bestCollarId[i * 2], 3, "%02x",
                             (uint8_t)mfgData[i]);
                }
                bestCollarId[len * 2] = '\0';
            }
        }
    }
};

void collar_detector_init() {
    // NimBLE already initialized by ble_server; just get the scan object
    pScan = NimBLEDevice::getScan();
    pScan->setActiveScan(true);
    pScan->setInterval(100);
    pScan->setWindow(99);
}

CollarScanResult collar_detector_scan() {
    CollarScanResult result = {false, {0}, 0, false};

    if (!pScan) {
        collar_detector_init();
        if (!pScan) return result;
    }

    CollarScanCallbacks callbacks;
    pScan->setScanCallbacks(&callbacks, false);
    pScan->start(COLLAR_BLE_SCAN_DURATION_MS / 1000, false);

    if (callbacks.found) {
        result.found = true;
        strncpy(result.collarId, callbacks.bestCollarId, sizeof(result.collarId) - 1);
        result.rssi = callbacks.bestRssi;
    }

    pScan->clearResults();
    return result;
}

#else // COLLAR_BLE_SCAN_ENABLED == 0

void collar_detector_init() {}

CollarScanResult collar_detector_scan() {
    return {false, {0}, 0, false};
}

#endif // COLLAR_BLE_SCAN_ENABLED

// NFC support (optional, requires PN532 wired to door controller)
#if COLLAR_NFC_ENABLED
#include <Wire.h>
#include <Adafruit_PN532.h>

static Adafruit_PN532 nfc(COLLAR_NFC_SDA_PIN, COLLAR_NFC_SCL_PIN);
static bool nfcInitialized = false;

bool collar_nfc_read(const char* collarId, char* challenge, char* response) {
    if (!nfcInitialized) {
        nfc.begin();
        uint32_t versiondata = nfc.getFirmwareVersion();
        if (!versiondata) return false;
        nfc.SAMConfig();
        nfcInitialized = true;
    }

    uint8_t uid[7];
    uint8_t uidLength;
    if (!nfc.readPassiveTargetID(PN532_MIFARE_ISO14443A, uid, &uidLength, 1000))
        return false;

    // In a full implementation, the door would:
    // 1. Generate random challenge bytes
    // 2. Send them to the collar via NFC APDU
    // 3. Read back the HMAC response
    // 4. Return challenge + response for server-side verification
    // For now, return the UID as a simple presence check
    for (uint8_t i = 0; i < uidLength && i < 16; i++) {
        snprintf(&challenge[i * 2], 3, "%02x", uid[i]);
    }
    challenge[uidLength * 2] = '\0';

    // Response placeholder — server does actual HMAC verification
    strcpy(response, "nfc_present");
    return true;
}

#else // COLLAR_NFC_ENABLED == 0

bool collar_nfc_read(const char*, char*, char*) {
    return false;
}

#endif // COLLAR_NFC_ENABLED

#endif // UNIT_TEST
