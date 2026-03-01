# Smart Collar Companion Device — Design Document

## Overview

An optional, collar-mounted device that adds a second layer of identification and tracking to the Smart Dog Door system. The collar provides:

1. **NFC tap-to-identify** — instant, cryptographic dog identification at the door (no camera needed)
2. **GPS tracking** — real-time location within the yard
3. **Virtual geofencing** — owner-defined allowed/restricted zones using satellite imagery
4. **Movement analytics** — historical path tracking, time-in-zone stats, anomaly alerts
5. **BLE/WiFi data backhaul** — sends telemetry to the door unit and API

The collar works alongside the existing camera-based perceptual hash identification, not as a replacement. When both signals agree, confidence is highest. When the collar is absent (battery dead, removed for bath), the system falls back to vision-only identification.

---

## 1. Hardware Design

### 1.1 MCU Selection: ESP32-S3-MINI-1

| Feature | ESP32-S3-MINI-1 | nRF52840 | STM32WB55 |
|---------|-----------------|----------|-----------|
| BLE 5.0 | Yes | Yes | Yes |
| WiFi | Yes (802.11 b/g/n) | No | No |
| NFC | Via external PN532 | Built-in NFC-A | No |
| GPS | Via external module | Via external module | Via external module |
| Flash | 8MB | 1MB | 1MB |
| RAM | 512KB SRAM + 8MB PSRAM | 256KB | 256KB |
| Deep sleep current | ~10uA | ~2uA | ~3uA |
| Arduino/PlatformIO | Yes | Yes | Partial |
| Cost | ~$4 | ~$6 | ~$8 |

**Choice: ESP32-S3-MINI-1** — same ecosystem as the door firmware (shared libraries, toolchain, OTA infrastructure), WiFi for direct API connectivity when in range, and the lowest cost. The slightly higher deep-sleep current vs nRF52840 is acceptable given the GPS module dominates power draw anyway.

### 1.2 Bill of Materials

| # | Component | Model | Purpose | Est. Cost |
|---|-----------|-------|---------|-----------|
| 1 | MCU | ESP32-S3-MINI-1 | Main controller, BLE + WiFi | $4 |
| 2 | GPS Module | u-blox MAX-M10S | GNSS receiver (GPS/GLONASS/Galileo), 1.5m accuracy | $12 |
| 3 | NFC Module | PN532 (I2C mode) | NFC-A tag emulation + reader | $6 |
| 4 | IMU | LSM6DSO (accel + gyro) | Motion detection, step counting, activity classification | $3 |
| 5 | Battery | LiPo 3.7V 500mAh (14250 size) | Power source, collar-sized form factor | $5 |
| 6 | Charge IC | TP4056 with protection | USB-C charging, overcharge/overdischarge protection | $1 |
| 7 | Voltage Reg | TPS63001 (buck-boost) | Stable 3.3V from 3.0-4.2V LiPo range | $2 |
| 8 | Antenna | Molex 2065600100 (ceramic chip) | Dual BLE + WiFi, compact for collar | $2 |
| 9 | GPS Antenna | Taoglas CGGP.18.2.A.02 | Compact ceramic patch, 18mm | $4 |
| 10 | Enclosure | Custom 3D-printed TPU | Waterproof (IP67), shock-resistant, collar clip mount | $3 |
| 11 | PCB | Custom 2-layer, 30x20mm | Compact layout for collar form factor | $2 |
| 12 | Buzzer | Passive piezo 5mm | Audible geofence alerts, find-my-dog | $0.50 |
| 13 | LED | WS2812B-Mini (RGB) | Status indicator (charge, GPS fix, alert) | $0.50 |
| | **Total** | | | **~$45** |

### 1.3 Pin Assignment (ESP32-S3-MINI-1)

