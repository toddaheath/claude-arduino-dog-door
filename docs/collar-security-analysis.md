# Collar System — Security Analysis

## Overview

Security analysis of the collar companion device, covering threat modeling, attack surfaces, mitigations, and privacy considerations.

---

## 1. Threat Model

### 1.1 Assets to Protect

| Asset | Sensitivity | Impact if Compromised |
|-------|-------------|----------------------|
| Shared secret (NFC auth) | Critical | Attacker can clone collar identity, grant unauthorized door access |
| GPS location data | High | Privacy violation — reveals owner's home address, daily patterns |
| WiFi credentials | High | Attacker can access home network |
| Door access decisions | High | Unauthorized entry to home |
| Geofence boundaries | Medium | Reveals property layout |
| Animal identity/name | Low | Minimal impact |

### 1.2 Threat Actors

| Actor | Capability | Motivation |
|-------|-----------|------------|
| Neighbor/passerby | Low — physical proximity only | Curious, let their dog in |
| Opportunistic thief | Medium — basic RF tools | Bypass door to enter home |
| Targeted attacker | High — custom hardware, RF expertise | Specific interest in home/property |
| Remote attacker | High — network exploitation | Mass exploitation, data harvesting |

### 1.3 Attack Surfaces

```
Physical          RF/Wireless         Network            Supply Chain
├── Collar theft  ├── BLE sniffing    ├── API injection   ├── Firmware tampering
├── Collar clone  ├── NFC replay      ├── MITM (WiFi)     ├── Component backdoor
├── JTAG/SWD      ├── WiFi deauth     ├── DDoS API        └── Counterfeit module
├── Flash readout ├── GPS spoofing    ├── Auth bypass
└── Side-channel  └── BLE hijack      └── Data exfil
```

---

## 2. Attack Analysis & Mitigations

### 2.1 NFC Replay Attack

**Attack:** Capture NFC handshake between collar and door, replay AUTH_RESPONSE later.

**Mitigation:**
- Challenge-response protocol with 32-byte random nonce — each handshake uses a unique nonce
- Timestamp validation (30-second window) — replayed frames have stale timestamps
- Physical proximity required (~4cm NFC range) — attacker must be present at the door

**Residual risk:** LOW — cryptographically sound if shared secret remains confidential.

### 2.2 Collar Cloning

**Attack:** Extract shared secret from stolen/borrowed collar, program a clone device.

**Mitigations:**
- ESP32-S3 flash encryption (AES-256 via eFuse) — reading flash dump yields encrypted data
- NVS encryption — shared secret stored in encrypted NVS partition
- Secret rotation — owner can rotate shared secret from SPA; invalidates any cloned secret
- Activity monitoring — API detects simultaneous authentications from "same" collar at different locations

**Residual risk:** MEDIUM — a sophisticated attacker with physical access to the collar for extended time could potentially extract secrets via fault injection (voltage glitching). Mitigation: remotely rotate the secret if collar is lost.

### 2.3 BLE Eavesdropping

**Attack:** Sniff BLE advertisements and characteristics to learn GPS location, battery level, activity.

**Mitigations:**
- BLE pairing with MITM protection (passkey entry)
- Encrypted characteristics for sensitive data (command, config)
- Location/activity characteristics are READ-only by paired devices
- BLE advertising only exposes collar name prefix (8 hex chars) — no location data in advertisements

**Residual risk:** LOW — BLE 5.0 with secure connections provides adequate encryption for this data sensitivity level.

### 2.4 WiFi Credential Theft

**Attack:** Extract WiFi SSID/password from collar's NVS storage.

**Mitigations:**
- ESP32-S3 flash encryption (eFuse-backed AES-256)
- WiFi credentials in encrypted NVS namespace
- Even with physical access, extraction requires breaking eFuse-backed encryption

**Residual risk:** LOW — comparable to the security of any WiFi IoT device with flash encryption enabled.

### 2.5 GPS Spoofing

**Attack:** Transmit false GPS signals to make the collar report incorrect location.

**Mitigations:**
- GPS spoofing is broadcast — it would also affect the dog's actual position
- IMU cross-validation: if IMU says "walking" but GPS shows "stationary" (or vice versa), flag as suspicious
- Rate-of-change validation: GPS position can't teleport faster than physically possible
- Spoofing detection: sudden HDOP improvement + all satellites at same signal strength = suspicious

**Residual risk:** MEDIUM — GPS spoofing is a known limitation of all civilian GPS receivers. For yard-scale geofencing, the attack requires specialized equipment and physical proximity. Practical impact is limited (attacker could suppress breach alerts while dog is in a restricted zone, but the dog is physically still there).

### 2.6 API MITM (Man-in-the-Middle)

**Attack:** Intercept WiFi communication between collar and API.

**Mitigations:**
- HTTPS with TLS 1.2+ for all API calls
- Certificate validation on ESP32-S3 (root CA bundle in firmware)
- Future: Certificate pinning for the API endpoint

**Residual risk:** LOW — standard TLS protection.

### 2.7 Firmware Tampering

**Attack:** Flash malicious firmware to the collar via USB-C.

