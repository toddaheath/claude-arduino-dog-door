#include "nfc_auth.h"
#include "config.h"

#ifndef UNIT_TEST

#include <Arduino.h>
#include <Wire.h>
#include <Adafruit_PN532.h>
#include <mbedtls/md.h>

static Adafruit_PN532 nfc(PIN_NFC_IRQ, PIN_NFC_RST);
static bool initialized = false;

static char collar_id[NFC_COLLAR_ID_LEN + 1] = {0};
static uint8_t shared_secret[NFC_SHARED_SECRET_LEN] = {0};
static size_t secret_length = 0;

bool nfc_init() {
    nfc.begin();
    uint32_t version = nfc.getFirmwareVersion();
    if (!version) {
        Serial.println("[NFC] PN532 not found");
        return false;
    }

    Serial.printf("[NFC] PN532 firmware v%d.%d\n",
                  (version >> 24) & 0xFF, (version >> 16) & 0xFF);

    nfc.SAMConfig();
    nfc.setPassiveActivationRetries(1);
    initialized = true;
    return true;
}

bool nfc_field_detected() {
    if (!initialized) return false;
    // Check IRQ pin — active low when an RF field is present
    return digitalRead(PIN_NFC_IRQ) == LOW;
}

void nfc_set_credentials(const char* id, const uint8_t* secret, size_t len) {
    strncpy(collar_id, id, NFC_COLLAR_ID_LEN);
    collar_id[NFC_COLLAR_ID_LEN] = '\0';

    secret_length = len > NFC_SHARED_SECRET_LEN ? NFC_SHARED_SECRET_LEN : len;
    memcpy(shared_secret, secret, secret_length);
}

/// Compute HMAC-SHA256 using mbedTLS (available on ESP32)
static bool compute_hmac(const uint8_t* key, size_t key_len,
                         const uint8_t* data, size_t data_len,
                         uint8_t* output) {
    mbedtls_md_context_t ctx;
    mbedtls_md_init(&ctx);

    const mbedtls_md_info_t* info = mbedtls_md_info_from_type(MBEDTLS_MD_SHA256);
    if (!info) return false;

    if (mbedtls_md_setup(&ctx, info, 1) != 0) return false;
    if (mbedtls_md_hmac_starts(&ctx, key, key_len) != 0) {
        mbedtls_md_free(&ctx);
        return false;
    }
    if (mbedtls_md_hmac_update(&ctx, data, data_len) != 0) {
        mbedtls_md_free(&ctx);
        return false;
    }
    if (mbedtls_md_hmac_finish(&ctx, output) != 0) {
        mbedtls_md_free(&ctx);
        return false;
    }

    mbedtls_md_free(&ctx);
    return true;
}

bool nfc_authenticate() {
    if (!initialized || secret_length == 0) return false;

    // In NFC target mode, the door reader sends us a challenge.
    // Protocol:
    //   1. Door sends SELECT AID command
    //   2. We respond with collar ID
    //   3. Door sends challenge (16 bytes random + 8 bytes timestamp)
    //   4. We compute HMAC-SHA256(secret, challenge) and respond

    // Wait for reader with timeout
    uint8_t uid[7];
    uint8_t uidLen;

    // Try to detect an ISO14443A card reader in target mode
    // On PN532, we emulate an NFC tag that the door reader can read
    uint8_t apdubuffer[64];
    uint8_t apdulen;

    // Emulate NFC-A target and wait for reader command
    if (!nfc.inListPassiveTarget()) {
        return false;
    }

    // Read the challenge APDU from the door reader
    apdulen = sizeof(apdubuffer);
    if (!nfc.inDataExchange(
            (uint8_t*)collar_id, strlen(collar_id),
            apdubuffer, &apdulen)) {
        Serial.println("[NFC] Failed to exchange collar ID");
        return false;
    }

    // apdubuffer should now contain: challenge (16 bytes) + timestamp (8 bytes)
    if (apdulen < 24) {
        Serial.printf("[NFC] Challenge too short: %d bytes\n", apdulen);
        return false;
    }

    // Compute HMAC-SHA256(shared_secret, challenge || timestamp)
    uint8_t hmac_output[32];
    if (!compute_hmac(shared_secret, secret_length,
                      apdubuffer, 24, hmac_output)) {
        Serial.println("[NFC] HMAC computation failed");
        return false;
    }

    // Send HMAC response back to door
    uint8_t response_ack[2];
    uint8_t ack_len = sizeof(response_ack);
    if (!nfc.inDataExchange(hmac_output, 32, response_ack, &ack_len)) {
        Serial.println("[NFC] Failed to send HMAC response");
        return false;
    }

    Serial.println("[NFC] Authentication handshake complete");
    return true;
}

#else // UNIT_TEST stubs

bool nfc_init() { return true; }
bool nfc_field_detected() { return false; }
bool nfc_authenticate() { return false; }
void nfc_set_credentials(const char*, const uint8_t*, size_t) {}

#endif // UNIT_TEST
