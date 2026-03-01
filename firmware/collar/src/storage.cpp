#include "storage.h"
#include "config.h"

#ifndef UNIT_TEST
#include <Preferences.h>
#include <SPIFFS.h>

static Preferences prefs;

// In-memory copies of NVS data
static char collar_id[33] = {0};
static uint8_t shared_secret[NFC_SHARED_SECRET_LEN] = {0};
static char wifi_ssid[33] = {0};
static char wifi_password[65] = {0};
static char api_key[65] = {0};
static uint32_t fence_version = 0;

// GPS point ring buffer
static const char* GPS_BUFFER_FILE = "/gps_buffer.bin";
static int buffered_count = 0;
static const int MAX_BUFFERED = 500;

// Fence event queue
static const char* FENCE_EVENTS_FILE = "/fence_events.bin";
static int pending_fence_events = 0;

void storage_init() {
    if (!SPIFFS.begin(true)) {
        Serial.println("[STORAGE] SPIFFS mount failed, formatting...");
        SPIFFS.format();
        SPIFFS.begin(true);
    }

    // Count existing buffered GPS points
    File f = SPIFFS.open(GPS_BUFFER_FILE, FILE_READ);
    if (f) {
        buffered_count = f.size() / sizeof(GpsFix);
        f.close();
    }

    // Count pending fence events
    f = SPIFFS.open(FENCE_EVENTS_FILE, FILE_READ);
    if (f) {
        pending_fence_events = f.size() / sizeof(FenceEventRecord);
        f.close();
    }

    Serial.printf("[STORAGE] Init: %d GPS points buffered, %d fence events pending\n",
                  buffered_count, pending_fence_events);
}

// ── Collar Identity ─────────────────────────────────────────

void storage_load_collar_identity() {
    prefs.begin("collar", true);
    String id = prefs.getString("id", "");
    strncpy(collar_id, id.c_str(), sizeof(collar_id) - 1);
    prefs.getBytes("secret", shared_secret, NFC_SHARED_SECRET_LEN);
    prefs.end();
    Serial.printf("[STORAGE] Collar ID: %s\n", collar_id[0] ? collar_id : "(not set)");
}

void storage_save_collar_identity(const char* id, const uint8_t* secret) {
    prefs.begin("collar", false);
    prefs.putString("id", id);
    prefs.putBytes("secret", secret, NFC_SHARED_SECRET_LEN);
    prefs.end();
    strncpy(collar_id, id, sizeof(collar_id) - 1);
    memcpy(shared_secret, secret, NFC_SHARED_SECRET_LEN);
}

const char* storage_get_collar_id() {
    return collar_id;
}

const uint8_t* storage_get_shared_secret() {
    return shared_secret;
}

// ── WiFi Credentials ────────────────────────────────────────

void storage_load_wifi_creds() {
    prefs.begin("wifi", true);
    String ssid = prefs.getString("ssid", "");
    String pass = prefs.getString("password", "");
    String key = prefs.getString("api_key", "");
    strncpy(wifi_ssid, ssid.c_str(), sizeof(wifi_ssid) - 1);
    strncpy(wifi_password, pass.c_str(), sizeof(wifi_password) - 1);
    strncpy(api_key, key.c_str(), sizeof(api_key) - 1);
    prefs.end();
}

void storage_save_wifi_creds(const char* ssid, const char* password, const char* key) {
    prefs.begin("wifi", false);
    prefs.putString("ssid", ssid);
    prefs.putString("password", password);
    prefs.putString("api_key", key);
    prefs.end();
    strncpy(wifi_ssid, ssid, sizeof(wifi_ssid) - 1);
    strncpy(wifi_password, password, sizeof(wifi_password) - 1);
    strncpy(api_key, key, sizeof(api_key) - 1);
}

const char* storage_get_wifi_ssid() { return wifi_ssid; }
const char* storage_get_wifi_password() { return wifi_password; }
const char* storage_get_api_key() { return api_key; }

// ── GPS Point Buffer ────────────────────────────────────────

