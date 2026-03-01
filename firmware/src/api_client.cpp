#include "api_client.h"
#include "config.h"
#include "network_manager.h"
#include "offline_queue.h"
#include <HTTPClient.h>
#include <WiFiClientSecure.h>
#include <ArduinoJson.h>

static WiFiClientSecure& getSecureClient() {
    static WiFiClientSecure client;
    static bool initialized = false;
    if (!initialized) {
#if API_INSECURE_TLS
        client.setInsecure();
#else
        if (strlen(API_CA_CERT) > 0) {
            client.setCACert(API_CA_CERT);
        } else {
            client.setInsecure();
        }
#endif
        initialized = true;
    }
    return client;
}

AccessResponse api_request_access(camera_fb_t* fb, const char* side,
                                  const char* collarId, int collarNfcVerified, int collarRssi) {
    AccessResponse response = {false, -1, "", 0.0f, "", "", false};

    if (!fb || !fb->buf || fb->len == 0) {
        response.reason = "Invalid frame buffer";
        return response;
    }

    // Build multipart form data
    String boundary = "----ESP32CAMBoundary";
    String contentType = "multipart/form-data; boundary=" + boundary;

    String bodyStart = "--" + boundary + "\r\n";
    bodyStart += "Content-Disposition: form-data; name=\"image\"; filename=\"capture.jpg\"\r\n";
    bodyStart += "Content-Type: image/jpeg\r\n\r\n";

    String apiKeyPart = "";
    if (strlen(API_KEY) > 0) {
        apiKeyPart = "\r\n--" + boundary + "\r\n";
        apiKeyPart += "Content-Disposition: form-data; name=\"apiKey\"\r\n\r\n";
        apiKeyPart += String(API_KEY);
    }

    String sidePart = "";
    if (side && strlen(side) > 0) {
        sidePart = "\r\n--" + boundary + "\r\n";
        sidePart += "Content-Disposition: form-data; name=\"side\"\r\n\r\n";
        sidePart += String(side);
    }

    String collarPart = "";
    if (collarId && strlen(collarId) > 0) {
        collarPart = "\r\n--" + boundary + "\r\n";
        collarPart += "Content-Disposition: form-data; name=\"collarId\"\r\n\r\n";
        collarPart += String(collarId);
        if (collarNfcVerified >= 0) {
            collarPart += "\r\n--" + boundary + "\r\n";
            collarPart += "Content-Disposition: form-data; name=\"collarNfcVerified\"\r\n\r\n";
            collarPart += collarNfcVerified ? "true" : "false";
        }
        collarPart += "\r\n--" + boundary + "\r\n";
        collarPart += "Content-Disposition: form-data; name=\"collarRssi\"\r\n\r\n";
        collarPart += String(collarRssi);
    }

    String bodyEnd = "\r\n--" + boundary + "--\r\n";
    int totalLen = bodyStart.length() + fb->len + apiKeyPart.length() + sidePart.length() + collarPart.length() + bodyEnd.length();

    uint8_t* body = (uint8_t*)malloc(totalLen);
    if (!body) {
        response.reason = "Failed to allocate request buffer";
        return response;
    }

    int offset = 0;
    memcpy(body + offset, bodyStart.c_str(), bodyStart.length()); offset += bodyStart.length();
    memcpy(body + offset, fb->buf, fb->len); offset += fb->len;
    if (apiKeyPart.length() > 0) { memcpy(body + offset, apiKeyPart.c_str(), apiKeyPart.length()); offset += apiKeyPart.length(); }
    if (sidePart.length() > 0) { memcpy(body + offset, sidePart.c_str(), sidePart.length()); offset += sidePart.length(); }
    if (collarPart.length() > 0) { memcpy(body + offset, collarPart.c_str(), collarPart.length()); offset += collarPart.length(); }
    memcpy(body + offset, bodyEnd.c_str(), bodyEnd.length());

    Serial.printf("Sending access request: %d bytes\n", totalLen);

    String url = String(API_BASE_URL) + String(API_ACCESS_ENDPOINT);
    int httpCode = network_manager_http_post_multipart(url.c_str(), body, totalLen, contentType.c_str());
    free(body);

    if (httpCode == -1) {
        // Network unavailable — queue as synthetic event
        QueuedEvent evt;
        evt.eventType = "UnknownAnimal";
        evt.notes = "Offline during detection";
        evt.batteryVoltage = -1;
        evt.apiKey = API_KEY;
        evt.timestamp = millis();
        offline_queue_push(evt);
        response.reason = "Queued";
        return response;
    }

    if (httpCode == HTTP_CODE_OK) {
        String responseStr = "";
        // Note: for network_manager WiFi path, response is read via HTTPClient internally
        // For the full response we'd need to refactor — here we parse from httpCode only
        // In practice this path is WiFi-only so we can read from HTTPClient
        // Simplified: mark success and parse response
        response.success = true;
        response.allowed = false;
        response.reason = "Response parsing requires direct HTTPClient ref";
        Serial.printf("API response HTTP %d\n", httpCode);
    } else {
        response.reason = "HTTP error: " + String(httpCode);
        Serial.printf("HTTP POST failed: %d\n", httpCode);
    }

    return response;
}

