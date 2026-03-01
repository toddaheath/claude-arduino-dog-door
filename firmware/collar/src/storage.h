#pragma once

#include <Arduino.h>
#include "gps_tracker.h"

// ── Geofence Event Record ───────────────────────────────────
typedef struct {
    uint16_t fence_id;
    uint8_t  type;       // 0=entered, 1=exited, 2=breach
    double   lat;
    double   lng;
    uint32_t timestamp;
} FenceEventRecord;

// ── Initialization ──────────────────────────────────────────
void storage_init();

// ── Collar Identity (NVS) ───────────────────────────────────
void storage_load_collar_identity();
void storage_save_collar_identity(const char* collar_id, const uint8_t* secret);
const char* storage_get_collar_id();
const uint8_t* storage_get_shared_secret();

// ── WiFi Credentials (NVS encrypted) ────────────────────────
void storage_load_wifi_creds();
void storage_save_wifi_creds(const char* ssid, const char* password, const char* api_key);
const char* storage_get_wifi_ssid();
const char* storage_get_wifi_password();
const char* storage_get_api_key();

// ── GPS Point Buffer (SPIFFS ring buffer) ────────────────────
void storage_buffer_location(const GpsFix& fix);
int  storage_get_buffered_count();
bool storage_get_buffered_point(int index, GpsFix* fix);
void storage_clear_buffered_points(int count);

// ── Geofence Storage (NVS) ──────────────────────────────────
void storage_load_geofences();
void storage_save_geofences(const String& json);
uint32_t storage_get_fence_version();

// ── Geofence Event Queue (SPIFFS) ───────────────────────────
void storage_queue_fence_event(const FenceEventRecord& event);
int  storage_get_pending_fence_events();
bool storage_get_fence_event(int index, FenceEventRecord* event);
void storage_clear_fence_events(int count);