```
ESP32-S3-MINI-1
├── GPIO1  (I2C SDA) ──→ PN532 + LSM6DSO (shared I2C bus)
├── GPIO2  (I2C SCL) ──→ PN532 + LSM6DSO
├── GPIO3  (UART TX)  ──→ MAX-M10S (RX)
├── GPIO4  (UART RX)  ←── MAX-M10S (TX)
├── GPIO5  (IRQ)      ←── PN532 interrupt (NFC field detected)
├── GPIO6  (IRQ)      ←── LSM6DSO interrupt (motion wakeup)
├── GPIO7  (GPIO)     ──→ Piezo buzzer (PWM)
├── GPIO8  (GPIO)     ──→ WS2812B data
├── GPIO9  (ADC)      ←── Battery voltage divider (100k/100k)
├── GPIO10 (GPIO)     ──→ GPS enable (power gate)
├── GPIO11 (GPIO)     ──→ PN532 reset
└── USB-C             ──→ TP4056 charge input + ESP32-S3 native USB (flashing/debug)
```

### 1.4 Power Budget & Battery Life

| Mode | Components Active | Current Draw | Duration |
|------|-------------------|-------------|----------|
| **Deep Sleep** | RTC + LSM6DSO (motion wakeup) | ~25uA | Overnight/inactive |
| **NFC Standby** | MCU light sleep + PN532 polling | ~5mA | Near door, waiting for tap |
| **GPS Tracking** | MCU + GPS (1Hz fixes) | ~30mA | Active outdoor time |
| **GPS + WiFi Upload** | MCU + GPS + WiFi burst | ~180mA | 2-second bursts every 30s |
| **Full Active** | All peripherals | ~220mA | Rare (firmware update) |

**Estimated battery life (500mAh LiPo):**
- Yard dog (4hrs GPS/day, 20hrs sleep): **~5 days**
- Indoor dog (1hr GPS/day, 23hrs sleep): **~14 days**
- Sleep only (collar off, deep sleep): **~230 days**

Power strategy:
- IMU wakeup from deep sleep on motion (eliminates idle GPS drain)
- GPS power-gated via MOSFET on GPIO10 (completely off in sleep)
- WiFi used in burst mode only (connect → upload batch → disconnect)
- NFC polling only when BLE proximity to door unit detected (RSSI > -60dBm)
- Adaptive GPS rate: 1Hz when moving, 0.1Hz when stationary (IMU-informed)

### 1.5 Physical Design

```
┌─────────────────────────────────┐
│  Collar Clip (TPU flex mount)   │
├─────────────────────────────────┤
│ ┌───────────────────────────┐   │
│ │  GPS Antenna (top face)   │   │  45mm
│ │                           │   │
│ │  ┌─────┐ ┌─────┐ ┌────┐  │   │
│ │  │ESP32│ │PN532│ │GPS │  │   │  30mm
│ │  │ S3  │ │     │ │M10S│  │   │
│ │  └─────┘ └─────┘ └────┘  │   │
│ │  ┌─────┐ ┌─────┐         │   │
│ │  │LiPo │ │IMU  │ [LED]   │   │
│ │  │500mA│ │     │ [BZR]   │   │
│ │  └─────┘ └─────┘         │   │
│ └───────────────────────────┘   │
│          USB-C port             │
├─────────────────────────────────┤
│  NFC Antenna (bottom face)      │
└─────────────────────────────────┘

Dimensions: ~45 x 30 x 15mm
Weight: ~25g (incl. battery)
Rating: IP67 (potted USB-C port cap)
```

---

## 2. Firmware Architecture (Collar)

The collar firmware is a separate PlatformIO project under `firmware/collar/`, sharing common libraries with the door firmware where possible.

### 2.1 Project Structure

```
firmware/collar/
├── platformio.ini
├── src/
│   ├── main.cpp              # Boot, sleep/wake state machine
│   ├── config.h              # Pins, UUIDs, timing constants
│   ├── nfc_manager.cpp/h     # PN532 tag emulation + door handshake
│   ├── gps_tracker.cpp/h     # u-blox NMEA parsing, fix management
│   ├── imu_manager.cpp/h     # LSM6DSO motion detection, step counter
│   ├── ble_collar.cpp/h      # BLE peripheral (advertise to door, phone)
│   ├── wifi_uploader.cpp/h   # Batch telemetry upload to API
│   ├── geofence.cpp/h        # Virtual fence evaluation engine
│   ├── power_manager.cpp/h   # Battery monitoring, sleep modes, GPS gating
│   ├── buzzer.cpp/h          # Alert tones (geofence breach, find-my-dog)
│   └── storage.cpp/h         # NVS for config, SPIFFS for track logs
├── lib/
│   └── shared/               # Symlink to common crypto/protocol code
└── test/
    └── test_geofence.cpp     # Unit tests for fence math
```

