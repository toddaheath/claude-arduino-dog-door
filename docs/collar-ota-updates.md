# Collar OTA Firmware Update System

## Overview

Over-the-air (OTA) firmware updates for the collar device, managed through the API and delivered via WiFi during the collar's periodic upload bursts.

---

## 1. OTA Architecture

```
Developer                API                        Collar
    │                     │                            │
    │── Upload firmware   │                            │
    │   binary (.bin)     │                            │
    │   + version string  │                            │
    │   + release notes   │                            │
    │                     │                            │
    │                     │── Store binary in          │
    │                     │   /data/firmware/           │
    │                     │                            │
    │                     │         (later, during WiFi upload burst)
    │                     │                            │
    │                     │◀── GET /collars/{id}/      │
    │                     │    firmware?current=1.0.0   │
    │                     │                            │
    │                     │── 200 { version: "1.1.0",  │
    │                     │        size: 1048576,       │
    │                     │        sha256: "abc..." }   │
    │                     │                            │
    │                     │◀── GET /collars/{id}/      │
    │                     │    firmware/download        │
    │                     │                            │
    │                     │── 200 [binary stream] ─────▶│
    │                     │                            │── Verify SHA256
    │                     │                            │── Write to OTA partition
    │                     │                            │── Set boot partition
    │                     │                            │── Reboot
    │                     │                            │
    │                     │◀── POST /collars/{id}/     │
    │                     │    firmware/confirm         │
    │                     │    { version: "1.1.0" }    │
    │                     │                            │
```

---

## 2. API Endpoints

### POST /api/v1/admin/firmware/collar — Upload New Firmware

Admin-only endpoint for uploading firmware binaries.

**Request:** `multipart/form-data`
```
binary: collar_firmware_v1.1.0.bin (file)
version: "1.1.0"
releaseNotes: "Added geofence corridor support, fixed GPS cold start timeout"
minBatteryPct: 30        # Minimum battery to allow update
forceUpdate: false        # If true, collar updates immediately on next connect
targetCollars: []         # Empty = all collars; specific IDs for staged rollout
```

**Response (201 Created):**
```json
{
    "id": 3,
    "version": "1.1.0",
    "size": 1048576,
    "sha256": "abc123...",
    "releaseNotes": "Added geofence corridor support, fixed GPS cold start timeout",
    "uploadedAt": "2026-03-01T10:00:00Z",
    "targetCollars": [],
    "deployedCount": 0,
    "totalTargetCount": 5
}
```

### GET /api/v1/collars/{id}/firmware — Check for Updates

Called by the collar during WiFi upload burst.

**Query Parameters:**
| Param | Type | Description |
|-------|------|-------------|
| current | string | Current firmware version on collar |

**Response (200 OK) — Update available:**
```json
{
    "updateAvailable": true,
    "version": "1.1.0",
    "size": 1048576,
    "sha256": "abc123def456...",
    "releaseNotes": "Added geofence corridor support",
    "minBatteryPct": 30,
    "forceUpdate": false
}
```

**Response (204 No Content):** No update available (collar is on latest).

### GET /api/v1/collars/{id}/firmware/download — Download Firmware Binary

Returns the raw firmware binary for streaming to the OTA partition.

**Response (200 OK):**
```
Content-Type: application/octet-stream
Content-Length: 1048576
X-Firmware-Version: 1.1.0
X-Firmware-SHA256: abc123def456...
```

### POST /api/v1/collars/{id}/firmware/confirm — Confirm Successful Update

Called by the collar after rebooting on the new firmware to confirm it's working.

**Request:**
```json
{
    "version": "1.1.0",
    "bootCount": 1
}
```

**Response (200 OK):**
```json
{
    "confirmed": true
}
```

---

## 3. Collar-Side OTA Implementation

### 3.1 Update Decision Logic

```c
bool should_update(const OtaInfo& info) {
    // Battery check
    float battery = power_get_percentage();
    if (battery < info.min_battery_pct) {
        Serial.printf("[OTA] Battery too low: %.0f%% < %d%%\n",
                      battery, info.min_battery_pct);
        return false;
    }

    // Version check (semantic versioning comparison)
    if (compare_versions(COLLAR_FW_VERSION, info.version) >= 0) {
        Serial.println("[OTA] Already on latest version");
        return false;
    }

    // WiFi signal strength check
    if (WiFi.RSSI() < -70) {
        Serial.printf("[OTA] WiFi signal too weak: %d dBm\n", WiFi.RSSI());
        return false;
    }

    return true;
}
```

