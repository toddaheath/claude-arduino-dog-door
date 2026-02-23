#include "cellular_manager.h"
#include "config.h"
// Serial2 is globally declared in Arduino framework (HardwareSerial.h)

static bool sendAT(const char* cmd, const char* expected, unsigned long timeoutMs) {
    Serial2.println(cmd);
    String response = "";
    unsigned long start = millis();
    while (millis() - start < timeoutMs) {
        while (Serial2.available()) {
            response += (char)Serial2.read();
        }
        if (response.indexOf(expected) >= 0) return true;
        delay(10);
    }
    Serial.printf("[CELL] AT timeout: %s (got: %s)\n", cmd, response.c_str());
    return false;
}

bool cellular_init() {
    Serial2.begin(CELLULAR_BAUD_RATE, SERIAL_8N1, CELLULAR_RX_PIN, CELLULAR_TX_PIN);
    delay(2000);

    if (!sendAT("AT", "OK", 3000)) {
        Serial.println("[CELL] Modem not responding");
        return false;
    }

    if (!sendAT("AT+CPIN?", "READY", CELLULAR_TIMEOUT_MS)) {
        Serial.println("[CELL] SIM not ready");
        return false;
    }

    String apnCmd = String("AT+CGDCONT=1,\"IP\",\"") + CELLULAR_APN + "\"";
    sendAT(apnCmd.c_str(), "OK", 3000);

    Serial.println("[OK] Cellular initialized");
    return true;
}

bool cellular_is_registered() {
    Serial2.println("AT+CREG?");
    String response = "";
    unsigned long start = millis();
    while (millis() - start < 3000) {
        while (Serial2.available()) {
            response += (char)Serial2.read();
        }
        if (response.indexOf("+CREG:") >= 0) break;
        delay(10);
    }
    // +CREG: 0,1 (registered home) or +CREG: 0,5 (roaming)
    return (response.indexOf(",1") >= 0 || response.indexOf(",5") >= 0);
}

int cellular_http_post(const char* url, const char* contentType, const uint8_t* body, size_t len) {
    // AT+HTTPINIT
    if (!sendAT("AT+HTTPINIT", "OK", 3000)) return -1;

    // Set URL
    String urlCmd = String("AT+HTTPPARA=\"URL\",\"") + url + "\"";
    if (!sendAT(urlCmd.c_str(), "OK", 3000)) {
        sendAT("AT+HTTPTERM", "OK", 3000);
        return -1;
    }

    // Set content type
    String ctCmd = String("AT+HTTPPARA=\"CONTENT\",\"") + contentType + "\"";
    sendAT(ctCmd.c_str(), "OK", 3000);

    // Send data
    String dataCmd = String("AT+HTTPDATA=") + len + ",10000";
    if (!sendAT(dataCmd.c_str(), "DOWNLOAD", 3000)) {
        sendAT("AT+HTTPTERM", "OK", 3000);
        return -1;
    }

    Serial2.write(body, len);
    delay(500);

    // POST
    if (!sendAT("AT+HTTPACTION=1", "+HTTPACTION:", CELLULAR_TIMEOUT_MS)) {
        sendAT("AT+HTTPTERM", "OK", 3000);
        return -1;
    }

    // Read response status from Serial2 buffer
    String resp = "";
    unsigned long start = millis();
    while (millis() - start < 2000) {
        while (Serial2.available()) resp += (char)Serial2.read();
        delay(10);
    }

    sendAT("AT+HTTPTERM", "OK", 3000);

    // Parse status from "+HTTPACTION: 1,200,..." or "+HTTPACTION:1,204,..."
    int statusStart = resp.indexOf(",");
    if (statusStart < 0) return -1;
    int statusEnd = resp.indexOf(",", statusStart + 1);
    if (statusEnd < 0) statusEnd = resp.length();
    String statusStr = resp.substring(statusStart + 1, statusEnd);
    statusStr.trim();
    return statusStr.toInt();
}