### 2.2 State Machine

```
                    ┌─────────────┐
                    │  DEEP SLEEP │ ←──── No motion for 5min
                    │  (~25uA)    │       or manual off
                    └──────┬──────┘
                           │ IMU motion interrupt
                           ▼
                    ┌─────────────┐
                    │  WAKE &     │
                    │  CLASSIFY   │──── Brief motion? → back to sleep
                    └──────┬──────┘
                           │ Sustained motion (dog is active)
                           ▼
              ┌────────────────────────┐
              │   GPS TRACKING MODE    │
              │  GPS on, 1Hz fixes     │
              │  BLE advertising       │
              │  Geofence evaluation   │
              │  Track log buffered    │
              └────────┬───────┬───────┘
                       │       │
          BLE sees door│       │ WiFi upload interval
          RSSI > -60dBm│       │ (every 30s if fixes buffered)
                       ▼       ▼
              ┌──────────┐  ┌──────────────┐
              │ NFC READY │  │ WIFI UPLOAD  │
              │ PN532 on  │  │ Connect, POST│
              │ Tag emul. │  │ batch, disco │
              └──────────┘  └──────────────┘
```

### 2.3 NFC Door Handshake Protocol

The collar emulates an NFC-A (ISO 14443-4) tag. When the dog approaches the door, the door-side PN532 reader detects the collar and performs a challenge-response:

```
Door (Reader)                    Collar (Tag Emulation)
     │                                │
     │──── RATS (select) ────────────▶│
     │                                │
     │◀─── ATS (collar ID + caps) ────│
     │                                │
     │──── AUTH_CHALLENGE ───────────▶│  (32-byte random nonce)
     │                                │
     │◀─── AUTH_RESPONSE ─────────────│  (HMAC-SHA256(nonce, shared_secret))
     │                                │  + collar_id + battery_level
     │                                │
     │──── AUTH_RESULT ──────────────▶│  (ACK/NAK + door_status)
     │                                │
```

**Security model:**
- Each collar has a unique 128-bit `collar_id` and 256-bit `shared_secret`, provisioned during pairing
- The shared secret is stored in ESP32-S3 NVS encrypted storage (eFuse-backed)
- Challenge-response prevents replay attacks
- The door firmware forwards `collar_id` + HMAC to the API for verification
- API maps `collar_id` → `animal_id` (new `CollarDevice` model)

### 2.4 BLE Collar Service

```
Service: "collar-svc"  UUID: 5a6d0001-8e7f-4b3c-9d2a-1c6e3f8b7d4e

Characteristics:
├── Location     (READ/NOTIFY)  UUID: ...0002
│   └── {lat, lng, altitude, accuracy, speed, heading, fixAge, satellites}
│
├── Geofence     (READ/NOTIFY)  UUID: ...0003
│   └── {inBounds: bool, nearestFenceId, distanceToFence, breachDirection}
│
├── Battery      (READ/NOTIFY)  UUID: ...0004
│   └── {voltage, percentage, isCharging, estimatedHoursRemaining}
│
├── Activity     (READ/NOTIFY)  UUID: ...0005
│   └── {steps, distanceTraveled, activeMinutes, currentState: idle|walking|running}
│
├── Command      (WRITE)        UUID: ...0006
│   └── "buzz" | "locate" | "sleep" | "wake" | "update_fences:{json}"
│
└── Config       (READ/WRITE)   UUID: ...0007
    └── {gpsRate, uploadInterval, geofenceEnabled, sleepTimeout}
```

The door's ESP32-CAM can read the collar's BLE Location characteristic for proximity-based NFC activation (only power on PN532 reader when collar BLE RSSI indicates the dog is within ~2m).

### 2.5 Geofence Engine (On-Device)

The collar evaluates geofences locally for instant alerts without network dependency.

