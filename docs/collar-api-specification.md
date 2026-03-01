# Collar API Specification

## Overview

REST API endpoints for the collar companion device system, extending the existing Smart Dog Door API at `/api/v1/`. All endpoints require JWT Bearer authentication unless otherwise noted.

---

## 1. Collar Device Management

### POST /api/v1/collars — Register & Pair New Collar

Creates a new collar device record and generates pairing credentials.

**Request:**
```json
{
    "animalId": 5,
    "name": "Luna's Collar"
}
```

**Response (201 Created):**
```json
{
    "id": 1,
    "collarId": "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6",
    "sharedSecret": "7f8e9d0c1b2a3948576605f4e3d2c1b0a9f8e7d6c5b4a3928170605f4e3d2c1b",
    "pairingCode": "847293",
    "animalId": 5,
    "animalName": "Luna",
    "name": "Luna's Collar",
    "createdAt": "2026-02-28T15:30:00Z"
}
```

**Notes:**
- `sharedSecret` is returned ONLY in this response (never again). The API stores a BCrypt hash.
- `pairingCode` is a 6-digit code for manual collar provisioning (valid for 10 minutes).
- The `collarId` is a 128-bit hex string that uniquely identifies the collar on the NFC/BLE layer.

### GET /api/v1/collars — List User's Collars

**Response (200 OK):**
```json
[
    {
        "id": 1,
        "collarId": "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6",
        "animalId": 5,
        "animalName": "Luna",
        "name": "Luna's Collar",
        "firmwareVersion": "1.2.0",
        "batteryLevel": 78.5,
        "lastSeenAt": "2026-02-28T15:45:00Z",
        "isActive": true,
        "lastLocation": {
            "latitude": 33.44842,
            "longitude": -112.07395,
            "accuracy": 2.5,
            "timestamp": "2026-02-28T15:44:55Z"
        }
    }
]
```

### GET /api/v1/collars/{id} — Get Collar Detail

**Response (200 OK):**
```json
{
    "id": 1,
    "collarId": "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6",
    "animalId": 5,
    "animalName": "Luna",
    "name": "Luna's Collar",
    "firmwareVersion": "1.2.0",
    "batteryLevel": 78.5,
    "lastSeenAt": "2026-02-28T15:45:00Z",
    "isActive": true,
    "lastLocation": {
        "latitude": 33.44842,
        "longitude": -112.07395,
        "altitude": 337.2,
        "accuracy": 2.5,
        "speed": 1.2,
        "heading": 45.0,
        "satellites": 12,
        "timestamp": "2026-02-28T15:44:55Z"
    },
    "stats": {
        "distanceTodayM": 1523.4,
        "activeMinutesToday": 47,
        "stepsToday": 3204,
        "breachesToday": 0,
        "avgDailyDistanceM": 2100.0,
        "avgDailyActiveMinutes": 65
    },
    "createdAt": "2026-02-15T10:00:00Z"
}
```

### PUT /api/v1/collars/{id} — Update Collar Settings

**Request:**
```json
{
    "name": "Luna's New Collar",
    "isActive": true,
    "animalId": 5
}
```

**Response (200 OK):** Updated collar object (same schema as GET).

### DELETE /api/v1/collars/{id} — Unpair Collar

Deactivates the collar and removes the shared secret. The collar will no longer authenticate with any door.

**Response (204 No Content)**

### POST /api/v1/collars/{id}/verify — NFC Verification (Door → API)

Called by the door firmware during the NFC handshake to verify the collar's HMAC response.

**Request:**
```json
{
    "apiKey": "door-api-key-here",
    "collarId": "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6",
    "nonce": "base64-encoded-32-byte-nonce",
    "doorId": "base64-encoded-16-byte-door-id",
    "timestamp": 1740754800,
    "hmacResponse": "base64-encoded-32-byte-hmac"
}
```

**Response (200 OK) — Verified:**
```json
{
    "verified": true,
    "animalId": 5,
    "animalName": "Luna",
    "isAllowed": true,
    "batteryLevel": 78.5
}
```

**Response (200 OK) — Failed:**
```json
{
    "verified": false,
    "reason": "hmac_mismatch"
}
```

**Notes:**
- This endpoint is authenticated by `apiKey` (same as existing door endpoints), not JWT.
- The API reconstructs the HMAC using the stored shared secret and compares with `FixedTimeEquals`.
- Timestamp validated: `|server_time - timestamp| <= 30 seconds`.