// Restore direct HTTPClient for access requests to retain response body parsing
AccessResponse api_request_access_direct(camera_fb_t* fb, const char* side,
                                         const char* collarId, int collarNfcVerified, int collarRssi) {
    AccessResponse response = {false, -1, "", 0.0f, "", "", false};

    if (!fb || !fb->buf || fb->len == 0) {
        response.reason = "Invalid frame buffer";
        return response;
    }

    if (network_manager_get_transport() != NetworkTransport::WiFi) {
        // Offline — queue and return
        QueuedEvent evt;
        evt.eventType = "UnknownAnimal";
        evt.notes = "Offline during detection";
        evt.batteryVoltage = -1;
        evt.apiKey = API_KEY;
        evt.timestamp = millis();
        offline_queue_push(evt);
        response.reason = "Queued";
        return response;
    }

    HTTPClient http;
    String url = String(API_BASE_URL) + String(API_ACCESS_ENDPOINT);
    http.begin(getSecureClient(), url);
    http.setTimeout(API_TIMEOUT_MS);

    String boundary = "----ESP32CAMBoundary";
    String contentType = "multipart/form-data; boundary=" + boundary;
    http.addHeader("Content-Type", contentType);

    String bodyStart = "--" + boundary + "\r\n";
    bodyStart += "Content-Disposition: form-data; name=\"image\"; filename=\"capture.jpg\"\r\n";
    bodyStart += "Content-Type: image/jpeg\r\n\r\n";

    String apiKeyPart = "";
    if (strlen(API_KEY) > 0) {
        apiKeyPart = "\r\n--" + boundary + "\r\n";
        apiKeyPart += "Content-Disposition: form-data; name=\"apiKey\"\r\n\r\n";
        apiKeyPart += String(API_KEY);
    }

    String sidePart = "";
    if (side && strlen(side) > 0) {
        sidePart = "\r\n--" + boundary + "\r\n";
        sidePart += "Content-Disposition: form-data; name=\"side\"\r\n\r\n";
        sidePart += String(side);
    }

    String collarPart = "";
    if (collarId && strlen(collarId) > 0) {
        collarPart = "\r\n--" + boundary + "\r\n";
        collarPart += "Content-Disposition: form-data; name=\"collarId\"\r\n\r\n";
        collarPart += String(collarId);
        if (collarNfcVerified >= 0) {
            collarPart += "\r\n--" + boundary + "\r\n";
            collarPart += "Content-Disposition: form-data; name=\"collarNfcVerified\"\r\n\r\n";
            collarPart += collarNfcVerified ? "true" : "false";
        }
        collarPart += "\r\n--" + boundary + "\r\n";
        collarPart += "Content-Disposition: form-data; name=\"collarRssi\"\r\n\r\n";
        collarPart += String(collarRssi);
    }

    String bodyEnd = "\r\n--" + boundary + "--\r\n";
    int totalLen = bodyStart.length() + fb->len + apiKeyPart.length() + sidePart.length() + collarPart.length() + bodyEnd.length();

    uint8_t* body = (uint8_t*)malloc(totalLen);
    if (!body) {
        response.reason = "Failed to allocate request buffer";
        return response;
    }

    int offset = 0;
    memcpy(body + offset, bodyStart.c_str(), bodyStart.length()); offset += bodyStart.length();
    memcpy(body + offset, fb->buf, fb->len); offset += fb->len;
    if (apiKeyPart.length() > 0) { memcpy(body + offset, apiKeyPart.c_str(), apiKeyPart.length()); offset += apiKeyPart.length(); }
    if (sidePart.length() > 0) { memcpy(body + offset, sidePart.c_str(), sidePart.length()); offset += sidePart.length(); }
    if (collarPart.length() > 0) { memcpy(body + offset, collarPart.c_str(), collarPart.length()); offset += collarPart.length(); }
    memcpy(body + offset, bodyEnd.c_str(), bodyEnd.length());

    Serial.printf("Sending access request: %d bytes\n", totalLen);

    int httpCode = http.POST(body, totalLen);
    free(body);

    if (httpCode == HTTP_CODE_OK) {
        String responseStr = http.getString();
        JsonDocument doc;
        DeserializationError err = deserializeJson(doc, responseStr);
        if (!err) {
            response.allowed = doc["allowed"] | false;
            response.animalId = doc["animalId"] | -1;
            response.animalName = doc["animalName"].as<String>();
            response.confidenceScore = doc["confidenceScore"] | 0.0f;
            response.reason = doc["reason"].as<String>();
            response.direction = doc["direction"].as<String>();
            response.success = true;
            Serial.printf("API response: allowed=%d, animal=%s, confidence=%.2f, direction=%s\n",
                         response.allowed, response.animalName.c_str(),
                         response.confidenceScore, response.direction.c_str());
        } else {
            response.reason = "JSON parse error: " + String(err.c_str());
        }
    } else {
        response.reason = "HTTP error: " + String(httpCode);
        Serial.printf("HTTP POST failed: %d\n", httpCode);
    }

    http.end();
    return response;
}

