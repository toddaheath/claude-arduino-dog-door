# Door-Side Integration for Collar Support

## Overview

This document details the changes needed to the existing ESP32-CAM door firmware to support collar-based identification alongside the existing camera pipeline.

---

## 1. Door Hardware Additions

### 1.1 PN532 NFC Reader Module

The PN532 is added to each door unit (inside and outside) to read the collar's NFC tag as the dog approaches.

**Wiring to ESP32-CAM:**

```
ESP32-CAM (Door Unit)
│
├── Existing GPIO usage:
│   ├── GPIO12 ← RCWL-0516 (radar)
│   ├── GPIO13 → HC-SR04 (trigger)
│   ├── GPIO14 ← HC-SR04 (echo)
│   ├── GPIO15 ← IR Break Beam
│   ├── GPIO2  → L298N (IN1)
│   ├── GPIO4  → L298N (IN2)
│   ├── GPIO16 ← Reed Switch
│   ├── GPIO33 → LED (green)
│   └── GPIO32 → LED (red)
│
├── New NFC Reader:
│   ├── GPIO1 (TX) → PN532 SDA (I2C mode, acting as SDA via software I2C)
│   ├── GPIO3 (RX) → PN532 SCL (I2C mode, acting as SCL via software I2C)
│   └── (NOTE: GPIO1/3 are UART0 TX/RX — we use software I2C to avoid conflict)
│
│   Alternative: Use SPI mode with existing unused pins
│   ├── GPIO0  → PN532 SS   (CAUTION: GPIO0 is boot pin, must be HIGH at boot)
│   ├── VSPI already used by camera — use HSPI:
│   │   This approach is complex. Recommended: use software I2C on any 2 free GPIOs.
│
└── Recommended approach: Dedicated I2C on GPIO1 + GPIO3
    (UART0 disabled after boot; Serial debug via USB or BLE instead)
```

**Challenge:** The ESP32-CAM (AI-Thinker) has very limited free GPIOs. Most are consumed by the camera, PSRAM, and flash.

**Practical options:**

| Option | Pins Used | Trade-off |
|--------|-----------|-----------|
| A: Repurpose UART0 | GPIO1, GPIO3 | Lose serial debug output after boot |
| B: Repurpose LEDs | GPIO32, GPIO33 | Lose status LEDs (can use NeoPixel on one pin instead) |
| C: External I2C expander | GPIO32 (one pin for 1-wire I2C) | Adds cost + complexity |
| D: Use second ESP32 | SPI/UART to PN532 | Dedicated NFC co-processor; highest cost |

**Recommended: Option A** — Repurpose UART0 pins (GPIO1/3) for software I2C to the PN532. Serial debug is redirected to BLE or disabled after boot. This is the simplest approach with zero additional cost.

```
PN532 Module (I2C mode)
├── VCC  → 3.3V (from buck converter)
├── GND  → GND
├── SDA  → ESP32 GPIO1 (software I2C)
├── SCL  → ESP32 GPIO3 (software I2C)
├── IRQ  → Not connected (polling mode; no free interrupt pin)
└── RSTO → Tied HIGH (no reset control)

Set PN532 DIP switches: SEL0=ON, SEL1=OFF (I2C mode)
```

### 1.2 Updated Door BOM (Additional Items)

| # | Item | Purpose | Est. Cost |
|---|------|---------|-----------|
| 13 | PN532 NFC/RFID Module | Read collar NFC tag | $6 |
| 14 | 4.7kΩ pull-up resistors (x2) | I2C pull-ups for PN532 | $0.10 |
| | **Additional total** | | **~$6.10** |

Updated total door cost: **~$90** (was ~$84).

---

## 2. Door Firmware Changes

### 2.1 New Files

```
firmware/src/
├── nfc_reader.cpp/h       # PN532 I2C reader, challenge-response initiator
├── collar_scanner.cpp/h   # BLE scan for collar proximity detection
└── (updated existing files)
```

### 2.2 config.h Updates