**Fence types supported:**
1. **Polygon** — arbitrary shape defined by vertex array (yard boundary)
2. **Circle** — center point + radius (stay-away zone around garden/pool)
3. **Corridor** — polyline + width (allowed path to neighbor's yard)

**Algorithm:**
- Polygon containment: ray-casting (point-in-polygon), O(n) per fence vertex count
- Circle containment: haversine distance, O(1)
- Corridor containment: perpendicular distance to nearest segment, O(n) per segment count
- Evaluation runs at GPS fix rate (1Hz moving, 0.1Hz stationary)
- Fences stored in NVS as compact binary (max 20 fences, ~2KB)

**Breach response:**
1. Immediate piezo buzz (configurable pattern per fence)
2. BLE notify to phone app (if connected)
3. WiFi upload breach event to API (high-priority, bypasses batch interval)
4. Repeated alerts every 10s while out of bounds

---

## 3. Door Firmware Changes

The existing ESP32-CAM door firmware needs additions to act as an NFC reader and BLE collar scanner.

### 3.1 New Hardware on Door Unit

| Component | Model | Purpose | Cost |
|-----------|-------|---------|------|
| NFC Reader | PN532 (I2C) | Read collar NFC tag at door | $6 |

Wiring: Share I2C with existing bus or use a second I2C on available GPIOs. The PN532 IRQ connects to a free GPIO for interrupt-driven detection.

### 3.2 New Door Firmware Modules

```
firmware/src/
├── nfc_reader.cpp/h      # PN532 reader, challenge-response protocol
├── collar_scanner.cpp/h  # BLE scan for collar advertisements, RSSI tracking
└── (existing files updated)
    ├── config.h           # + NFC pins, collar BLE service UUID
    └── main.cpp           # + NFC/collar check in access pipeline
```

### 3.3 Updated Access Pipeline

Current pipeline (camera-only):
```
Radar → Ultrasonic → Capture → TFLite → API Recognition → Decision
```

New pipeline (camera + collar fusion):
```
Radar → Ultrasonic → Collar BLE scan (parallel) ──┐
    │                                               │
    └──→ Capture → TFLite ──┐                       │
                             │                       │
                    ┌────────┴───────────────────────┘
                    ▼
              NFC Handshake (if collar BLE detected)
                    │
                    ▼
              API Recognition (image + collar_id)
                    │
                    ▼
              Fused Decision (weighted confidence)
```

**Fusion scoring:**
- Camera pHash match alone: confidence as-is (existing behavior)
- Collar NFC match alone: 0.95 confidence (cryptographic, very reliable)
- Both agree (same animal): min(camera_conf + 0.15, 1.0) — boosted confidence
- Disagree (different animals): flag for review, deny access, alert owner

---

## 4. API Changes

### 4.1 New Models

```csharp
// Models/CollarDevice.cs
public class CollarDevice
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public int UserId { get; set; }
    public string CollarId { get; set; }         // 128-bit hex, unique
    public string SharedSecretHash { get; set; }  // BCrypt hash of shared secret
    public string? Name { get; set; }             // "Luna's Collar"
    public string? FirmwareVersion { get; set; }
    public float? BatteryLevel { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Animal Animal { get; set; }
    public User User { get; set; }
    public List<Geofence> Geofences { get; set; }
}

// Models/LocationPoint.cs
public class LocationPoint
{
    public long Id { get; set; }
    public int CollarDeviceId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public float? Altitude { get; set; }
    public float? Accuracy { get; set; }
    public float? Speed { get; set; }
    public float? Heading { get; set; }
    public int? Satellites { get; set; }
    public float? BatteryVoltage { get; set; }
    public DateTime Timestamp { get; set; }

    public CollarDevice CollarDevice { get; set; }
}

// Models/Geofence.cs
public class Geofence
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; }              // "Backyard", "Garden (no-go)"
    public GeofenceType Type { get; set; }         // Polygon, Circle, Corridor
    public string BoundaryJson { get; set; }       // GeoJSON geometry
    public GeofenceAction Action { get; set; }     // Allow, Deny
    public bool IsEnabled { get; set; }
    public bool BuzzerEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; }
    public List<CollarDevice> AppliesTo { get; set; }  // Empty = all collars
}

public enum GeofenceType { Polygon, Circle, Corridor }
public enum GeofenceAction { Allow, Deny }

// Models/GeofenceEvent.cs
public class GeofenceEvent
{
    public long Id { get; set; }
    public int CollarDeviceId { get; set; }
    public int GeofenceId { get; set; }
    public GeofenceEventType EventType { get; set; }  // Entered, Exited, Breach
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }

    public CollarDevice CollarDevice { get; set; }
    public Geofence Geofence { get; set; }
}

public enum GeofenceEventType { Entered, Exited, Breach }
```

### 4.2 New API Endpoints

```
Collar Management
  POST   /api/v1/collars                    # Register & pair new collar
  GET    /api/v1/collars                    # List user's collar devices
  GET    /api/v1/collars/{id}               # Get collar detail + last known location
  PUT    /api/v1/collars/{id}               # Update collar settings (name, active)
  DELETE /api/v1/collars/{id}               # Unpair and deactivate collar
  POST   /api/v1/collars/{id}/verify        # Verify NFC challenge-response (door calls this)

Location Tracking
  POST   /api/v1/collars/{id}/locations     # Batch upload GPS points (collar calls this)
  GET    /api/v1/collars/{id}/locations      # Query location history (time range, downsample)
  GET    /api/v1/collars/{id}/location       # Current/latest location

Geofencing
  POST   /api/v1/geofences                  # Create geofence (GeoJSON boundary)
  GET    /api/v1/geofences                  # List user's geofences
  GET    /api/v1/geofences/{id}             # Get geofence detail
  PUT    /api/v1/geofences/{id}             # Update boundary, action, settings
  DELETE /api/v1/geofences/{id}             # Remove geofence
  GET    /api/v1/geofences/events           # Geofence event history (breach log)

Updated Door Access
  POST   /api/v1/doors/access-request       # + optional collar_id & hmac fields

Satellite Imagery Proxy
  GET    /api/v1/maps/satellite?bbox=...    # Proxy to public tile server (caches tiles)
```

### 4.3 Updated Access Request Flow

```csharp
// Enhanced AccessRequestDto
public class AccessRequestDto
{
    // Existing fields
    public IFormFile Image { get; set; }
    public string ApiKey { get; set; }
    public string Side { get; set; }

    // New optional collar fields
    public string? CollarId { get; set; }
    public string? Nonce { get; set; }           // The challenge nonce sent to collar
    public string? HmacResponse { get; set; }     // Collar's HMAC-SHA256 response
}

// Enhanced AccessResponseDto
public class AccessResponseDto
{
    // Existing fields
    public bool Allowed { get; set; }
    public int? AnimalId { get; set; }
    public string? AnimalName { get; set; }
    public float ConfidenceScore { get; set; }
    public string? Reason { get; set; }
    public string? Direction { get; set; }

    // New fields
    public string? IdentificationMethod { get; set; }  // "camera", "collar", "both"
    public float? CameraConfidence { get; set; }
    public float? CollarConfidence { get; set; }
}
```

### 4.4 Location Data Retention

GPS points accumulate fast (1Hz = 86,400 points/day). Retention policy:

| Age | Resolution | Storage |
|-----|-----------|---------|
| 0-24 hours | Full (1Hz raw) | ~3.5MB/day per collar |
| 1-7 days | Downsampled to 1 point/10s | ~350KB/day |
| 7-30 days | Downsampled to 1 point/min | ~6KB/day |
| 30+ days | Daily summary (distance, time active, zones visited) | ~200B/day |

Implemented as a background service (`LocationRetentionService`) using EF Core bulk operations, triggered daily.

---

## 5. React SPA Changes

### 5.1 New Pages

#### Collar Management (`/collars`)
- List of registered collar devices with battery level, last seen, firmware version
- Pairing flow: generate shared secret, display QR code for collar provisioning
- Per-collar settings: GPS rate, upload interval, buzzer volume

#### Live Map (`/map`)
- Full-screen satellite imagery map (Leaflet + public tile provider)
- Real-time dog location overlay (WebSocket or polling)
- Breadcrumb trail (last N minutes of movement)
- Toggle between satellite and street map views
- Geofence boundaries rendered as colored overlays

#### Geofence Editor (`/map/geofences`)
- Draw polygons, circles, corridors directly on satellite map
- Leaflet.draw plugin for interactive boundary creation
- Assign fence action (allow/deny) and target collars
- Color coding: green = allowed zone, red = restricted zone
- Edit existing fences by dragging vertices

#### Movement History (`/collars/{id}/history`)
- Date range picker for historical track playback
- Animated playback of movement path on satellite map
- Heat map overlay showing time-spent density
- Daily stats: distance traveled, active time, zones visited
- Export track as GPX file

#### Geofence Events (`/geofence-events`)
- Table of breach events with timestamp, collar, fence name, location
- Click to view breach location on map
- Filter by collar, fence, date range

### 5.2 Map Technology Stack

```
Leaflet 1.9+                   # Map rendering engine
├── leaflet-draw               # Polygon/circle drawing tools
├── leaflet-realtime            # Live location updates
└── leaflet-heat                # Heat map overlay

Tile Providers (public, free):
├── Esri World Imagery          # Satellite tiles (primary)
├── OpenStreetMap               # Street map fallback
└── Stamen Terrain              # Topographic option

No API key required for these public tile servers.
```

### 5.3 Updated Navigation

```
Dashboard | Animals | Access Log | Map | Collars | Settings | Notifications | Profile
                                   ▲       ▲
                                   │       └── NEW
                                   └────────── NEW (with sub-routes for geofence editor, history)
```

---

## 6. Virtual Geofencing — Detailed Design

### 6.1 Satellite Imagery Integration

The owner defines geofences by drawing on satellite imagery of their property:

1. Owner navigates to `/map` and sees their property via public satellite tiles (Esri World Imagery)
2. Zooms to their yard — satellite imagery shows the house, yard, garden, pool, street
3. Uses drawing tools to trace boundaries:
   - **Allowed zone** (green polygon): trace the yard perimeter
   - **Restricted zone** (red circle): draw over the swimming pool, garden bed, or street
   - **Allowed corridor** (blue polyline + width): path to the neighbor's yard for playdates
4. Saves fences — API stores GeoJSON, pushes to collar via next BLE sync or WiFi

### 6.2 Fence Evaluation Logic

```
For each GPS fix:
  1. Check all DENY fences first
     - If inside ANY deny fence → BREACH (alert + buzz)
  2. Check all ALLOW fences
     - If inside at least one allow fence → IN BOUNDS (ok)
     - If no allow fences defined → skip (unrestricted)
  3. If allow fences exist but dog is outside ALL of them → BREACH

Priority: Deny fences always override allow fences.
```

### 6.3 Fence Sync Protocol

Fences are synced from API to collar via two channels:

1. **BLE** (when phone app is nearby): Push updated fence set via Command characteristic
2. **WiFi** (during telemetry upload): Collar checks `GET /api/v1/geofences?collarId={id}&updatedSince={last_sync}` and downloads changes

Fence data format on device (compact binary, ~100 bytes per polygon fence):
```
[type:1][id:2][action:1][vertex_count:1][vertices:N*8][buzzer:1][name_len:1][name:N]
```

### 6.4 Edge Cases

- **GPS drift near fence boundary**: 3-meter hysteresis buffer before triggering breach
- **GPS cold start**: No breach alerts until first valid fix (HDOP < 5.0)
- **Tunnel/covered area**: If GPS fix age > 30s, use last known position, don't alert
- **Multiple dogs**: Each collar evaluates independently; fences can target specific collars or all

---

## 7. Pairing & Provisioning Flow

### 7.1 Initial Collar Setup

```
Owner                           SPA                         API                      Collar
  │                              │                           │                         │
  │── "Add Collar" ────────────▶ │                           │                         │
  │                              │── POST /collars ─────────▶│                         │
  │                              │                           │── Generate:             │
  │                              │                           │   collar_id (128-bit)   │
  │                              │                           │   shared_secret (256-bit)│
  │                              │                           │   pairing_code (6-digit) │
  │                              │◀── {collar_id, secret,   │                         │
  │                              │     pairing_code, qr_url} │                         │
  │                              │                           │                         │
  │◀── Display QR code ─────────│                           │                         │
  │    + pairing code            │                           │                         │
  │                              │                           │                         │
  │── Hold collar near phone ───────────────────────────────────────────────────────▶ │
  │   (or enter pairing code)    │                           │                 BLE pair │
  │                              │                           │                         │
  │                              │                           │◀── Collar confirms ─────│
  │                              │                           │    (WiFi: POST /verify)  │
  │                              │                           │                         │
  │◀── "Collar paired!" ────────│                           │                         │
```

### 7.2 Door NFC Reader Setup

The PN532 reader is added to the existing door hardware. During door firmware update:
1. Door discovers collar BLE advertisements and stores known collar MAC addresses
2. When a collar approaches (RSSI threshold), door activates PN532 reader
3. NFC handshake runs automatically — no owner action needed

---

## 8. Data Flow Summary

```
┌──────────────┐         BLE/NFC          ┌──────────────┐
│              │◀─────────────────────────▶│              │
│  Collar      │    (identification +      │  Door Unit   │
│  (on dog)    │     proximity data)       │  (ESP32-CAM) │
│              │                           │              │
└──────┬───────┘                           └──────┬───────┘
       │                                          │
       │ WiFi (batch upload)          WiFi/Cellular│
       │ every 30s when active        (access req) │
       │                                          │
       ▼                                          ▼
┌─────────────────────────────────────────────────────┐
│                    .NET API                          │
│                                                     │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────┐  │
│  │ CollarSvc   │  │ DoorSvc      │  │ GeofenceSvc│  │
│  │ - verify NFC│  │ - fused ID   │  │ - CRUD     │  │
│  │ - locations │  │ - decisions  │  │ - events   │  │
│  │ - battery   │  │ - logging    │  │ - sync     │  │
│  └─────────────┘  └──────────────┘  └────────────┘  │
│                                                     │
│  ┌──────────────────────────────────────────────┐    │
│  │  PostgreSQL                                  │    │
│  │  collar_devices | location_points | geofences│    │
│  │  geofence_events | (existing tables)         │    │
│  └──────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
       ▲
       │ HTTPS
       ▼
┌─────────────────────────┐
│     React SPA           │
│  Map | Collars | Fences │
└─────────────────────────┘
```

---

## 9. Implementation Phases

### Phase 1: Collar Hardware + Basic Firmware
- PCB design and 3D-print enclosure
- ESP32-S3 firmware: boot, BLE advertising, GPS tracking, WiFi upload
- Power management (deep sleep, GPS gating, IMU wakeup)
- Basic NVS storage for config and credentials

### Phase 2: NFC Door Integration
- Add PN532 reader to door unit
- Implement NFC challenge-response protocol (door + collar firmware)
- Update door firmware access pipeline for collar-aware identification
- API: `CollarDevice` model, pairing endpoint, NFC verify endpoint

### Phase 3: API + Location Tracking
- API: Location batch upload, query, and retention service
- API: Collar CRUD endpoints
- SPA: Collar management page, live map with real-time location
- SPA: Movement history with track playback

### Phase 4: Virtual Geofencing
- API: Geofence CRUD, event logging
- Collar firmware: on-device geofence engine (polygon, circle, corridor)
- Collar firmware: fence sync (BLE + WiFi)
- SPA: Geofence editor on satellite map (Leaflet.draw)
- SPA: Geofence event log and breach alerts

### Phase 5: Polish + Analytics
- SPA: Heat map, daily movement stats, GPX export
- API: Movement analytics (distance/day, favorite zones, anomaly detection)
- Collar firmware: OTA updates via WiFi
- Notification integration (SMS/email on geofence breach, low battery)
- Find-my-dog feature (trigger buzzer from SPA)

---

## 10. Open Questions

1. **Collar-to-collar**: Should collars be aware of each other (multi-dog proximity, social tracking)?
2. **Third-party satellite providers**: Esri World Imagery is free but limited zoom. Integrate Mapbox/Google for higher resolution at cost?
3. **Indoor tracking**: BLE triangulation using door unit + phone as anchors when GPS is unavailable indoors?
4. **Regulatory**: FCC/CE certification for the collar's radio emissions (ESP32-S3 module is pre-certified, but full assembly may need testing)?
5. **Collar size variants**: One size fits all, or small/medium/large with different battery capacities?