### POST /api/v1/collars/{id}/rotate-secret — Rotate Shared Secret

Generates a new shared secret. The collar must be re-provisioned.

**Response (200 OK):**
```json
{
    "newSharedSecret": "new-256-bit-hex-string",
    "rotatedAt": "2026-02-28T16:00:00Z"
}
```

---

## 2. Location Tracking

### POST /api/v1/collars/{id}/locations — Batch Upload GPS Points

Called by the collar firmware during WiFi upload bursts. Accepts up to 1000 points per request.

**Request:**
```json
{
    "apiKey": "collar-api-key-or-door-api-key",
    "points": [
        {
            "lat": 33.44842,
            "lng": -112.07395,
            "alt": 337.2,
            "acc": 2.5,
            "spd": 1.2,
            "hdg": 45.0,
            "sat": 12,
            "bat": 3.85,
            "ts": 1740754800
        },
        {
            "lat": 33.44843,
            "lng": -112.07396,
            "alt": 337.1,
            "acc": 2.3,
            "spd": 1.1,
            "hdg": 44.0,
            "sat": 12,
            "bat": 3.85,
            "ts": 1740754801
        }
    ]
}
```

**Response (201 Created):**
```json
{
    "accepted": 2,
    "rejected": 0,
    "latestTimestamp": "2026-02-28T15:46:41Z"
}
```

**Notes:**
- Short field names (`lat`, `lng`, `alt`, `acc`, `spd`, `hdg`, `sat`, `bat`, `ts`) to minimize payload size from the collar's WiFi upload.
- `ts` is Unix epoch seconds.
- Points with `ts` older than 24 hours are rejected (stale data).
- The API updates `CollarDevice.LastSeenAt` and `BatteryLevel` with the latest point.

### GET /api/v1/collars/{id}/locations — Query Location History

**Query Parameters:**
| Param | Type | Default | Description |
|-------|------|---------|-------------|
| from | datetime | 1 hour ago | Start of time range (ISO 8601) |
| to | datetime | now | End of time range (ISO 8601) |
| maxPoints | int | 500 | Maximum points returned (server downsamples) |
| format | string | json | `json` or `geojson` |

**Response (200 OK) — JSON format:**
```json
{
    "collarId": 1,
    "animalName": "Luna",
    "from": "2026-02-28T14:00:00Z",
    "to": "2026-02-28T15:00:00Z",
    "pointCount": 360,
    "points": [
        {
            "lat": 33.44842,
            "lng": -112.07395,
            "alt": 337.2,
            "acc": 2.5,
            "spd": 1.2,
            "hdg": 45.0,
            "bat": 78.5,
            "ts": "2026-02-28T14:00:00Z"
        }
    ]
}
```

**Response (200 OK) — GeoJSON format:**
```json
{
    "type": "FeatureCollection",
    "features": [
        {
            "type": "Feature",
            "geometry": {
                "type": "LineString",
                "coordinates": [
                    [-112.07395, 33.44842, 337.2],
                    [-112.07396, 33.44843, 337.1]
                ]
            },
            "properties": {
                "collarId": 1,
                "animalName": "Luna",
                "from": "2026-02-28T14:00:00Z",
                "to": "2026-02-28T15:00:00Z",
                "pointCount": 360
            }
        }
    ]
}
```

**Downsampling algorithm:**
When `pointCount > maxPoints`, the API uses the Ramer-Douglas-Peucker algorithm to simplify the track while preserving shape. This keeps bends and turns while removing redundant points from straight segments.

### GET /api/v1/collars/{id}/location — Current Location

Returns the most recent GPS point.

**Response (200 OK):**
```json
{
    "latitude": 33.44842,
    "longitude": -112.07395,
    "altitude": 337.2,
    "accuracy": 2.5,
    "speed": 1.2,
    "heading": 45.0,
    "satellites": 12,
    "batteryVoltage": 3.85,
    "batteryPercentage": 78.5,
    "timestamp": "2026-02-28T15:46:41Z",
    "ageSeconds": 5
}
```

### GET /api/v1/collars/{id}/stats — Movement Statistics

**Query Parameters:**
| Param | Type | Default | Description |
|-------|------|---------|-------------|
| from | datetime | 7 days ago | Start date |
| to | datetime | now | End date |