```c
// ── NFC Reader (PN532) ───────────────────────────────────────
#define NFC_READER_ENABLED    true
#define NFC_SDA_PIN           1     // Software I2C (repurposed UART0 TX)
#define NFC_SCL_PIN           3     // Software I2C (repurposed UART0 RX)
#define NFC_POLL_INTERVAL_MS  200   // Poll for NFC tag every 200ms when BLE proximity detected
#define NFC_HANDSHAKE_TIMEOUT 500   // 500ms for full challenge-response

// ── Collar BLE Scanning ──────────────────────────────────────
#define COLLAR_BLE_SERVICE_UUID  "5a6d0001-8e7f-4b3c-9d2a-1c6e3f8b7d4e"
#define COLLAR_BLE_RSSI_THRESHOLD -60  // dBm; collar within ~2m
#define COLLAR_BLE_SCAN_INTERVAL_MS 2000  // Scan every 2s
#define COLLAR_BLE_SCAN_DURATION_MS 500   // Each scan lasts 500ms

// ── Fused Identification ─────────────────────────────────────
#define FUSION_COLLAR_ONLY_CONFIDENCE 0.95
#define FUSION_BOOST 0.15              // Camera confidence boost when collar agrees
#define FUSION_DISAGREE_PENALTY 0.5    // Multiply confidence when camera & collar disagree
```

### 2.3 NFC Reader Module

```c
// firmware/src/nfc_reader.h
#pragma once

#include <stdint.h>

typedef struct {
    bool found;
    char collar_id[33];       // 32 hex chars + null
    uint8_t battery_pct;
    uint8_t gps_fix_status;
    float latitude;
    float longitude;
} CollarNfcInfo;

typedef struct {
    bool success;
    char collar_id[33];
    uint8_t nonce[32];
    uint8_t hmac[32];
    uint8_t battery_pct;
} NfcAuthResult;

void nfc_reader_init();
CollarNfcInfo nfc_scan_collar(uint32_t timeout_ms);
NfcAuthResult nfc_authenticate(const CollarNfcInfo& collar);
```

