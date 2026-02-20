#include "offline_queue.h"
#include "config.h"
#include <LittleFS.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>

static const char* QUEUE_DIR = "/queue";

void offline_queue_init() {
    if (!LittleFS.begin(true)) {
        Serial.println("[WARN] LittleFS mount failed; reformatting");
        LittleFS.format();
        LittleFS.begin();
    }
    if (!LittleFS.exists(QUEUE_DIR)) {
        LittleFS.mkdir(QUEUE_DIR);
    }
    Serial.printf("[OK] Offline queue ready (%d events)\n", offline_queue_size());
}

bool offline_queue_push(const QueuedEvent& event) {
    if (offline_queue_size() >= OFFLINE_QUEUE_MAX_EVENTS) {
        Serial.println("[WARN] Offline queue full; dropping event");
        return false;
    }

    // Find next free index
    int idx = 0;
    while (true) {
        char path[32];
        snprintf(path, sizeof(path), "%s/%03d.json", QUEUE_DIR, idx);
        if (!LittleFS.exists(path)) break;
        idx++;
        if (idx >= OFFLINE_QUEUE_MAX_EVENTS) return false;
    }

    char path[32];
    snprintf(path, sizeof(path), "%s/%03d.json", QUEUE_DIR, idx);

    JsonDocument doc;
    doc["eventType"] = event.eventType;
    doc["notes"] = event.notes;
    doc["batteryVoltage"] = event.batteryVoltage;
    doc["apiKey"] = event.apiKey;
    doc["timestamp"] = (unsigned long)event.timestamp;

    File f = LittleFS.open(path, "w");
    if (!f) return false;
    serializeJson(doc, f);
    f.close();

    Serial.printf("[QUEUE] Queued event: %s\n", event.eventType.c_str());
    return true;
}

int offline_queue_size() {
    int count = 0;
    File dir = LittleFS.open(QUEUE_DIR);
    if (!dir || !dir.isDirectory()) return 0;
    File entry = dir.openNextFile();
    while (entry) {
        if (!entry.isDirectory()) count++;
        entry = dir.openNextFile();
    }
    return count;
}

int offline_queue_flush(const char* baseUrl, const char* endpoint) {
    int flushed = 0;
    File dir = LittleFS.open(QUEUE_DIR);
    if (!dir || !dir.isDirectory()) return 0;

    File entry = dir.openNextFile();
    while (entry) {
        if (entry.isDirectory()) {
            entry = dir.openNextFile();
            continue;
        }

        String path = String(QUEUE_DIR) + "/" + entry.name();
        String content = entry.readString();
        entry.close();

        HTTPClient http;
        String url = String(baseUrl) + String(endpoint);
        http.begin(url);
        http.addHeader("Content-Type", "application/json");
        http.setTimeout(API_TIMEOUT_MS);

        int code = http.POST(content);
        http.end();

        if (code == 204 || code == 200) {
            LittleFS.remove(path);
            flushed++;
            Serial.printf("[QUEUE] Flushed: %s (HTTP %d)\n", path.c_str(), code);
        } else {
            Serial.printf("[QUEUE] Flush failed: %s (HTTP %d)\n", path.c_str(), code);
        }

        entry = dir.openNextFile();
    }

    return flushed;
}
