# Multi-Dog Scenarios & Edge Cases

## Overview

Detailed analysis of how the collar system handles multiple dogs, edge cases in identification, and unusual real-world situations.

---

## 1. Multi-Dog Household Scenarios

### 1.1 Two Dogs Approach Door Simultaneously

**Scenario:** Luna and Max both approach the door from outside at the same time.

```
Timeline:
t=0s    Radar detects motion (one trigger for both dogs)
t=0.2s  Ultrasonic confirms proximity (detects nearest dog)
t=0.3s  BLE scan finds TWO collar advertisements
        - Luna's collar: RSSI -45 dBm (closer)
        - Max's collar: RSSI -52 dBm (farther)
t=0.5s  Camera captures image (both dogs may be in frame)
t=0.8s  NFC: Only one collar can be in NFC range (~4cm) at a time
        - Luna's collar: NFC handshake succeeds (she's closer to door)
        - Max's collar: Not in NFC range yet
t=1.0s  API: Access request with Luna's image + Luna's collar NFC
        → Access granted for Luna
t=1.2s  Door opens
t=2.0s  Luna passes through (IR beam triggered)
t=3.0s  Max approaches (now in NFC range)
t=3.2s  Second radar trigger (or IR beam re-trigger)
t=3.5s  Camera captures Max, NFC handshake with Max's collar
t=4.0s  API: Access request with Max's image + Max's collar NFC
        → Access granted for Max
t=5.0s  Max passes through
t=15s   Auto-close (no IR beam interruption)
```

**Behavior:** The system processes one dog at a time in rapid succession. The door stays open (auto-close timer resets on each new IR beam trigger) until both dogs pass through. Each dog gets a separate access event logged.

**Optimization:** If both dogs are known and allowed, the door can stay open for the auto-close timeout without requiring the second dog to trigger a full re-identification cycle. The BLE scanner can pre-identify both collars, and the camera can confirm both dogs in a single wider-angle shot.

### 1.2 Allowed Dog + Blocked Dog Together

**Scenario:** Luna (allowed) and a neighbor's dog Buddy (not registered) approach together.

```
t=0s    Motion detected
t=0.3s  BLE scan: Luna's collar found, no other collar
t=0.5s  Camera captures: two dogs in frame
t=0.8s  NFC: Luna's collar verified
t=1.0s  API:
        - Camera: pHash matches Luna (confidence 0.72)
        - Camera: second subject detected, no match (unknown)
        - Collar: Luna verified (confidence 0.95)
        → Decision: Grant access for Luna
        → Log: Two subjects detected, one unknown
t=1.2s  Door opens
t=2.0s  Luna enters
t=2.5s  Buddy tries to follow
        IR beam detects continued presence
        → Door stays open (safety: never close on an animal)
t=3.0s  API: Logs "UnknownAnimal" event with image
        → Notification to owner: "Unknown animal detected at door"
```

**Problem:** The door can't physically block one dog while letting another through. The door either opens or stays closed.

**Mitigation strategies:**
1. **Alert-only mode**: Door opens for the allowed dog but sends an alert about the unknown animal. Owner decides if action is needed.
2. **Two-stage entry**: Door opens to a small vestibule (if physically possible), allowed dog enters, door closes, second door opens. Impractical for most installations.
3. **Owner override**: Owner gets push notification with camera image and can remotely lock the door if the unknown animal shouldn't enter.

**Recommendation:** Alert-only mode is the practical approach. The door prioritizes the safety and convenience of the allowed dog while alerting the owner about unexpected visitors.

### 1.3 Dog Without Collar (Battery Dead, Removed for Bath)

**Scenario:** Luna's collar is charging inside. Luna wants to go outside.

```
t=0s    Motion detected (inside camera)
t=0.3s  BLE scan: No collar advertisements found
t=0.5s  Camera captures Luna
t=0.8s  NFC: No collar to scan (skipped)
t=1.0s  API:
        - Camera: pHash matches Luna (confidence 0.72)
        - Collar: Not present
        → identificationMethod: "camera"
        → confidence: 0.72 (camera only, no boost)
        → Decision: Access granted (above 0.6 threshold)
```

**Behavior:** Falls back to camera-only identification. This is the existing system behavior — the collar is purely additive. No collar = no change from baseline.

**User experience:** The SPA shows "Identified by: Camera only" in the access log. If the collar has been offline for > 1 hour, the owner gets a "collar offline" notification (configurable).

### 1.4 Wrong Collar on Wrong Dog

**Scenario:** During grooming, Luna's collar was accidentally put on Max.

