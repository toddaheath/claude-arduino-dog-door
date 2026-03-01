# Collar NFC Protocol Specification

## Overview

The collar device emulates an NFC-A (ISO 14443-4) tag using the PN532 module. The door unit contains a PN532 reader that detects the collar and performs a mutual challenge-response authentication to cryptographically verify the dog's identity.

This protocol runs over NFC-DEP (Data Exchange Protocol) at 106 kbps with a maximum payload of 253 bytes per frame.

---

## 1. Protocol Layers

```
┌─────────────────────────────────────────┐
│  Application Layer                       │
│  (Challenge-Response Auth Protocol)      │
├─────────────────────────────────────────┤
│  Framing Layer                           │
│  (TLV-encoded messages)                  │
├─────────────────────────────────────────┤
│  NFC-DEP (ISO 18092)                     │
│  (Data Exchange Protocol)                │
├─────────────────────────────────────────┤
│  NFC-A (ISO 14443-4)                     │
│  (Physical transport at 106 kbps)        │
└─────────────────────────────────────────┘
```

---

## 2. Message Format

All messages use a TLV (Type-Length-Value) encoding:

```
Byte 0:     Message Type (1 byte)
Byte 1-2:   Payload Length (2 bytes, big-endian)
Byte 3..N:  Payload (variable)
Byte N+1-4: CRC32 (4 bytes, over type + length + payload)
```

### Message Types

| Type | Hex  | Name              | Direction        | Description |
|------|------|-------------------|------------------|-------------|
| 0x01 | 0x01 | COLLAR_ANNOUNCE   | Collar → Door    | Initial identification after NFC field detected |
| 0x02 | 0x02 | AUTH_CHALLENGE    | Door → Collar    | Random nonce for HMAC challenge |
| 0x03 | 0x03 | AUTH_RESPONSE     | Collar → Door    | HMAC-SHA256 of nonce using shared secret |
| 0x04 | 0x04 | AUTH_RESULT       | Door → Collar    | Success/failure + door status info |
| 0x05 | 0x05 | STATUS_REQUEST    | Door → Collar    | Request collar status (battery, GPS, etc.) |
| 0x06 | 0x06 | STATUS_RESPONSE   | Collar → Door    | Collar telemetry data |
| 0xFF | 0xFF | ERROR             | Either           | Protocol error with error code |

---

## 3. Handshake Sequence

### 3.1 Normal Authentication (Happy Path)

```
Time    Door (Reader)                         Collar (Tag)
─────────────────────────────────────────────────────────────
t=0     NFC field activated
        (PN532 polling for ISO 14443-4 tags)

t=5ms                                         Detected NFC field
                                              PN532 enters tag emulation mode

t=10ms  RATS → (Request for Answer to Select)
                                              ← ATS (Answer to Select)
                                                 Historical bytes: "SDDC01"
                                                 (Smart Dog Door Collar v01)

t=15ms                                        → COLLAR_ANNOUNCE
                                                 collar_id: [16 bytes]
                                                 protocol_version: 0x01
                                                 capabilities: 0x07
                                                   bit 0: GPS available
                                                   bit 1: IMU available
                                                   bit 2: Buzzer available
                                                 firmware_version: [4 bytes]

t=20ms  Lookup collar_id in local cache
        (or forward to API if unknown)
        Generate 32-byte random nonce

        AUTH_CHALLENGE →
          nonce: [32 bytes]
          door_id: [16 bytes]
          timestamp: [4 bytes, unix epoch]

t=30ms                                        Compute HMAC-SHA256:
                                                key = shared_secret
                                                msg = nonce ‖ door_id ‖ timestamp
                                              ← AUTH_RESPONSE
                                                hmac: [32 bytes]
                                                battery_pct: [1 byte]
                                                gps_fix: [1 byte] (0=no, 1=yes)
                                                rssi: [1 byte] (BLE signal strength)

t=35ms  Verify HMAC:
        - Retrieve shared_secret for collar_id
        - Compute expected HMAC
        - Constant-time compare

        AUTH_RESULT →
          status: 0x00 (AUTH_OK)
          door_open: [1 byte] (current state)
          access_granted: [1 byte]
          animal_name: [N bytes, UTF-8]

t=40ms                                        LED flash green (if access granted)
                                              or red (if denied)

Total: ~40ms for full handshake
```