### 3.2 OTA Download & Flash

```c
#include <Update.h>
#include <HTTPClient.h>
#include <mbedtls/sha256.h>

bool perform_ota_update(const char* download_url, const char* expected_sha256, size_t expected_size) {
    HTTPClient http;
    http.begin(download_url);
    http.addHeader("Accept", "application/octet-stream");

    int httpCode = http.GET();
    if (httpCode != 200) {
        Serial.printf("[OTA] Download failed: HTTP %d\n", httpCode);
        http.end();
        return false;
    }

    int content_length = http.getSize();
    if (content_length != expected_size) {
        Serial.printf("[OTA] Size mismatch: %d != %zu\n", content_length, expected_size);
        http.end();
        return false;
    }

    // Start OTA update
    if (!Update.begin(content_length)) {
        Serial.printf("[OTA] Not enough space: %s\n", Update.errorString());
        http.end();
        return false;
    }

    // Stream firmware and compute SHA256 simultaneously
    WiFiClient* stream = http.getStreamPtr();
    mbedtls_sha256_context sha_ctx;
    mbedtls_sha256_init(&sha_ctx);
    mbedtls_sha256_starts(&sha_ctx, 0);  // SHA-256 (not SHA-224)

    uint8_t buf[1024];
    size_t written = 0;

    while (http.connected() && written < content_length) {
        size_t available = stream->available();
        if (available == 0) {
            delay(1);
            continue;
        }

        size_t to_read = min(available, sizeof(buf));
        size_t read = stream->readBytes(buf, to_read);

        // Write to OTA partition
        size_t ota_written = Update.write(buf, read);
        if (ota_written != read) {
            Serial.printf("[OTA] Write error at %zu bytes\n", written);
            Update.abort();
            http.end();
            return false;
        }

        // Update SHA256
        mbedtls_sha256_update(&sha_ctx, buf, read);
        written += read;

        // Progress indicator
        if (written % 65536 == 0) {
            float pct = (float)written / content_length * 100;
            Serial.printf("[OTA] Progress: %.0f%% (%zu/%d)\n", pct, written, content_length);
            esp_task_wdt_reset();  // Keep watchdog happy during long download
        }
    }

    http.end();

    // Verify SHA256
    uint8_t sha256_result[32];
    mbedtls_sha256_finish(&sha_ctx, sha256_result);
    mbedtls_sha256_free(&sha_ctx);

    char sha256_hex[65];
    for (int i = 0; i < 32; i++) {
        sprintf(&sha256_hex[i * 2], "%02x", sha256_result[i]);
    }
    sha256_hex[64] = '\0';

    if (strcmp(sha256_hex, expected_sha256) != 0) {
        Serial.println("[OTA] SHA256 mismatch! Aborting.");
        Serial.printf("[OTA] Expected: %s\n", expected_sha256);
        Serial.printf("[OTA] Got:      %s\n", sha256_hex);
        Update.abort();
        return false;
    }

    // Finalize
    if (!Update.end(true)) {
        Serial.printf("[OTA] Finalization error: %s\n", Update.errorString());
        return false;
    }

    Serial.println("[OTA] Update successful! SHA256 verified.");
    Serial.println("[OTA] Rebooting to new firmware...");

    return true;  // Caller should ESP.restart()
}
```

### 3.3 Rollback Protection

After an OTA update, the collar boots on the new firmware with a "pending confirmation" state. If the new firmware crashes repeatedly, the bootloader rolls back to the previous version.