```c
// firmware/src/nfc_reader.cpp

#include "nfc_reader.h"
#include "config.h"
#include <SoftWire.h>  // Software I2C library
#include <Adafruit_PN532.h>
#include <mbedtls/entropy.h>
#include <mbedtls/ctr_drbg.h>

// Software I2C for PN532 (GPIO1/3)
static SoftWire swire(NFC_SDA_PIN, NFC_SCL_PIN);
static Adafruit_PN532 nfc_reader(/* I2C via swire */);
static bool nfc_initialized = false;

// Random number generator for challenge nonces
static mbedtls_entropy_context entropy;
static mbedtls_ctr_drbg_context ctr_drbg;

void nfc_reader_init() {
    // Disable UART0 (we're repurposing its pins)
    Serial.end();

    // Initialize software I2C
    swire.begin();

    // Initialize PN532
    nfc_reader.begin();
    uint32_t version = nfc_reader.getFirmwareVersion();
    if (!version) {
        // PN532 not found — collar support disabled
        nfc_initialized = false;
        return;
    }

    nfc_reader.SAMConfig();
    nfc_reader.setPassiveActivationRetries(1);  // Quick timeout

    // Initialize RNG for nonce generation
    mbedtls_entropy_init(&entropy);
    mbedtls_ctr_drbg_init(&ctr_drbg);
    mbedtls_ctr_drbg_seed(&ctr_drbg, mbedtls_entropy_func, &entropy, NULL, 0);

    nfc_initialized = true;
}

CollarNfcInfo nfc_scan_collar(uint32_t timeout_ms) {
    CollarNfcInfo info = { .found = false };
    if (!nfc_initialized) return info;

    uint8_t uid[7];
    uint8_t uid_len;

    // Try to detect an ISO 14443-4 tag
    if (!nfc_reader.inListPassiveTarget(uid, &uid_len, timeout_ms)) {
        return info;  // No tag found
    }

    // Tag found — read COLLAR_ANNOUNCE
    uint8_t response[64];
    uint8_t response_len = sizeof(response);

    if (nfc_reader.inDataExchange(NULL, 0, response, &response_len)) {
        if (response[0] == 0x01 && response_len >= 27) {
            // Parse COLLAR_ANNOUNCE
            info.found = true;

            // collar_id: bytes 3-18 (16 bytes → 32 hex chars)
            for (int i = 0; i < 16; i++) {
                sprintf(&info.collar_id[i * 2], "%02x", response[3 + i]);
            }
            info.collar_id[32] = '\0';

            info.battery_pct = response[25];
        }
    }

    return info;
}

NfcAuthResult nfc_authenticate(const CollarNfcInfo& collar) {
    NfcAuthResult result = { .success = false };
    if (!nfc_initialized || !collar.found) return result;

    // Copy collar_id
    memcpy(result.collar_id, collar.collar_id, 33);

    // Generate 32-byte random nonce
    mbedtls_ctr_drbg_random(&ctr_drbg, result.nonce, 32);

    // Build AUTH_CHALLENGE message
    uint8_t challenge[57];  // TLV header (3) + nonce (32) + door_id (16) + timestamp (4) + flags (2)
    challenge[0] = 0x02;     // MSG_AUTH_CHALLENGE
    challenge[1] = 0x00;
    challenge[2] = 54;       // Payload length

    memcpy(&challenge[3], result.nonce, 32);    // nonce
    memcpy(&challenge[35], door_id, 16);         // door_id (from NVS)

    uint32_t now = (uint32_t)time(NULL);
    challenge[51] = (now >> 24) & 0xFF;
    challenge[52] = (now >> 16) & 0xFF;
    challenge[53] = (now >> 8) & 0xFF;
    challenge[54] = now & 0xFF;

    challenge[55] = 0x01;  // Flags: request_status
    challenge[56] = 0x00;

    // Send AUTH_CHALLENGE, receive AUTH_RESPONSE
    uint8_t response[64];
    uint8_t response_len = sizeof(response);

    if (!nfc_reader.inDataExchange(challenge, sizeof(challenge), response, &response_len)) {
        return result;  // Communication error
    }

    if (response[0] != 0x03 || response_len < 52) {
        return result;  // Invalid response
    }

    // Parse AUTH_RESPONSE
    memcpy(result.hmac, &response[3], 32);   // HMAC (for API verification)
    result.battery_pct = response[35];
    result.success = true;

    // Send AUTH_RESULT (we'll get the actual decision from the API)
    // For now, send a preliminary ACK
    uint8_t ack[7];
    ack[0] = 0x04;  // MSG_AUTH_RESULT
    ack[1] = 0x00;
    ack[2] = 4;     // Payload length
    ack[3] = 0x00;  // Status: OK (preliminary)
    ack[4] = 0x00;  // Door state (will update after API response)
    ack[5] = 0x00;  // Access (pending)
    ack[6] = 0x00;  // Name length: 0

    nfc_reader.inDataExchange(ack, sizeof(ack), NULL, NULL);

    return result;
}
```

### 2.4 Collar BLE Scanner

```c
// firmware/src/collar_scanner.h
#pragma once

typedef struct {
    bool detected;
    int rssi;
    char collar_id_prefix[9];  // First 8 hex chars of collar ID (from BLE name)
} CollarBleInfo;

void collar_scanner_init();
CollarBleInfo collar_scan();
```