```
t=0s    Motion detected
t=0.3s  BLE scan: "Luna's collar" found (but it's on Max's neck)
t=0.5s  Camera captures Max
t=0.8s  NFC: Luna's collar verified → collar says "Luna"
t=1.0s  API:
        - Camera: pHash matches Max (confidence 0.75)
        - Collar: Verified as Luna (confidence 0.95)
        → DISAGREEMENT: Camera says Max, collar says Luna
        → Fused confidence: min(0.75, 0.95) × 0.5 = 0.375
        → Decision: DENIED (0.375 < 0.6 threshold)
        → Alert: "Identification conflict: Camera identified Max but collar belongs to Luna"
```

**Behavior:** The disagreement penalty kicks in. Access is denied, and the owner is alerted about the mismatch. This is a security feature — if someone put a stolen collar on a different dog, it would also trigger this alert.

**Resolution:** Owner swaps collars back. Or owner temporarily grants access via the SPA.

### 1.5 New Dog Added (No Reference Photos Yet)

**Scenario:** Family adopts a new dog Bella. No reference photos uploaded yet, collar not paired.

```
t=0s    Motion detected
t=0.3s  BLE scan: No collar
t=0.5s  Camera captures Bella
t=0.8s  API:
        - Camera: No pHash match (no photos in database)
        → identificationMethod: "camera"
        → EventType: UnknownAnimal
        → Decision: Denied
        → Approach photo saved for future reference
```

**Getting Bella set up:**
1. Owner uploads reference photos of Bella via SPA (`/animals/new` → upload photos)
2. API computes pHash for each photo
3. Owner pairs a new collar via SPA (`/collars` → "Pair New Collar")
4. Next time Bella approaches, both camera + collar identify her

**First-time photo bootstrapping:** The "approach photos" that the door automatically saves for every motion event can be used. Owner goes to Access Log, finds Bella's approach photos, and assigns them to the new animal profile.

---

## 2. Geofence Multi-Dog Scenarios

### 2.1 Different Rules for Different Dogs

**Scenario:** Luna is allowed in the full backyard. Max (a puppy) is restricted to a smaller area.

```
Fences defined:
1. "Full Backyard" - Polygon - ALLOW - Applies to: Luna
2. "Puppy Zone" - Polygon (smaller) - ALLOW - Applies to: Max
3. "Pool" - Circle - DENY - Applies to: ALL
```

Each collar evaluates only the fences assigned to it:
- **Luna's collar** checks: Full Backyard (allow) + Pool (deny)
- **Max's collar** checks: Puppy Zone (allow) + Pool (deny)

If Max wanders into Luna's larger area (but outside Puppy Zone):
- Max's collar: BREACH (outside all allow zones)
- Luna's collar: OK (inside Full Backyard)

**API perspective:** Geofence events are per-collar. Max's breach doesn't affect Luna's status.

### 2.2 Dog Follows Another Into Restricted Zone

**Scenario:** Max follows Luna near the pool. Both collars are tracking.

```
Luna's collar:
  t=0s    GPS fix: 8m from Pool center (outside r=3m deny zone) → OK
  t=1s    GPS fix: 6m from Pool → approaching warning (< 5m threshold)
  t=1s    BUZZ: short beep (approaching deny zone)

Max's collar:
  t=0s    GPS fix: 10m from Pool → OK
  t=2s    GPS fix: 7m from Pool → OK

Luna's collar:
  t=3s    GPS fix: 4m from Pool → still outside (3m + 3m hysteresis = OK)
  t=4s    GPS fix: 2m from Pool → BREACH! (inside deny zone)
  t=4s    BUZZ: double beep (breach alert)
  t=4s    WiFi: upload breach event → API → notification to owner

Max's collar:
  t=5s    GPS fix: 4m from Pool → approaching warning
  t=5s    BUZZ: short beep
  t=7s    GPS fix: 5m from Pool → OK (turned around after hearing Luna's buzz)
```

**Behavior:** Each collar acts independently. Luna's buzzer might serve as a warning to Max too (social learning), but the system doesn't rely on this.

### 2.3 Both Dogs Breach Simultaneously

**Scenario:** Both dogs run to the fence line together.

```
Both collars:
  t=0s    GPS: inside "Backyard" allow zone
  t=5s    GPS: approaching backyard boundary
  t=5s    BUZZ: both collars beep (approaching warning)
  t=8s    GPS: both outside backyard boundary
  t=8s    BUZZ: both collars double-beep (breach)
  t=8s    WiFi: both collars queue breach events

API receives:
  - GeofenceEvent: Luna breached "Backyard" at t=8s
  - GeofenceEvent: Max breached "Backyard" at t=8s
  - Notification: "Luna AND Max left the Backyard at 3:45 PM"
```

**Notification deduplication:** The API can detect simultaneous breaches of the same fence and combine them into a single owner notification: "2 dogs left the Backyard" instead of two separate alerts.

---

## 3. Environmental Edge Cases

### 3.1 Snow/Rain Affecting GPS

