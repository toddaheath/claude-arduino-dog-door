#include "api_client.h"
#include "config.h"
#include <HTTPClient.h>
#include <ArduinoJson.h>

AccessResponse api_request_access(camera_fb_t* fb) {
    AccessResponse response = {false, -1, "", 0.0f, "", false};

    if (!fb || !fb->buf || fb->len == 0) {
        response.reason = "Invalid frame buffer";
        return response;
    }

    HTTPClient http;
    String url = String(API_BASE_URL) + String(API_ACCESS_ENDPOINT);

    http.begin(url);
    http.setTimeout(API_TIMEOUT_MS);

    // Build multipart form data
    String boundary = "----ESP32CAMBoundary";
    String contentType = "multipart/form-data; boundary=" + boundary;
    http.addHeader("Content-Type", contentType);

    // Construct the multipart body
    String bodyStart = "--" + boundary + "\r\n";
    bodyStart += "Content-Disposition: form-data; name=\"image\"; filename=\"capture.jpg\"\r\n";
    bodyStart += "Content-Type: image/jpeg\r\n\r\n";

    String apiKeyPart = "";
    if (strlen(API_KEY) > 0) {
        apiKeyPart = "\r\n--" + boundary + "\r\n";
        apiKeyPart += "Content-Disposition: form-data; name=\"apiKey\"\r\n\r\n";
        apiKeyPart += String(API_KEY);
    }

    String bodyEnd = "\r\n--" + boundary + "--\r\n";

    // Calculate total content length
    int totalLen = bodyStart.length() + fb->len + apiKeyPart.length() + bodyEnd.length();

    // Allocate buffer for the entire request body
    uint8_t* body = (uint8_t*)malloc(totalLen);
    if (!body) {
        response.reason = "Failed to allocate request buffer";
        return response;
    }

    int offset = 0;
    memcpy(body + offset, bodyStart.c_str(), bodyStart.length());
    offset += bodyStart.length();
    memcpy(body + offset, fb->buf, fb->len);
    offset += fb->len;
    if (apiKeyPart.length() > 0) {
        memcpy(body + offset, apiKeyPart.c_str(), apiKeyPart.length());
        offset += apiKeyPart.length();
    }
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
            response.success = true;

            Serial.printf("API response: allowed=%d, animal=%s, confidence=%.2f\n",
                         response.allowed, response.animalName.c_str(),
                         response.confidenceScore);
        } else {
            response.reason = "JSON parse error: " + String(err.c_str());
            Serial.println(response.reason);
        }
    } else {
        response.reason = "HTTP error: " + String(httpCode);
        Serial.printf("HTTP POST failed: %d\n", httpCode);
    }

    http.end();
    return response;
}