```c
// In setup(), after OTA boot:

#include <esp_ota_ops.h>

void verify_ota_boot() {
    const esp_partition_t* running = esp_ota_get_running_partition();
    esp_ota_img_states_t state;
    esp_ota_get_state_partition(running, &state);

    if (state == ESP_OTA_IMG_PENDING_VERIFY) {
        Serial.println("[OTA] Running new firmware (pending verification)");

        // Run self-tests
        bool tests_pass = true;
        tests_pass &= imu_self_test();
        tests_pass &= gps_self_test();
        tests_pass &= ble_self_test();
        tests_pass &= nfc_self_test();
        tests_pass &= power_self_test();

        if (tests_pass) {
            esp_ota_mark_app_valid_cancel_rollback();
            Serial.println("[OTA] Firmware verified! Marked as valid.");

            // Notify API of successful update
            wifi_confirm_ota(COLLAR_FW_VERSION);
        } else {
            Serial.println("[OTA] Self-test FAILED! Rolling back...");
            esp_ota_mark_app_invalid_rollback_and_reboot();
            // Device reboots to previous firmware
        }
    }
}
```

### 3.4 Self-Test Suite

```c
bool imu_self_test() {
    int32_t accel[3];
    if (imu.Get_X_Axes(accel) != LSM6DSO_OK) return false;

    // Gravity should read ~1000 mg on one axis
    float magnitude = sqrt(accel[0]*accel[0] + accel[1]*accel[1] + accel[2]*accel[2]) / 1000.0;
    return (magnitude > 0.8 && magnitude < 1.2);
}

bool gps_self_test() {
    // Just check that the GPS module responds (don't wait for a fix)
    gps_power_on();
    delay(1000);
    bool ok = gnss.isConnected();
    gps_power_off();
    return ok;
}

bool ble_self_test() {
    // Verify BLE stack initializes
    return NimBLEDevice::getInitialized();
}

bool nfc_self_test() {
    // Verify PN532 responds
    nfc_reader.begin();
    uint32_t version = nfc_reader.getFirmwareVersion();
    return (version != 0);
}

bool power_self_test() {
    // Verify battery reading is in valid range
    float v = power_get_voltage();
    return (v > 2.5 && v < 4.5);
}
```

---

## 4. Staged Rollout

The API supports staged rollout to catch issues before deploying to all collars:

```
Phase 1: Deploy to 1 collar (developer's test collar)
  → Monitor for 24 hours
  → Check: no crashes, GPS working, NFC working, battery life normal

Phase 2: Deploy to 10% of collars
  → Monitor for 48 hours
  → Check: no increase in collar_offline events, no battery drain issues

Phase 3: Deploy to 100% of collars
  → Full rollout
```

API tracks deployment status:
```json
{
    "firmwareId": 3,
    "version": "1.1.0",
    "rolloutPhase": 2,
    "totalTargets": 50,
    "deployed": 5,
    "confirmed": 4,
    "rolledBack": 0,
    "pending": 1,
    "failed": 0
}
```

---

## 5. Database Model

```csharp
public class CollarFirmware
{
    public int Id { get; set; }
    public string Version { get; set; }
    public string FilePath { get; set; }
    public long FileSize { get; set; }
    public string Sha256 { get; set; }
    public string? ReleaseNotes { get; set; }
    public int MinBatteryPct { get; set; }
    public bool ForceUpdate { get; set; }
    public DateTime UploadedAt { get; set; }
    public int UploadedByUserId { get; set; }

    // Staged rollout
    public string? TargetCollarIds { get; set; }  // JSON array, null = all
    public int RolloutPhase { get; set; }
}

public class CollarFirmwareDeployment
{
    public int Id { get; set; }
    public int CollarFirmwareId { get; set; }
    public int CollarDeviceId { get; set; }
    public string Status { get; set; }  // "pending", "downloading", "installed", "confirmed", "rolled_back", "failed"
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
```

---

## 6. Security Considerations

1. **Binary signing**: Future enhancement — sign firmware binaries with an Ed25519 key. The collar verifies the signature before flashing. Prevents tampered firmware from being installed even if the API is compromised.

2. **HTTPS only**: Firmware download always over HTTPS. The ESP32-S3 validates the server certificate (or uses a pinned CA).

3. **SHA256 verification**: The collar independently computes the SHA256 of the downloaded binary and compares with the API-provided hash. Protects against truncated or corrupted downloads.

4. **Rollback protection**: The ESP32 bootloader automatically rolls back to the previous firmware if the new firmware fails self-tests or crashes during the first boot.

5. **Battery gate**: No updates below 30% battery (configurable) to prevent bricking from power loss during flash write.