**Response (200 OK):**
```json
{
    "collarId": 1,
    "animalName": "Luna",
    "period": {
        "from": "2026-02-21T00:00:00Z",
        "to": "2026-02-28T23:59:59Z"
    },
    "summary": {
        "totalDistanceM": 14700.0,
        "totalActiveMinutes": 455,
        "totalSteps": 22400,
        "avgDailyDistanceM": 2100.0,
        "avgDailyActiveMinutes": 65,
        "avgDailySteps": 3200,
        "totalBreaches": 2,
        "maxSpeedMs": 8.2
    },
    "daily": [
        {
            "date": "2026-02-28",
            "distanceM": 1523.4,
            "activeMinutes": 47,
            "steps": 3204,
            "breaches": 0,
            "firstActiveAt": "2026-02-28T07:15:00Z",
            "lastActiveAt": "2026-02-28T18:30:00Z"
        }
    ],
    "heatmap": {
        "bounds": {
            "north": 33.44860,
            "south": 33.44820,
            "east": -112.07370,
            "west": -112.07420
        },
        "gridSize": 20,
        "cells": [
            { "row": 5, "col": 10, "minutes": 45.2 },
            { "row": 6, "col": 10, "minutes": 32.1 },
            { "row": 5, "col": 11, "minutes": 28.7 }
        ]
    }
}
```

---

## 3. Geofencing

### POST /api/v1/geofences — Create Geofence

**Request:**
```json
{
    "name": "Backyard",
    "type": "polygon",
    "action": "allow",
    "buzzerPattern": "short",
    "isEnabled": true,
    "boundary": {
        "type": "Polygon",
        "coordinates": [[
            [-112.07410, 33.44850],
            [-112.07380, 33.44850],
            [-112.07380, 33.44830],
            [-112.07410, 33.44830],
            [-112.07410, 33.44850]
        ]]
    },
    "collarIds": []
}
```

**Notes on `boundary`:**
- Uses GeoJSON geometry format (RFC 7946)
- For `polygon`: standard GeoJSON Polygon (first and last coordinate must match)
- For `circle`: GeoJSON Point + `radius` field (extension)
  ```json
  {
      "type": "Point",
      "coordinates": [-112.07395, 33.44842],
      "radius": 3.0
  }
  ```
- For `corridor`: GeoJSON LineString + `width` field (extension)
  ```json
  {
      "type": "LineString",
      "coordinates": [
          [-112.07410, 33.44850],
          [-112.07420, 33.44855],
          [-112.07430, 33.44860]
      ],
      "width": 2.0
  }
  ```

**`collarIds`**: Empty array = applies to ALL collars. Specific IDs = only those collars.

**Response (201 Created):**
```json
{
    "id": 1,
    "name": "Backyard",
    "type": "polygon",
    "action": "allow",
    "buzzerPattern": "short",
    "isEnabled": true,
    "boundary": { "..." },
    "collarIds": [],
    "areaM2": 600.0,
    "perimeterM": 100.0,
    "createdAt": "2026-02-28T16:00:00Z",
    "updatedAt": "2026-02-28T16:00:00Z",
    "version": 1
}
```

### GET /api/v1/geofences — List Geofences

**Query Parameters:**
| Param | Type | Default | Description |
|-------|------|---------|-------------|
| collarId | int | null | Filter fences applicable to a specific collar |
| action | string | null | Filter by "allow" or "deny" |

**Response (200 OK):** Array of geofence objects (same schema as create response).

### GET /api/v1/geofences/{id} — Get Geofence Detail

**Response (200 OK):** Single geofence object.

### PUT /api/v1/geofences/{id} — Update Geofence

**Request:** Same schema as POST (all fields). Increments `version` for collar sync.

**Response (200 OK):** Updated geofence object.

### DELETE /api/v1/geofences/{id} — Delete Geofence

**Response (204 No Content)**

Increments the fence set version so collars remove the fence on next sync.

### GET /api/v1/geofences/sync — Collar Fence Sync Endpoint

Called by the collar during WiFi upload to get the latest fence set.

**Query Parameters:**
| Param | Type | Required | Description |
|-------|------|----------|-------------|
| collarId | string | yes | The collar's hex ID |
| sinceVersion | int | no | Only return if version > this |

**Response (200 OK):** Full fence set (see collar-geofence-engine.md sync format).

**Response (304 Not Modified):** If `sinceVersion` matches current version.

### GET /api/v1/geofences/events — Geofence Event History