```c
// firmware/src/collar_scanner.cpp

#include "collar_scanner.h"
#include "config.h"
#include <BLEDevice.h>
#include <BLEScan.h>

static BLEScan* pBLEScan = nullptr;

void collar_scanner_init() {
    // BLE already initialized by ble_server.cpp
    pBLEScan = BLEDevice::getScan();
    pBLEScan->setActiveScan(false);  // Passive scan
    pBLEScan->setInterval(100);
    pBLEScan->setWindow(50);
}

CollarBleInfo collar_scan() {
    CollarBleInfo info = { .detected = false, .rssi = -100 };

    BLEScanResults results = pBLEScan->start(1, false);  // 1 second scan

    for (int i = 0; i < results.getCount(); i++) {
        BLEAdvertisedDevice device = results.getDevice(i);

        // Check if this device advertises the collar BLE service UUID
        if (device.haveServiceUUID() &&
            device.isAdvertisingService(BLEUUID(COLLAR_BLE_SERVICE_UUID))) {

            info.detected = true;
            info.rssi = device.getRSSI();

            // Extract collar ID prefix from device name
            // Name format: "SDD-Collar-a1b2c3d4"
            String name = device.getName().c_str();
            if (name.startsWith("SDD-Collar-")) {
                String prefix = name.substring(11, 19);
                strncpy(info.collar_id_prefix, prefix.c_str(), 8);
                info.collar_id_prefix[8] = '\0';
            }

            break;  // Found a collar
        }
    }

    pBLEScan->clearResults();
    return info;
}
```

### 2.5 Updated Main Loop (Access Pipeline)

The main loop in `main.cpp` is updated to include collar detection in the access pipeline:

```c
// In the motion-detected → capture → identify flow:

void process_approach() {
    // Stage 1: Radar detected motion (existing)
    // Stage 2: Ultrasonic confirms proximity (existing)
    // Stage 3: Check for collar via BLE (NEW — parallel with camera capture)

    CollarBleInfo ble_info = { .detected = false };
    NfcAuthResult nfc_result = { .success = false };

    if (NFC_READER_ENABLED) {
        ble_info = collar_scan();

        if (ble_info.detected && ble_info.rssi > COLLAR_BLE_RSSI_THRESHOLD) {
            // Collar is close enough — try NFC handshake
            CollarNfcInfo nfc_info = nfc_scan_collar(NFC_HANDSHAKE_TIMEOUT);
            if (nfc_info.found) {
                nfc_result = nfc_authenticate(nfc_info);
            }
        }
    }

    // Stage 4: Capture image (existing)
    camera_fb_t* fb = esp_camera_fb_get();
    if (!fb) {
        Serial.println("[CAM] Capture failed");
        return;
    }

    // Stage 5: On-device TFLite inference (existing)
    float dog_confidence = tflite_detect_dog(fb->buf, fb->len);

    // Stage 6: Upload approach photo (existing)
    api_upload_approach(fb, current_side);

    // Stage 7: Send access request with both camera + collar data (UPDATED)
    AccessResponse resp;
    if (nfc_result.success) {
        // Include collar NFC data in access request
        resp = api_access_request_with_collar(
            fb, current_side,
            nfc_result.collar_id,
            nfc_result.nonce,
            nfc_result.hmac,
            nfc_result.battery_pct
        );
    } else {
        // Camera-only access request (existing behavior)
        resp = api_access_request(fb, current_side);
    }

    esp_camera_fb_return(fb);

    // Stage 8: Act on decision (existing, with enhanced logging)
    if (resp.allowed) {
        Serial.printf("[ACCESS] GRANTED: %s (method: %s, confidence: %.2f)\n",
                      resp.animal_name, resp.identification_method, resp.confidence);
        open_door();
    } else {
        Serial.printf("[ACCESS] DENIED: %s (method: %s, confidence: %.2f)\n",
                      resp.animal_name ? resp.animal_name : "unknown",
                      resp.identification_method, resp.confidence);
        flash_red_led();
    }
}
```

### 2.6 API Client Update

```c
// firmware/src/api_client.cpp — additions

AccessResponse api_access_request_with_collar(
    camera_fb_t* fb, const char* side,
    const char* collar_id, const uint8_t* nonce,
    const uint8_t* hmac, uint8_t battery_pct
) {
    // Build multipart form data (existing image fields + new collar fields)
    // ... existing image upload code ...

    // Add collar fields as additional form parts
    http.addFormField("collarId", collar_id);

    // Base64-encode nonce and HMAC
    char nonce_b64[48], hmac_b64[48];
    base64_encode(nonce, 32, nonce_b64);
    base64_encode(hmac, 32, hmac_b64);

    http.addFormField("nonce", nonce_b64);
    http.addFormField("hmacResponse", hmac_b64);
    http.addFormField("collarBattery", String(battery_pct).c_str());

    // ... existing POST and response parsing ...

    // Parse enhanced response fields
    AccessResponse resp;
    resp.allowed = doc["allowed"];
    resp.animal_id = doc["animalId"];
    resp.animal_name = doc["animalName"];
    resp.confidence = doc["confidenceScore"];
    resp.identification_method = doc["identificationMethod"];  // NEW
    resp.camera_confidence = doc["cameraConfidence"];          // NEW
    resp.collar_confidence = doc["collarConfidence"];          // NEW

    return resp;
}
```