### 3.2 Error Cases

**Unknown collar (not paired with this door's user):**
```
Door receives COLLAR_ANNOUNCE with unknown collar_id
Door sends AUTH_RESULT with status: 0x01 (UNKNOWN_COLLAR)
Door logs DoorEvent: UnknownAnimal with CollarId in notes
```

**HMAC verification failure (tampered or wrong secret):**
```
Door computes expected HMAC, does not match received HMAC
Door sends AUTH_RESULT with status: 0x02 (AUTH_FAILED)
Door logs DoorEvent: AccessDenied with "NFC auth failed" in notes
Door sends alert notification to owner
```

**Timeout (collar removed during handshake):**
```
Door waits 500ms for response, no response received
Door sends ERROR with code: 0x01 (TIMEOUT)
Door falls back to camera-only identification
```

**Protocol version mismatch:**
```
Collar announces protocol_version > door's supported version
Door sends ERROR with code: 0x02 (VERSION_MISMATCH)
Door includes its max supported version in error payload
```

---

## 4. Payload Specifications

### 4.1 COLLAR_ANNOUNCE (0x01)

```
Offset  Size  Field              Description
0       16    collar_id          128-bit unique collar identifier (hex)
16      1     protocol_version   Protocol version (currently 0x01)
17      1     capabilities       Bitmask of available features
18      4     firmware_version   Semantic version (major.minor.patch.build)
22      1     battery_pct        Battery percentage (0-100)
23      4     uptime_seconds     Seconds since last boot
```
Total: 27 bytes

### 4.2 AUTH_CHALLENGE (0x02)

```
Offset  Size  Field              Description
0       32    nonce              Cryptographically random challenge
32      16    door_id            Door unit identifier
48      4     timestamp          Unix epoch (seconds), for replay window check
52      2     challenge_flags    Bit 0: request_status (include telemetry in response)
```
Total: 54 bytes

### 4.3 AUTH_RESPONSE (0x03)

```
Offset  Size  Field              Description
0       32    hmac               HMAC-SHA256(nonce ‖ door_id ‖ timestamp, shared_secret)
32      1     battery_pct        Battery percentage (0-100)
33      1     gps_fix_status     0=no fix, 1=2D, 2=3D
34      4     latitude           IEEE 754 float (degrees)
38      4     longitude          IEEE 754 float (degrees)
42      2     speed_cmps         Speed in cm/s (0-65535)
44      1     satellites         Number of GPS satellites
45      1     hdop_tenths        HDOP * 10 (e.g., 15 = 1.5)
46      1     activity_state     0=idle, 1=walking, 2=running
47      4     steps_today        Step count since midnight
51      1     geofence_status    0=in bounds, 1=breach, 2=no fences
```
Total: 52 bytes

### 4.4 AUTH_RESULT (0x04)

```
Offset  Size  Field              Description
0       1     status             0x00=OK, 0x01=UNKNOWN, 0x02=AUTH_FAIL, 0x03=DENIED
1       1     door_open          Current door state (0=closed, 1=open)
2       1     access_granted     0=no, 1=yes (door will open)
3       1     name_length        Length of animal name (0 if unknown)
4       N     animal_name        UTF-8 encoded animal name (max 32 bytes)
```
Total: 4 + N bytes (max 36)

### 4.5 STATUS_REQUEST (0x05)

```
Offset  Size  Field              Description
0       1     request_flags      Bit 0: GPS, Bit 1: battery, Bit 2: activity, Bit 3: geofence
```
Total: 1 byte

### 4.6 STATUS_RESPONSE (0x06)

```
Offset  Size  Field              Description
0       1     response_flags     Mirror of request_flags (indicates which fields present)
--- If GPS flag set ---
1       4     latitude           IEEE 754 float
5       4     longitude          IEEE 754 float
9       4     altitude           IEEE 754 float (meters)
13      2     accuracy_cm        Horizontal accuracy in cm
15      2     speed_cmps         Speed in cm/s
17      2     heading_deg        Heading in degrees (0-359)
19      1     satellites         Satellite count
20      4     fix_timestamp      Unix epoch of last fix
--- If battery flag set ---
24      2     voltage_mv         Battery voltage in millivolts
26      1     percentage         0-100
27      1     charging           0=no, 1=yes
28      2     est_hours          Estimated hours remaining
--- If activity flag set ---
30      1     state              0=idle, 1=walking, 2=running
31      4     steps_today        Steps since midnight
35      4     distance_m         Distance in meters today
39      2     active_minutes     Active minutes today
--- If geofence flag set ---
41      1     in_bounds          0=no, 1=yes
42      2     nearest_fence_id   Fence ID
44      2     distance_cm        Distance to nearest fence boundary in cm
46      1     breach_count       Breaches today
```
Total: variable (max ~47 bytes)

### 4.7 ERROR (0xFF)

```
Offset  Size  Field              Description
0       1     error_code         0x01=TIMEOUT, 0x02=VERSION, 0x03=MALFORMED, 0x04=BUSY
1       1     detail_length      Length of detail string
2       N     detail             UTF-8 error description (max 64 bytes)
```
Total: 2 + N bytes (max 66)

---

## 5. Security Considerations

### 5.1 Shared Secret Management

- **Generation**: 256-bit secret generated server-side using `RandomNumberGenerator.GetBytes(32)`
- **Storage on collar**: ESP32-S3 NVS with flash encryption enabled (eFuse-backed AES-256)
- **Storage on API**: BCrypt hash of the secret (the raw secret is shown once during pairing, then discarded)
- **Rotation**: Owner can rotate the secret from the SPA; collar re-provisions via BLE or WiFi

### 5.2 Replay Protection

- The `timestamp` field in AUTH_CHALLENGE limits replay window to +/- 30 seconds
- The collar rejects challenges where `|collar_time - challenge_timestamp| > 30`
- Since NFC has a range of ~4cm, physical replay is impractical, but the timestamp guard prevents captured-and-replayed NFC frames

### 5.3 Brute Force Prevention

- After 5 consecutive AUTH_FAIL results for the same collar_id, the door enters a 60-second cooldown before attempting NFC auth again for that collar
- The API logs all auth failures and can trigger owner notifications

### 5.4 Constant-Time Comparison

Both door firmware and API use constant-time HMAC comparison to prevent timing side-channels:

```c
// Door firmware (C++)
bool constant_time_compare(const uint8_t* a, const uint8_t* b, size_t len) {
    volatile uint8_t result = 0;
    for (size_t i = 0; i < len; i++) {
        result |= a[i] ^ b[i];
    }
    return result == 0;
}
```

```csharp
// API (C#)
CryptographicOperations.FixedTimeEquals(expected, received);
```

---

## 6. Door Firmware Integration

### 6.1 NFC Reader Initialization

```c
// nfc_reader.cpp
#include <Wire.h>
#include <Adafruit_PN532.h>

#define NFC_IRQ_PIN    XX  // TBD based on available GPIO
#define NFC_RESET_PIN  XX

Adafruit_PN532 nfc(NFC_IRQ_PIN, NFC_RESET_PIN);

void nfc_init() {
    nfc.begin();
    uint32_t version = nfc.getFirmwareVersion();
    if (!version) {
        Serial.println("[NFC] PN532 not found - collar auth disabled");
        return;
    }
    nfc.SAMConfig();  // Configure Secure Access Module
    nfc.setPassiveActivationRetries(0x01);  // Quick timeout for polling
    Serial.printf("[NFC] PN532 ready (firmware %d.%d)\n",
                  (version >> 24) & 0xFF, (version >> 16) & 0xFF);
}
```

### 6.2 Integration into Access Pipeline

```c
// In main.cpp loop, after ultrasonic confirms proximity:

AccessResult process_access(CameraImage& img, const char* side) {
    AccessResult result;
    result.method = "camera";  // default

    // Parallel: start camera capture AND check for collar
    capture_image(img);
    CollarInfo collar = nfc_scan_collar(500);  // 500ms timeout

    if (collar.found) {
        NfcAuthResult auth = nfc_authenticate(collar);
        if (auth.success) {
            result.collar_id = collar.collar_id;
            result.collar_hmac = auth.hmac;
            result.collar_nonce = auth.nonce;
            result.method = "both";
            result.collar_battery = auth.battery_pct;
        }
        // Auth failure: continue with camera-only, log the failure
    }

    // Send to API with both image and collar data (if available)
    ApiAccessResponse api_resp = api_access_request(
        img, side, result.collar_id, result.collar_nonce, result.collar_hmac
    );

    result.allowed = api_resp.allowed;
    result.animal_name = api_resp.animal_name;
    result.confidence = api_resp.confidence;
    return result;
}
```

---

## 7. Collar Firmware NFC Tag Emulation

```c
// nfc_manager.cpp (collar side)
#include <Wire.h>
#include <Adafruit_PN532.h>
#include <mbedtls/md.h>

// PN532 in tag emulation mode
void nfc_emulate_tag() {
    uint8_t cmd[64];
    uint8_t response[64];
    uint8_t cmd_len;

    // Wait for reader field (blocking, wakes from light sleep via IRQ)
    if (!nfc.inListPassiveTarget()) return;

    // Read incoming command
    if (!nfc.tgGetData(cmd, &cmd_len)) return;

    switch (cmd[0]) {
        case MSG_AUTH_CHALLENGE: {
            // Parse nonce (32 bytes), door_id (16 bytes), timestamp (4 bytes)
            uint8_t* nonce = &cmd[3];       // Skip TLV header
            uint8_t* door_id = &cmd[35];
            uint32_t timestamp = (cmd[51] << 24) | (cmd[52] << 16) |
                                 (cmd[53] << 8) | cmd[54];

            // Validate timestamp (within 30s of our clock)
            uint32_t now = (uint32_t)(time(NULL));
            if (abs((int32_t)(now - timestamp)) > 30) {
                nfc_send_error(ERR_TIMEOUT, "timestamp out of range");
                return;
            }

            // Compute HMAC-SHA256(nonce ‖ door_id ‖ timestamp, shared_secret)
            uint8_t msg[52];
            memcpy(msg, nonce, 32);
            memcpy(msg + 32, door_id, 16);
            memcpy(msg + 48, &cmd[51], 4);

            uint8_t hmac[32];
            compute_hmac_sha256(shared_secret, 32, msg, 52, hmac);

            // Build AUTH_RESPONSE
            build_auth_response(response, hmac, battery_pct,
                                gps_fix_status, lat, lng, speed,
                                satellites, hdop, activity_state,
                                steps_today, geofence_status);

            nfc.tgSetData(response, AUTH_RESPONSE_LEN);
            break;
        }

        case MSG_STATUS_REQUEST: {
            build_status_response(response, cmd[3]);  // request_flags
            nfc.tgSetData(response, status_len);
            break;
        }
    }
}

void compute_hmac_sha256(const uint8_t* key, size_t key_len,
                         const uint8_t* msg, size_t msg_len,
                         uint8_t* output) {
    mbedtls_md_context_t ctx;
    mbedtls_md_init(&ctx);
    mbedtls_md_setup(&ctx, mbedtls_md_info_from_type(MBEDTLS_MD_SHA256), 1);
    mbedtls_md_hmac_starts(&ctx, key, key_len);
    mbedtls_md_hmac_update(&ctx, msg, msg_len);
    mbedtls_md_hmac_finish(&ctx, output);
    mbedtls_md_free(&ctx);
}
```

---

## 8. Testing Strategy

### 8.1 Unit Tests (Collar Firmware)
- HMAC computation with known test vectors (RFC 4231)
- TLV message encoding/decoding roundtrip
- Timestamp validation (in range, out of range, boundary)
- CRC32 computation and verification

### 8.2 Integration Tests (Door + Collar)
- Two PN532 modules on bench: one reader, one emulator
- Full handshake with valid credentials → AUTH_OK
- Handshake with wrong secret → AUTH_FAILED
- Handshake with unknown collar_id → UNKNOWN_COLLAR
- Timeout simulation (remove collar mid-handshake)
- 100 rapid successive handshakes (stress test)

### 8.3 API Integration Tests
- POST /collars/{id}/verify with valid HMAC → 200 + animal info
- POST /collars/{id}/verify with invalid HMAC → 401
- POST /doors/access-request with both image + collar fields → fused confidence
- POST /doors/access-request with collar only (no image) → collar-only confidence
- POST /doors/access-request with image only (no collar) → existing behavior unchanged