**Mitigations:**
- ESP32-S3 Secure Boot v2 — only signed firmware boots
- Flash encryption — dumped firmware is unreadable without eFuse key
- OTA updates verified by SHA-256 hash from API
- Future: Ed25519 firmware signature verification

**Residual risk:** LOW with Secure Boot enabled. Without Secure Boot: MEDIUM (requires physical access to USB-C port).

### 2.8 Door Access Bypass via Collar

**Attack:** Use a compromised collar to gain door access to someone's home.

**Mitigations:**
- Collar NFC alone cannot open the door — API also checks `Animal.IsAllowed`
- Owner can deactivate collar instantly from SPA (sets `IsActive = false`)
- Camera identification runs in parallel — provides visual verification
- Disagreement between collar and camera identity triggers alert and denies access
- API logs all access attempts with identification method

**Residual risk:** LOW — defense in depth (NFC + camera + API policy).

---

## 3. Privacy Considerations

### 3.1 Location Data

**What's collected:**
- GPS coordinates at 0.1-1 Hz while the dog is active
- Accuracy, speed, heading, altitude, satellite count
- Battery voltage (indicates collar charging patterns → indirect home location)

**Privacy controls:**
- All location data is user-scoped (UserId FK) — only the owner can access
- Guest users (via invitation) can see animals but NOT collar/location data
- Location history auto-purged per retention policy (raw data: 24h, downsampled: 30d, summaries: indefinite)
- Owner can delete all collar data from SPA (cascading delete)
- No location data shared with third parties

### 3.2 Geofence Boundaries

Geofence boundaries effectively describe the owner's property layout. These are:
- Stored server-side in the owner's account only
- Not shared with other users (even guests)
- Deleted when the geofence is removed
- Not exposed in any public API

### 3.3 Satellite Imagery

The map tile proxy caches publicly available satellite imagery. This does NOT constitute collecting location data — the same imagery is available to anyone via Esri/Google/Bing Maps.

### 3.4 Data Minimization

- Collar only transmits GPS when actively tracking (not during sleep)
- BLE advertisements contain no location data
- NFC handshake contains minimal telemetry (battery %, fix status)
- WiFi uploads are batched (reduces connection metadata)
- API stores only what's needed; aggressive retention/purge policy

---

## 4. Secure Boot & Flash Encryption Setup

### 4.1 First-Time Provisioning (Manufacturing)

```bash
# Step 1: Generate signing key (keep this secure!)
espsecure.py generate_signing_key --version 2 collar_signing_key.pem

# Step 2: Enable Secure Boot v2 (ONE-TIME, irreversible!)
espefuse.py --port /dev/ttyUSB0 burn_efuse SECURE_BOOT_EN

# Step 3: Enable Flash Encryption (ONE-TIME, irreversible!)
espefuse.py --port /dev/ttyUSB0 burn_efuse FLASH_CRYPT_CNT 0x01

# Step 4: Flash signed firmware
espsecure.py sign_data --version 2 --keyfile collar_signing_key.pem \
    --output firmware_signed.bin firmware.bin
esptool.py --port /dev/ttyUSB0 write_flash 0x0 firmware_signed.bin

# Step 5: Set NVS encryption key
python3 nvs_partition_gen.py encrypt collar_nvs.csv collar_nvs.bin \
    --keygen --keyfile nvs_key.bin
esptool.py write_flash 0x310000 nvs_key.bin
```

### 4.2 Production Key Management

| Key | Storage | Rotation |
|-----|---------|----------|
| Firmware signing key | HSM / offline air-gapped machine | Never (eFuse-burned) |
| NVS encryption key | Per-device eFuse | Never (hardware-bound) |
| Collar shared secret | API (BCrypt hash) + Collar NVS | On-demand via SPA |
| WiFi password | Collar NVS (encrypted) | Via BLE provisioning |
| API key | Collar NVS (encrypted) + API DB | Via SPA settings |

---

## 5. Compliance Notes

### 5.1 FCC/CE

- The ESP32-S3-MINI-1 module is **pre-certified** (FCC ID: 2AC7Z-ESPS3MINI1)
- The PN532 operates at 13.56 MHz (NFC band) — exempt from most certification when used in standard NFC configurations
- The u-blox MAX-M10S is a GPS receiver only (passive) — no RF emissions to certify
- **Full device assembly** may need unintentional radiator testing (FCC Part 15 Class B) depending on production volume and distribution

### 5.2 Battery Safety

- LiPo battery with TP4056 protection IC (overcharge, overdischarge, overcurrent, short circuit)
- USB-C charging compliant with USB Power Delivery for low-power devices
- TPU enclosure is non-flammable and provides physical battery protection
- Operating temperature range: -10C to +50C (LiPo safe range)

### 5.3 Animal Safety

- All components are enclosed in sealed, potted TPU enclosure
- No exposed electrical contacts
- Weight (~25g) is well below 5% of body weight for dogs > 2kg (common guideline for wearable pet devices)
- Collar clip allows quick release
- Buzzer volume limited in firmware to prevent hearing damage (max 85 dB at 1m)
