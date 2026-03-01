#include "wifi_uploader.h"
#include "config.h"

#ifndef UNIT_TEST
#include <Arduino.h>
#include <WiFi.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>
#include <Update.h>
#include "storage.h"
#include "power_manager.h"

// Forward declaration
void geofence_reload();

bool wifi_connect(uint32_t timeout_ms) {
    if (WiFi.status() == WL_CONNECTED) return true;

    WiFi.mode(WIFI_STA);
    WiFi.begin(storage_get_wifi_ssid(), storage_get_wifi_password());

    uint32_t start = millis();
    while (WiFi.status() != WL_CONNECTED && millis() - start < timeout_ms) {
        delay(100);
    }

    if (WiFi.status() == WL_CONNECTED) {
        Serial.printf("[WIFI] Connected, IP: %s\n", WiFi.localIP().toString().c_str());
        return true;
    }

    Serial.println("[WIFI] Connection failed");
    WiFi.disconnect(true);
    WiFi.mode(WIFI_OFF);
    return false;
}

void wifi_disconnect() {
    WiFi.disconnect(true);
    WiFi.mode(WIFI_OFF);
    Serial.println("[WIFI] Disconnected");
}

int wifi_upload_locations() {
    int count = storage_get_buffered_count();
    if (count == 0) return 0;

    int batch_size = min(count, (int)WIFI_MAX_BATCH_SIZE);

    JsonDocument doc;
    doc["apiKey"] = storage_get_api_key();
    JsonArray points = doc["points"].to<JsonArray>();

    for (int i = 0; i < batch_size; i++) {
        GpsFix fix;
        if (!storage_get_buffered_point(i, &fix)) break;

        JsonObject pt = points.add<JsonObject>();
        pt["lat"] = fix.lat;
        pt["lng"] = fix.lng;
        pt["alt"] = fix.altitude;
        pt["acc"] = fix.accuracy;
        pt["spd"] = fix.speed;
        pt["hdg"] = fix.heading;
        pt["sat"] = fix.satellites;
        pt["bat"] = power_get_voltage();
        pt["ts"]  = fix.timestamp;
    }

    String payload;
    serializeJson(doc, payload);

    HTTPClient http;
    String url = String(API_BASE_URL) + "/collars/" +
                 String(storage_get_collar_id()) + "/locations";
    http.begin(url);
    http.addHeader("Content-Type", "application/json");

    int httpCode = http.POST(payload);
    if (httpCode == 201) {
        storage_clear_buffered_points(batch_size);
        Serial.printf("[WIFI] Uploaded %d points\n", batch_size);
        http.end();
        return batch_size;
    } else {
        Serial.printf("[WIFI] Upload failed: HTTP %d\n", httpCode);
        http.end();
        return 0;
    }
}

void wifi_upload_geofence_events() {
    int count = storage_get_pending_fence_events();
    if (count == 0) return;

    JsonDocument doc;
    doc["apiKey"] = storage_get_api_key();
    doc["collarId"] = storage_get_collar_id();
    JsonArray events = doc["events"].to<JsonArray>();

    for (int i = 0; i < count; i++) {
        FenceEventRecord rec;
        if (!storage_get_fence_event(i, &rec)) break;

        JsonObject ev = events.add<JsonObject>();
        ev["fenceId"] = rec.fence_id;
        ev["type"] = rec.type;
        ev["lat"] = rec.lat;
        ev["lng"] = rec.lng;
        ev["ts"]  = rec.timestamp;
    }

    String payload;
    serializeJson(doc, payload);

    HTTPClient http;
    String url = String(API_BASE_URL) + "/geofences/events";
    http.begin(url);
    http.addHeader("Content-Type", "application/json");

    int httpCode = http.POST(payload);
    if (httpCode == 201) {
        storage_clear_fence_events(count);
        Serial.printf("[WIFI] Uploaded %d geofence events\n", count);
    }
    http.end();
}

void wifi_sync_geofences() {
    HTTPClient http;
    String url = String(API_BASE_URL) + "/geofences/sync?collarId=" +
                 String(storage_get_collar_id()) +
                 "&sinceVersion=" + String(storage_get_fence_version());
    http.begin(url);

    int httpCode = http.GET();
    if (httpCode == 200) {
        String response = http.getString();
        JsonDocument doc;
        DeserializationError err = deserializeJson(doc, response);
        if (!err) {
            uint32_t version = doc["version"];
            if (version > storage_get_fence_version()) {
                Serial.printf("[FENCE] Updating fences: v%u → v%u\n",
                              storage_get_fence_version(), version);
                storage_save_geofences(response);
                geofence_reload();
            }
        }
    } else if (httpCode == 304) {
        Serial.println("[FENCE] Fences up to date");
    }
    http.end();
}

bool wifi_check_ota_available() {
    HTTPClient http;
    String url = String(API_BASE_URL) + "/collars/" +
                 String(storage_get_collar_id()) +
                 "/firmware?current=" + String(COLLAR_FW_VERSION);
    http.begin(url);

    int httpCode = http.GET();
    bool available = (httpCode == 200);
    http.end();
    return available;
}

bool wifi_perform_ota() {
    HTTPClient http;
    String url = String(API_BASE_URL) + "/collars/" +
                 String(storage_get_collar_id()) + "/firmware/download";
    http.begin(url);
    http.setTimeout(60000);  // 60s timeout for firmware download

    int httpCode = http.GET();
    if (httpCode != 200) {
        Serial.printf("[OTA] Download failed: HTTP %d\n", httpCode);
        http.end();
        return false;
    }

    int contentLength = http.getSize();
    if (contentLength <= 0) {
        Serial.println("[OTA] Invalid content length");
        http.end();
        return false;
    }

    Serial.printf("[OTA] Downloading %d bytes\n", contentLength);

    if (!Update.begin(contentLength)) {
        Serial.printf("[OTA] Not enough space: %s\n", Update.errorString());
        http.end();
        return false;
    }

    WiFiClient* stream = http.getStreamPtr();
    uint8_t buf[1024];
    int written = 0;

    while (http.connected() && written < contentLength) {
        size_t available = stream->available();
        if (available > 0) {
            int readBytes = stream->readBytes(buf, min(available, sizeof(buf)));
            size_t w = Update.write(buf, readBytes);
            if (w != (size_t)readBytes) {
                Serial.printf("[OTA] Write error at %d bytes\n", written);
                Update.abort();
                http.end();
                return false;
            }
            written += readBytes;

            // Progress every 10%
            if (written % (contentLength / 10) < 1024) {
                Serial.printf("[OTA] Progress: %d%%\n", written * 100 / contentLength);
            }
        }
        delay(1);
    }

    http.end();

    if (!Update.end(true)) {
        Serial.printf("[OTA] Finalize failed: %s\n", Update.errorString());
        return false;
    }

    Serial.println("[OTA] Update successful, reboot required");
    return true;
}

#endif  // !UNIT_TEST