**Query Parameters:**
| Param | Type | Default | Description |
|-------|------|---------|-------------|
| collarId | int | null | Filter by collar |
| fenceId | int | null | Filter by fence |
| eventType | string | null | "entered", "exited", "breach" |
| from | datetime | 24 hours ago | Start time |
| to | datetime | now | End time |
| page | int | 1 | Page number |
| pageSize | int | 50 | Items per page |

**Response (200 OK):**
```json
{
    "items": [
        {
            "id": 42,
            "collarId": 1,
            "animalName": "Luna",
            "fenceId": 2,
            "fenceName": "Pool",
            "eventType": "breach",
            "latitude": 33.44842,
            "longitude": -112.07395,
            "timestamp": "2026-02-28T14:30:00Z",
            "durationSeconds": 45,
            "resolved": true,
            "resolvedAt": "2026-02-28T14:30:45Z"
        }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 50
}
```

### POST /api/v1/geofences/events — Report Geofence Events (Collar → API)

Called by the collar to report breach events.

**Request:**
```json
{
    "apiKey": "door-or-collar-api-key",
    "collarId": "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6",
    "events": [
        {
            "fenceId": 2,
            "type": "breach",
            "lat": 33.44842,
            "lng": -112.07395,
            "ts": 1740754800
        },
        {
            "fenceId": 2,
            "type": "exited",
            "lat": 33.44843,
            "lng": -112.07396,
            "ts": 1740754845
        }
    ]
}
```

**Response (201 Created):**
```json
{
    "accepted": 2,
    "notificationsTriggered": 1
}
```

---

## 4. Satellite Map Tile Proxy

The SPA needs satellite imagery for the geofence editor and live map. To avoid CORS issues and provide a consistent experience, the API proxies public tile servers.

### GET /api/v1/maps/tile/{provider}/{z}/{x}/{y}

**Path Parameters:**
| Param | Type | Description |
|-------|------|-------------|
| provider | string | "satellite", "street", "terrain" |
| z | int | Zoom level (0-20) |
| x | int | Tile X coordinate |
| y | int | Tile Y coordinate |

**Response (200 OK):** PNG image (256x256 pixels)

**Headers:**
```
Content-Type: image/png
Cache-Control: public, max-age=86400
```

**Provider mapping:**
| Provider | Upstream | Attribution |
|----------|----------|-------------|
| satellite | Esri World Imagery | Esri, Maxar, Earthstar |
| street | OpenStreetMap | OpenStreetMap contributors |
| terrain | Stamen Terrain | Stamen Design, ODbL |

**Notes:**
- The API caches tiles in a local directory (`tiles/{provider}/{z}/{x}/{y}.png`) to reduce upstream requests.
- Tiles older than 30 days are re-fetched.
- The API adds proper attribution headers.

---

## 5. Enhanced Door Access Request

The existing `POST /api/v1/doors/access-request` is extended with optional collar fields.

### Updated AccessRequestDto

```
Field            Type       Required  Description
─────────────────────────────────────────────────────────
image            file       yes       Camera capture (existing)
apiKey           string     yes       Door API key (existing)
side             string     yes       "inside" or "outside" (existing)
collarId         string     no        Collar hex ID (if NFC detected)
nonce            string     no        Base64 challenge nonce
hmacResponse     string     no        Base64 HMAC from collar
collarBattery    int        no        Battery percentage from collar
```

### Updated AccessResponseDto

```json
{
    "allowed": true,
    "animalId": 5,
    "animalName": "Luna",
    "confidenceScore": 0.92,
    "reason": "Matched by camera and collar",
    "direction": "Entering",
    "identificationMethod": "both",
    "cameraConfidence": 0.77,
    "collarConfidence": 0.95,
    "collarBattery": 78
}
```

### Fused Confidence Calculation

```csharp
public float CalculateFusedConfidence(float? cameraConf, float? collarConf, bool collarVerified)
{
    // Camera only
    if (!collarVerified || collarConf == null)
        return cameraConf ?? 0f;

    // Collar only (no camera match, but collar verified)
    if (cameraConf == null || cameraConf < 0.4f)
        return collarConf.Value;  // 0.95 for verified collar

    // Both present — check agreement
    // (camera and collar must identify the same animal)
    if (cameraAnimalId == collarAnimalId)
    {
        // Agreement: boost camera confidence
        return Math.Min(cameraConf.Value + 0.15f, 1.0f);
    }
    else
    {
        // Disagreement: suspicious — return lower of the two
        return Math.Min(cameraConf.Value, collarConf.Value) * 0.5f;
    }
}
```