**Scenario:** Heavy rain or snow attenuates GPS signals, reducing accuracy.

```
Normal conditions:   HDOP 1.2,  Accuracy 2.5m,  Satellites 12
Heavy rain:          HDOP 2.5,  Accuracy 5-8m,  Satellites 8-10
Dense cloud/storm:   HDOP 4.0,  Accuracy 10-15m, Satellites 5-7
Indoor/under cover:  HDOP >5.0, Accuracy >15m,  Satellites 2-4
```

**Mitigation:**
- GPS quality gating: geofence evaluation suppressed when HDOP > 5.0 or accuracy > 15m
- Wider hysteresis during poor conditions (configurable: 3m default, 5m when HDOP > 3)
- IMU-based dead reckoning for short GPS outages (< 30s)
- No breach alerts during "no fix" periods (avoid false alarms)

### 3.2 Magnetic Interference (Near Metal Fences, Cars)

**Scenario:** Dog near a metal fence or parked car experiences GPS multipath.

**Impact:** GPS multipath causes position to "jump" by 5-10m intermittently.

**Mitigation:**
- 3-meter hysteresis prevents single-sample jumps from triggering breach
- Speed validation: if GPS says the dog moved 10m in 1 second (10 m/s = 22 mph) but IMU says "walking" (< 2 m/s), discard the outlier
- Outlier filtering: Kalman filter or simple median filter on GPS positions

```c
// Simple outlier rejection
bool is_gps_outlier(const GpsFix* current, const GpsFix* previous) {
    if (!previous) return false;

    double distance = haversine_distance(current->lat, current->lng,
                                          previous->lat, previous->lng);
    double dt = (current->timestamp - previous->timestamp);
    if (dt <= 0) return true;

    double speed = distance / dt;  // m/s
    float imu_speed = imu_get_speed_estimate();

    // If GPS implies >5x the IMU speed, it's probably multipath
    if (speed > imu_speed * 5.0 && speed > 3.0) {
        return true;
    }
    return false;
}
```

### 3.3 Dog Digs Under Fence (Physical Escape)

**Scenario:** Dog physically leaves the property through a gap in the physical fence.

```
t=0s    Dog inside "Backyard" allow zone → OK
t=30s   Dog squeezes under fence
t=31s   GPS fix: 1m outside backyard boundary → within hysteresis, suppressed
t=35s   GPS fix: 5m outside → BREACH triggered
t=35s   BUZZ: double beep (but dog is already outside)
t=35s   WiFi: breach event uploaded
t=36s   API: Push notification to owner with GPS coordinates
```

**Limitation:** The geofence alert comes after the dog has already left. The buzzer serves as a training signal (dog learns to associate the beep with being out of bounds) but cannot physically prevent escape.

**Enhancement idea:** Proximity warnings at 5m from boundary give the owner time to intervene before a breach. The repeated short beeps as the dog approaches the boundary may deter some dogs from testing the edge.

### 3.4 Collar Stuck in NFC Range (Dog Sleeping by Door)

**Scenario:** Dog falls asleep against the door. Collar is within NFC range of the door reader.

```
t=0s    BLE: Collar detected with strong RSSI (-35 dBm)
t=0.5s  NFC: Tag detected → handshake initiated
t=1s    NFC: AUTH_OK
t=1s    No motion detected (radar quiet, ultrasonic shows stationary object)
        → No access request sent (no approach photo, no decision)
        → NFC result cached but not acted on

t=30s   NFC: Re-scan → collar still there
        → Suppressed (same collar, already authenticated in last 60s)
        → No repeated API calls

t=300s  Dog wakes up, moves
        → Radar triggers
        → Fresh access request with cached NFC + new camera image
```

**Mitigation:** NFC authentication results are cached per collar_id for 60 seconds. Repeated NFC detections of the same collar without motion don't trigger repeated API calls. The full access pipeline (motion → proximity → capture → identify → decide) still requires motion as the first trigger.

### 3.5 Power Outage (Door Unit Loses Power)

**Scenario:** Home loses power. Door unit goes offline. Collar is battery-powered.

```
Door unit:
  - ESP32-CAM loses power → firmware event: PowerLost (if capacitor holds long enough)
  - No NFC reader available
  - No camera available
  - Door mechanism: linear actuator defaults to OPEN (fail-open for safety)
    OR defaults to CLOSED (fail-secure for security)
    (configurable in firmware, fail-open is recommended for pet safety)

Collar:
  - Still running on LiPo battery
  - GPS tracking continues
  - Geofence evaluation continues (on-device, no API needed)
  - Buzzer still works for breach alerts
  - WiFi upload fails (router may also be down) → events queued in SPIFFS
  - BLE advertising continues

On power restore:
  - Door unit reboots (30s watchdog)
  - Door unit re-initializes sensors, camera, NFC, BLE
  - Collar WiFi reconnects → uploads queued GPS points and geofence events
  - Normal operation resumes
```

