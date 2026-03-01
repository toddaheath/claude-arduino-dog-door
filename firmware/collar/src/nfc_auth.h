#pragma once

#include <stdint.h>
#include <stdbool.h>

/// Initialize NFC (PN532 via I2C).
/// Returns true if PN532 detected on the I2C bus.
bool nfc_init();

/// Check if an NFC reader (door) is requesting authentication.
/// Non-blocking — returns true if a field is detected on PIN_NFC_IRQ.
bool nfc_field_detected();

/// Perform the HMAC-SHA256 challenge-response authentication.
/// 1. Read challenge bytes from the door's NFC reader (via APDU)
/// 2. Compute HMAC-SHA256(shared_secret, challenge || timestamp)
/// 3. Send response back via NFC
/// Returns true if the handshake completed (verification is server-side).
bool nfc_authenticate();

/// Set the collar identity and shared secret (loaded from NVS at boot).
void nfc_set_credentials(const char* collar_id, const uint8_t* secret, size_t secret_len);