---

## 3. Backward Compatibility

The collar integration is fully backward-compatible:

1. **No collar → existing behavior**: If no collar is detected (NFC fields are null/empty in the access request), the API processes the request exactly as before using camera-only identification.

2. **PN532 not installed → collar features disabled**: If `NFC_READER_ENABLED` is `false` or the PN532 is not detected at boot, the door firmware skips all collar-related code paths.

3. **Collar without camera match → collar-only identification**: If the camera can't identify the dog but the collar NFC is verified, the API uses collar-only confidence (0.95).

4. **API version compatibility**: New fields in AccessRequestDto and AccessResponseDto are optional/nullable. Older firmware versions that don't send collar fields will work unchanged.

---

## 4. Timing Budget

The collar detection adds time to the access pipeline. Here's the impact:

| Stage | Without Collar | With Collar (found) | With Collar (not found) |
|-------|---------------|--------------------|-----------------------|
| BLE scan | — | 500ms | 500ms |
| NFC detect | — | 10ms | — |
| NFC handshake | — | 40ms | — |
| Camera capture | 200ms | 200ms (parallel) | 200ms |
| TFLite inference | 300ms | 300ms | 300ms |
| API request | 200ms | 250ms (+collar data) | 200ms |
| **Total** | **~700ms** | **~800ms** | **~900ms** |

The BLE scan runs in parallel with camera capture and TFLite inference when possible. Worst case (collar scan with no collar found) adds ~200ms to the pipeline.

---

## 5. Testing Strategy

### 5.1 Door Firmware Tests

- NFC reader initialization (PN532 found/not found)
- BLE scan for collar advertisements (found at various RSSI levels)
- NFC challenge-response with mock collar
- Access pipeline with collar data (fused confidence calculation)
- Access pipeline without collar (backward compatibility)
- GPIO conflict verification (software I2C doesn't interfere with camera/sensors)

### 5.2 API Integration Tests

```csharp
// New test cases for CollarController and enhanced DoorService

[Fact]
public async Task AccessRequest_WithValidCollar_ReturnsBothMethodConfidence()
{
    // Arrange: Register collar, upload reference photos
    // Act: POST /doors/access-request with image + collar NFC fields
    // Assert: Response has identificationMethod="both", boosted confidence
}

[Fact]
public async Task AccessRequest_WithCollarOnly_ReturnsCollarConfidence()
{
    // Arrange: Register collar, no reference photos match
    // Act: POST /doors/access-request with collar NFC fields + non-matching image
    // Assert: Response uses collar confidence (0.95)
}

[Fact]
public async Task AccessRequest_WithInvalidHmac_DeniesAccess()
{
    // Act: POST /doors/access-request with wrong HMAC
    // Assert: collar_confidence is 0, falls back to camera-only
}

[Fact]
public async Task AccessRequest_WithoutCollar_ExistingBehaviorUnchanged()
{
    // Act: POST /doors/access-request without collar fields
    // Assert: Same response as before collar feature
}

[Fact]
public async Task CollarVerify_WithValidHmac_ReturnsAnimalInfo()
{
    // Act: POST /collars/{id}/verify with correct HMAC
    // Assert: verified=true, animalId, animalName
}

[Fact]
public async Task CollarVerify_WithReplayedTimestamp_Rejects()
{
    // Act: POST /collars/{id}/verify with timestamp > 30s old
    // Assert: verified=false, reason="timestamp_expired"
}
```