bool api_post_approach_photo(camera_fb_t* fb, const char* side) {
    if (!fb || !fb->buf || fb->len == 0) return false;

    if (network_manager_get_transport() != NetworkTransport::WiFi) {
        // No network — skip approach photo upload (not queued; approach events are best-effort)
        return false;
    }

    HTTPClient http;
    String url = String(API_BASE_URL) + String(API_APPROACH_ENDPOINT);
    http.begin(getSecureClient(), url);
    http.setTimeout(API_TIMEOUT_MS);

    String boundary = "----ESP32CAMBoundary";
    http.addHeader("Content-Type", "multipart/form-data; boundary=" + boundary);

    String bodyStart = "--" + boundary + "\r\n";
    bodyStart += "Content-Disposition: form-data; name=\"image\"; filename=\"approach.jpg\"\r\n";
    bodyStart += "Content-Type: image/jpeg\r\n\r\n";

    String apiKeyPart = "";
    if (strlen(API_KEY) > 0) {
        apiKeyPart = "\r\n--" + boundary + "\r\n";
        apiKeyPart += "Content-Disposition: form-data; name=\"apiKey\"\r\n\r\n";
        apiKeyPart += String(API_KEY);
    }

    String sidePart = "";
    if (side && strlen(side) > 0) {
        sidePart = "\r\n--" + boundary + "\r\n";
        sidePart += "Content-Disposition: form-data; name=\"side\"\r\n\r\n";
        sidePart += String(side);
    }

    String bodyEnd = "\r\n--" + boundary + "--\r\n";
    int totalLen = bodyStart.length() + fb->len + apiKeyPart.length() + sidePart.length() + bodyEnd.length();

    uint8_t* body = (uint8_t*)malloc(totalLen);
    if (!body) {
        http.end();
        return false;
    }

    int offset = 0;
    memcpy(body + offset, bodyStart.c_str(), bodyStart.length()); offset += bodyStart.length();
    memcpy(body + offset, fb->buf, fb->len); offset += fb->len;
    if (apiKeyPart.length() > 0) { memcpy(body + offset, apiKeyPart.c_str(), apiKeyPart.length()); offset += apiKeyPart.length(); }
    if (sidePart.length() > 0) { memcpy(body + offset, sidePart.c_str(), sidePart.length()); offset += sidePart.length(); }
    memcpy(body + offset, bodyEnd.c_str(), bodyEnd.length());

    int httpCode = http.POST(body, totalLen);
    free(body);
    http.end();

    Serial.printf("Approach photo upload: HTTP %d\n", httpCode);
    return (httpCode == HTTP_CODE_NO_CONTENT || httpCode == HTTP_CODE_OK);
}

void api_post_firmware_event(const char* apiKey, const char* eventType, const char* notes, double batteryVoltage) {
    JsonDocument doc;
    doc["apiKey"] = apiKey;
    doc["eventType"] = eventType;
    doc["notes"] = notes ? notes : "";
    doc["batteryVoltage"] = batteryVoltage;

    String body;
    serializeJson(doc, body);

    String url = String(API_BASE_URL) + String(API_FIRMWARE_EVENT_ENDPOINT);
    int code = network_manager_http_post_json(url.c_str(), body);

    if (code != 204 && code != 200) {
        QueuedEvent evt;
        evt.eventType = eventType;
        evt.notes = notes ? notes : "";
        evt.batteryVoltage = batteryVoltage;
        evt.apiKey = apiKey;
        evt.timestamp = millis();
        offline_queue_push(evt);
    }
}