---

## 6. Notification Integration

New notification events for the collar system, added to the existing `NotificationPreferences` model:

| Event | Email | SMS | Push | Description |
|-------|-------|-----|------|-------------|
| GeofenceBreach | ✓ | ✓ | ✓ | Dog entered deny zone or left allow zone |
| CollarBatteryLow | ✓ | ✓ | — | Battery below 15% |
| CollarBatteryCritical | ✓ | ✓ | ✓ | Battery below 5% |
| CollarOffline | ✓ | — | — | No check-in for > 1 hour |
| CollarDisconnected | — | — | ✓ | BLE connection lost (near door) |
| SustainedBreach | ✓ | ✓ | ✓ | Breach lasting > 5 minutes |

These map to new boolean fields on `NotificationPreferences`:
```csharp
public bool GeofenceBreachEnabled { get; set; } = true;
public bool CollarBatteryLowEnabled { get; set; } = true;
public bool CollarBatteryCriticalEnabled { get; set; } = true;
public bool CollarOfflineEnabled { get; set; } = true;
public bool CollarDisconnectedEnabled { get; set; } = true;
public bool SustainedBreachEnabled { get; set; } = true;
```

---

## 7. Database Schema Changes

### New Tables

```sql
-- Collar devices
CREATE TABLE collar_devices (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id),
    animal_id INTEGER NOT NULL REFERENCES animals(id),
    collar_id VARCHAR(32) NOT NULL UNIQUE,
    shared_secret_hash VARCHAR(72) NOT NULL,  -- BCrypt
    name VARCHAR(100),
    firmware_version VARCHAR(20),
    battery_level REAL,
    last_seen_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_collar_devices_user_id ON collar_devices(user_id);
CREATE INDEX idx_collar_devices_collar_id ON collar_devices(collar_id);

-- GPS location points (high-volume, time-series optimized)
CREATE TABLE location_points (
    id BIGSERIAL PRIMARY KEY,
    collar_device_id INTEGER NOT NULL REFERENCES collar_devices(id),
    latitude DOUBLE PRECISION NOT NULL,
    longitude DOUBLE PRECISION NOT NULL,
    altitude REAL,
    accuracy REAL,
    speed REAL,
    heading REAL,
    satellites SMALLINT,
    battery_voltage REAL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX idx_location_points_device_time
    ON location_points(collar_device_id, timestamp DESC);

-- Consider TimescaleDB extension for automatic partitioning:
-- SELECT create_hypertable('location_points', 'timestamp');

-- Geofences
CREATE TABLE geofences (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id),
    name VARCHAR(100) NOT NULL,
    type VARCHAR(20) NOT NULL,      -- 'polygon', 'circle', 'corridor'
    action VARCHAR(10) NOT NULL,     -- 'allow', 'deny'
    boundary_json JSONB NOT NULL,    -- GeoJSON geometry
    buzzer_pattern VARCHAR(20) NOT NULL DEFAULT 'short',
    is_enabled BOOLEAN NOT NULL DEFAULT true,
    version INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_geofences_user_id ON geofences(user_id);

-- Many-to-many: which collars each fence applies to
CREATE TABLE geofence_collar_devices (
    geofence_id INTEGER NOT NULL REFERENCES geofences(id) ON DELETE CASCADE,
    collar_device_id INTEGER NOT NULL REFERENCES collar_devices(id) ON DELETE CASCADE,
    PRIMARY KEY (geofence_id, collar_device_id)
);

-- Geofence events (breaches, entries, exits)
CREATE TABLE geofence_events (
    id BIGSERIAL PRIMARY KEY,
    collar_device_id INTEGER NOT NULL REFERENCES collar_devices(id),
    geofence_id INTEGER NOT NULL REFERENCES geofences(id),
    event_type VARCHAR(20) NOT NULL,  -- 'entered', 'exited', 'breach'
    latitude DOUBLE PRECISION NOT NULL,
    longitude DOUBLE PRECISION NOT NULL,
    duration_seconds INTEGER,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX idx_geofence_events_device_time
    ON geofence_events(collar_device_id, timestamp DESC);
CREATE INDEX idx_geofence_events_fence_time
    ON geofence_events(geofence_id, timestamp DESC);

-- Daily movement summaries (for retention: replaces raw points after 30 days)
CREATE TABLE movement_summaries (
    id SERIAL PRIMARY KEY,
    collar_device_id INTEGER NOT NULL REFERENCES collar_devices(id),
    date DATE NOT NULL,
    distance_m REAL NOT NULL,
    active_minutes INTEGER NOT NULL,
    steps INTEGER,
    max_speed_ms REAL,
    breach_count INTEGER NOT NULL DEFAULT 0,
    first_active_at TIMESTAMP WITH TIME ZONE,
    last_active_at TIMESTAMP WITH TIME ZONE,
    UNIQUE (collar_device_id, date)
);
```