**Key point:** The collar's geofencing works independently of the door unit, API, and WiFi. Even in a total infrastructure outage, the dog still gets buzzer warnings when approaching restricted zones.

---

## 4. Collar Lifecycle Edge Cases

### 4.1 Collar Transferred to New Dog

**Scenario:** Luna's collar is re-assigned to new dog Bella.

```
SPA flow:
1. Owner goes to /collars/1 (Luna's Collar)
2. Changes "Linked Animal" from Luna to Bella
3. Changes name from "Luna's Collar" to "Bella's Collar"
4. API: Updates CollarDevice.AnimalId to Bella's ID

Effects:
- NFC auth now maps collar_id → Bella (not Luna)
- Camera + collar fusion now checks if camera says "Bella" AND collar says "Bella"
- Location history stays linked to collar device (historical tracks remain)
- Old association (Luna) preserved in access logs (AnimalId is per-event, not per-collar)
```

**Data integrity:** Access log events store AnimalId directly, not through the collar. So historical events for Luna remain correct even after the collar is re-assigned.

### 4.2 Multiple Collars for Same Dog

**Scenario:** Owner has a backup collar for Luna (e.g., one for daily use, one for outdoor adventures with larger battery).

```
Database:
- CollarDevice #1: "Luna's Daily Collar" → AnimalId: 5 (Luna)
- CollarDevice #2: "Luna's Adventure Collar" → AnimalId: 5 (Luna)

Behavior:
- Either collar authenticates as Luna
- Both collars track separately (different location streams)
- Geofences apply to both (or can be per-collar)
- SPA shows both collars in Luna's profile

Edge case: Both collars active simultaneously (one on, one charging)?
- Only one should be on Luna at a time
- If both are GPS-tracking, API uses the most recent fix from either
- BLE: Door might see both advertisements → uses the one with strongest RSSI
```

### 4.3 Collar Firmware Crash Loop

**Scenario:** OTA update introduced a bug. Collar crashes on boot.

```
Boot 1: setup() → crash → watchdog reboot (30s)
Boot 2: setup() → crash → watchdog reboot (30s)
Boot 3: ESP32-S3 bootloader detects repeated crashes
         → Rolls back to previous firmware partition (OTA_0 → OTA_1 or vice versa)
Boot 4: Previous known-good firmware boots successfully
         → Collar reports firmware version to API (old version)
         → API marks the firmware deployment as "rolled_back"
         → Owner notified: "Luna's collar rolled back to v1.0.0 after update failure"
```

**Mitigation:** ESP32-S3 Secure Boot v2 with dual OTA partitions. The self-test suite runs on first boot after OTA. If self-test fails → immediate rollback. If the firmware crashes 3 times before calling `esp_ota_mark_app_valid_cancel_rollback()` → bootloader auto-rolls back.

---

## 5. API Scalability Considerations

### 5.1 Many Collars, Many Points

| Collars | Points/Day (1Hz, 4hr active) | Annual Storage |
|---------|------------------------------|----------------|
| 1 | ~14,400 | ~6 MB |
| 10 | ~144,000 | ~63 MB |
| 100 | ~1,440,000 | ~630 MB |
| 1,000 | ~14,400,000 | ~6.3 GB |
| 10,000 | ~144,000,000 | ~63 GB |

At 100+ collars, consider:
- TimescaleDB hypertables for automatic time-partitioning
- Compression on chunks older than 7 days (~10x reduction)
- More aggressive downsampling (1-point/min after 24h instead of 7 days)
- Separate read replica for map/analytics queries

At 10,000+ collars:
- Dedicated time-series database (TimescaleDB or InfluxDB)
- Horizontal sharding by collar_id
- Message queue (RabbitMQ/Kafka) between collar upload and database write
- CDN for satellite tile caching

**For the typical self-hosted user (1-5 collars):** Standard PostgreSQL handles this trivially. No special infrastructure needed.

### 5.2 Concurrent NFC Handshakes

Each door unit handles one NFC handshake at a time (physical limitation of PN532). With two door units (inside + outside), the system can handle 2 concurrent handshakes.

NFC handshake throughput: ~25 handshakes/second per door unit (40ms each). This is far more than needed — even a house full of dogs queues at most 3-4 handshakes.

### 5.3 BLE Scan Collisions

When scanning for collar BLE advertisements, the door unit may see advertisements from neighbors' BLE devices, AirTags, fitness trackers, etc.

**Filtering:** The collar scanner only looks for advertisements containing the SDD collar service UUID (`5a6d0001-...`). Random BLE devices are ignored. Even in a dense apartment building with hundreds of BLE devices, the scan filters effectively because it matches on a specific 128-bit UUID.
