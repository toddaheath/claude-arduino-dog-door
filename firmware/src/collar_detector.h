#ifndef COLLAR_DETECTOR_H
#define COLLAR_DETECTOR_H

#include <Arduino.h>

/// Result of BLE scan for nearby collar
struct CollarScanResult {
    bool found;
    char collarId[33];   // 32 hex chars + null
    int rssi;
    bool nfcVerified;    // true if NFC challenge-response succeeded
};

/// Initialize BLE scanning for collar detection
void collar_detector_init();

/// Scan for nearby collar BLE advertisements.
/// Returns scan result with strongest collar found (by RSSI).
/// Blocks for COLLAR_BLE_SCAN_DURATION_MS.
CollarScanResult collar_detector_scan();

/// Attempt NFC verification with a detected collar (if NFC reader enabled).
/// challenge: random bytes from door
/// response: HMAC response from collar
/// Returns true if NFC read succeeded (verification done server-side).
bool collar_nfc_read(const char* collarId, char* challenge, char* response);

#endif // COLLAR_DETECTOR_H