### Existing Table Modifications

```sql
-- Add collar reference to door_events (nullable, for NFC-identified events)
ALTER TABLE door_events ADD COLUMN collar_device_id INTEGER REFERENCES collar_devices(id);
ALTER TABLE door_events ADD COLUMN identification_method VARCHAR(20);  -- 'camera', 'collar', 'both'
ALTER TABLE door_events ADD COLUMN camera_confidence REAL;
ALTER TABLE door_events ADD COLUMN collar_confidence REAL;

-- Add collar notification prefs to notification_preferences
ALTER TABLE notification_preferences ADD COLUMN geofence_breach_enabled BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE notification_preferences ADD COLUMN collar_battery_low_enabled BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE notification_preferences ADD COLUMN collar_battery_critical_enabled BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE notification_preferences ADD COLUMN collar_offline_enabled BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE notification_preferences ADD COLUMN collar_disconnected_enabled BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE notification_preferences ADD COLUMN sustained_breach_enabled BOOLEAN NOT NULL DEFAULT true;
```

---

## 8. Background Services

### LocationRetentionService

Runs daily at 2:00 AM UTC. Applies the retention policy:

```csharp
public class LocationRetentionService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddHours(2); // 2 AM tomorrow
            await Task.Delay(nextRun - now, stoppingToken);

            // 1. Downsample 1-7 day old points to 1/10s
            await DownsampleRange(
                DateTime.UtcNow.AddDays(-7),
                DateTime.UtcNow.AddDays(-1),
                TimeSpan.FromSeconds(10));

            // 2. Downsample 7-30 day old points to 1/min
            await DownsampleRange(
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow.AddDays(-7),
                TimeSpan.FromMinutes(1));

            // 3. Generate daily summaries for 30+ day old data
            await GenerateSummaries(DateTime.UtcNow.AddDays(-30));

            // 4. Delete raw points older than 30 days
            await DeleteOldPoints(DateTime.UtcNow.AddDays(-30));
        }
    }
}
```

### CollarHealthMonitorService

Runs every 5 minutes. Checks for collars that haven't reported in:

```csharp
public class CollarHealthMonitorService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            var offlineThreshold = DateTime.UtcNow.AddHours(-1);
            var offlineCollars = await _db.CollarDevices
                .Where(c => c.IsActive && c.LastSeenAt < offlineThreshold)
                .Include(c => c.User)
                .Include(c => c.Animal)
                .ToListAsync();

            foreach (var collar in offlineCollars)
            {
                await _notificationService.SendCollarOfflineAlert(collar);
            }

            var lowBatteryCollars = await _db.CollarDevices
                .Where(c => c.IsActive && c.BatteryLevel < 15)
                .Include(c => c.User)
                .Include(c => c.Animal)
                .ToListAsync();

            foreach (var collar in lowBatteryCollars)
            {
                if (collar.BatteryLevel < 5)
                    await _notificationService.SendCollarBatteryCriticalAlert(collar);
                else
                    await _notificationService.SendCollarBatteryLowAlert(collar);
            }
        }
    }
}
```

---

## 9. Rate Limiting

| Endpoint Pattern | Limit | Window | Notes |
|------------------|-------|--------|-------|
| POST /collars/{id}/locations | 60/min | Per collar | ~1 upload/sec max |
| POST /geofences/events | 30/min | Per collar | Burst during breaches |
| GET /collars/{id}/locations | 10/min | Per user | Heavy query |
| GET /maps/tile/* | 100/min | Per user | Tile loading bursts |
| POST /collars/{id}/verify | 30/min | Per door | NFC verifications |
| Other collar endpoints | 30/min | Per user | Standard CRUD |
