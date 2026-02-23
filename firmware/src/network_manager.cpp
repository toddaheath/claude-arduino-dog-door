#include "network_manager.h"
#include "config.h"
#include "wifi_manager.h"
#include "cellular_manager.h"
#include "offline_queue.h"
#include <HTTPClient.h>
#include <ArduinoJson.h>

static NetworkTransport _transport = NetworkTransport::None;
static bool _cellularReady = false;

void network_manager_init() {
    _cellularReady = cellular_init();
    if (_cellularReady) {
        Serial.println("[NET] Cellular available");
    }
}

void network_manager_ensure_connected() {
    wifi_ensure_connected();
    if (wifi_is_connected()) {
        _transport = NetworkTransport::WiFi;
        return;
    }
    // Fall back to cellular
    if (_cellularReady && cellular_is_registered()) {
        _transport = NetworkTransport::Cellular;
    } else {
        _transport = NetworkTransport::None;
    }
}

NetworkTransport network_manager_get_transport() {
    return _transport;
}

bool network_manager_is_connected() {
    return _transport != NetworkTransport::None;
}

int network_manager_http_post_json(const char* url, const String& body) {
    if (_transport == NetworkTransport::WiFi) {
        HTTPClient http;
        http.begin(url);
        http.addHeader("Content-Type", "application/json");
        http.setTimeout(API_TIMEOUT_MS);
        int code = http.POST(body);
        http.end();
        return code;
    }
    if (_transport == NetworkTransport::Cellular) {
        return cellular_http_post(url, "application/json",
            (const uint8_t*)body.c_str(), body.length());
    }
    return -1;
}

int network_manager_http_post_multipart(const char* url, const uint8_t* body, size_t len, const char* contentType) {
    if (_transport == NetworkTransport::WiFi) {
        HTTPClient http;
        http.begin(url);
        http.addHeader("Content-Type", contentType);
        http.setTimeout(API_TIMEOUT_MS);
        int code = http.POST(const_cast<uint8_t*>(body), len);
        http.end();
        return code;
    }
    // Cellular can't handle large JPEG payloads
    Serial.println("[NET] Multipart skipped: WiFi unavailable, queuing event");
    return -1;
}