void storage_buffer_location(const GpsFix& fix) {
    if (buffered_count >= MAX_BUFFERED) {
        // Ring buffer full — overwrite oldest (shift file)
        Serial.println("[STORAGE] GPS buffer full, dropping oldest point");
        // Simple approach: just cap at max, wifi upload should drain regularly
        return;
    }

    File f = SPIFFS.open(GPS_BUFFER_FILE, FILE_APPEND);
    if (f) {
        f.write((const uint8_t*)&fix, sizeof(GpsFix));
        f.close();
        buffered_count++;
    }
}

int storage_get_buffered_count() {
    return buffered_count;
}

bool storage_get_buffered_point(int index, GpsFix* fix) {
    if (index >= buffered_count) return false;

    File f = SPIFFS.open(GPS_BUFFER_FILE, FILE_READ);
    if (!f) return false;

    f.seek(index * sizeof(GpsFix));
    size_t read = f.read((uint8_t*)fix, sizeof(GpsFix));
    f.close();
    return read == sizeof(GpsFix);
}

void storage_clear_buffered_points(int count) {
    if (count >= buffered_count) {
        SPIFFS.remove(GPS_BUFFER_FILE);
        buffered_count = 0;
        return;
    }

    // Read remaining points, rewrite file
    int remaining = buffered_count - count;
    GpsFix* temp = (GpsFix*)malloc(remaining * sizeof(GpsFix));
    if (!temp) return;

    File f = SPIFFS.open(GPS_BUFFER_FILE, FILE_READ);
    if (f) {
        f.seek(count * sizeof(GpsFix));
        f.read((uint8_t*)temp, remaining * sizeof(GpsFix));
        f.close();
    }

    f = SPIFFS.open(GPS_BUFFER_FILE, FILE_WRITE);
    if (f) {
        f.write((const uint8_t*)temp, remaining * sizeof(GpsFix));
        f.close();
    }

    free(temp);
    buffered_count = remaining;
}

// ── Geofence Storage ────────────────────────────────────────

void storage_load_geofences() {
    prefs.begin("geofence", true);
    fence_version = prefs.getUInt("version", 0);
    prefs.end();
    Serial.printf("[STORAGE] Geofence version: %u\n", fence_version);
}

void storage_save_geofences(const String& json) {
    // Save raw JSON to SPIFFS for full fence data
    File f = SPIFFS.open("/fences.json", FILE_WRITE);
    if (f) {
        f.print(json);
        f.close();
    }

    // Increment version in NVS
    prefs.begin("geofence", false);
    fence_version++;
    prefs.putUInt("version", fence_version);
    prefs.end();
}

uint32_t storage_get_fence_version() {
    return fence_version;
}

// ── Geofence Event Queue ────────────────────────────────────

void storage_queue_fence_event(const FenceEventRecord& event) {
    File f = SPIFFS.open(FENCE_EVENTS_FILE, FILE_APPEND);
    if (f) {
        f.write((const uint8_t*)&event, sizeof(FenceEventRecord));
        f.close();
        pending_fence_events++;
    }
}

int storage_get_pending_fence_events() {
    return pending_fence_events;
}

bool storage_get_fence_event(int index, FenceEventRecord* event) {
    if (index >= pending_fence_events) return false;

    File f = SPIFFS.open(FENCE_EVENTS_FILE, FILE_READ);
    if (!f) return false;

    f.seek(index * sizeof(FenceEventRecord));
    size_t read = f.read((uint8_t*)event, sizeof(FenceEventRecord));
    f.close();
    return read == sizeof(FenceEventRecord);
}

void storage_clear_fence_events(int count) {
    if (count >= pending_fence_events) {
        SPIFFS.remove(FENCE_EVENTS_FILE);
        pending_fence_events = 0;
        return;
    }

    int remaining = pending_fence_events - count;
    FenceEventRecord* temp = (FenceEventRecord*)malloc(remaining * sizeof(FenceEventRecord));
    if (!temp) return;

    File f = SPIFFS.open(FENCE_EVENTS_FILE, FILE_READ);
    if (f) {
        f.seek(count * sizeof(FenceEventRecord));
        f.read((uint8_t*)temp, remaining * sizeof(FenceEventRecord));
        f.close();
    }

    f = SPIFFS.open(FENCE_EVENTS_FILE, FILE_WRITE);
    if (f) {
        f.write((const uint8_t*)temp, remaining * sizeof(FenceEventRecord));
        f.close();
    }

    free(temp);
    pending_fence_events = remaining;
}

#endif  // !UNIT_TEST
