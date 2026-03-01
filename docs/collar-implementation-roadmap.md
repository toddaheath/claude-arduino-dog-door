# Collar System — Implementation Roadmap

## Overview

Detailed task breakdown for each implementation phase, with dependencies and effort estimates.

---

## Phase 1: Collar Hardware + Basic Firmware

**Goal:** A collar device that tracks GPS, advertises via BLE, and reports battery status.

### Hardware Tasks
- [ ] Design KiCad schematic (ESP32-S3 + GPS + IMU + PN532 + power)
- [ ] Route 2-layer PCB layout (45x30mm)
- [ ] Generate Gerber files and order PCB fabrication
- [ ] Order components (BOM from collar-device-design.md)
- [ ] Design TPU enclosure in FreeCAD/Fusion360
- [ ] 3D print enclosure prototype
- [ ] Assemble first prototype (hand solder)
- [ ] Verify power circuits (voltage regulator, charge IC, voltage divider)

### Firmware Tasks
- [ ] Create `firmware/collar/` PlatformIO project
- [ ] Implement `config.h` with all pin assignments and constants
- [ ] Implement `power_manager.cpp` (ADC reading, LiPo curve, EMA smoothing)
- [ ] Implement `imu_manager.cpp` (LSM6DSO init, motion detection, wake interrupt config)
- [ ] Implement `gps_tracker.cpp` (u-blox UBX protocol, fix parsing, power gating)
- [ ] Implement `ble_collar.cpp` (NimBLE service, 6 characteristics, command handler)
- [ ] Implement `storage.cpp` (NVS for config, SPIFFS ring buffer for GPS points)
- [ ] Implement `main.cpp` state machine (deep sleep ↔ wake/classify ↔ GPS tracking)
- [ ] Implement `buzzer.cpp` (PWM tone generation, pattern playback)
- [ ] Write PlatformIO native unit tests for power manager
- [ ] Write PlatformIO native unit tests for GPS fix parsing
- [ ] Verify BLE advertising from phone (nRF Connect app)
- [ ] Verify GPS fix acquisition (outdoor test)
- [ ] Verify deep sleep current (bench multimeter: < 30uA target)
- [ ] Verify IMU wake-from-sleep (shake test)

### Dependencies
- None (standalone hardware/firmware work)

---

## Phase 2: NFC Door Integration

**Goal:** Collar authenticates with door via NFC challenge-response; door firmware sends collar data to API.

### Door Firmware Tasks
- [ ] Add PN532 module to door hardware (I2C wiring to ESP32-CAM)
- [ ] Implement `nfc_reader.cpp` (PN532 init, ISO 14443-4 tag detection)
- [ ] Implement NFC challenge generation (random nonce via mbedtls)
- [ ] Implement `collar_scanner.cpp` (BLE scan for collar advertisements)
- [ ] Update `main.cpp` access pipeline (parallel BLE scan + camera capture)
- [ ] Update `api_client.cpp` to include collar fields in access request
- [ ] Update `config.h` with NFC pins and collar BLE UUID
- [ ] Bench test: NFC handshake between two PN532 modules

### Collar Firmware Tasks
- [ ] Implement `nfc_manager.cpp` (PN532 tag emulation mode)
- [ ] Implement HMAC-SHA256 computation (mbedtls)
- [ ] Implement NFC handshake protocol (COLLAR_ANNOUNCE, AUTH_RESPONSE)
- [ ] Implement timestamp validation (30-second replay window)
- [ ] Implement constant-time HMAC comparison
- [ ] Update state machine: BLE door proximity → NFC_READY state
- [ ] Write unit tests for HMAC (RFC 4231 test vectors)
- [ ] Write unit tests for TLV message encoding/decoding
- [ ] Write unit tests for timestamp validation
- [ ] Integration test: full NFC handshake on bench

### API Tasks
- [ ] Create `CollarDevice` EF Core model
- [ ] Create EF migration `AddCollarDevices`
- [ ] Implement `CollarService` (CRUD, secret management)
- [ ] Implement `CollarController` (register, list, get, update, delete)
- [ ] Implement `POST /collars/{id}/verify` (HMAC verification endpoint)
- [ ] Update `DoorService.ProcessAccessRequestAsync` for collar fields
- [ ] Update `AccessRequestDto` and `AccessResponseDto` with collar fields
- [ ] Implement fused confidence calculation
- [ ] Write integration tests: collar CRUD (5 tests)
- [ ] Write integration tests: NFC verify (5 tests)
- [ ] Write integration tests: fused access request (5 tests)

### Dependencies
- Phase 1 (collar hardware + BLE must work before NFC testing)

---

## Phase 3: API + Location Tracking

**Goal:** Collar uploads GPS data to API; SPA shows live map with dog location.

### Collar Firmware Tasks
- [ ] Implement `wifi_uploader.cpp` (WiFi connect, batch JSON upload, disconnect)
- [ ] Implement location buffering in SPIFFS (ring buffer, max 500 points)
- [ ] Update state machine: periodic WiFi upload state
- [ ] Implement adaptive GPS rate (1Hz moving, 0.1Hz stationary, IMU-informed)
- [ ] Test WiFi upload reliability (100 points, verify all stored in DB)

### API Tasks
- [ ] Create `LocationPoint` EF Core model
- [ ] Create `MovementSummary` EF Core model
- [ ] Create EF migration `AddLocationTracking`
- [ ] Implement `LocationService` (batch insert, query with downsampling, current location)
- [ ] Implement `LocationController` (batch upload, query, current, stats)
- [ ] Implement Ramer-Douglas-Peucker downsampling algorithm
- [ ] Implement `LocationRetentionService` (background job: downsample + purge)
- [ ] Implement heatmap generation (grid-based time-density)
- [ ] Implement `MapTileService` (proxy + disk cache for satellite tiles)
- [ ] Implement `MapController` (tile proxy endpoint)
- [ ] Configure rate limiting for collar-upload and tile-proxy policies
- [ ] Write integration tests: location upload (4 tests)
- [ ] Write integration tests: location query + downsampling (3 tests)
- [ ] Write integration tests: movement stats + heatmap (2 tests)

### SPA Tasks
- [ ] Install Leaflet + react-leaflet + leaflet-heat NPM packages
- [ ] Create `types/collar.ts` (CollarDevice, LocationPoint, etc.)
- [ ] Create `api/collarApi.ts` (collar CRUD)
- [ ] Create `api/locationApi.ts` (location upload/query)
- [ ] Create `api/mapApi.ts` (tile proxy URL builder)
- [ ] Create `components/map/SatelliteMap.tsx` (Leaflet wrapper with tile layers)
- [ ] Create `components/map/DogMarker.tsx` (animated location marker)
- [ ] Create `components/map/TrackOverlay.tsx` (GPS track with gradient)
- [ ] Create `components/collar/CollarCard.tsx` (summary card)
- [ ] Create `components/collar/BatteryIndicator.tsx`
- [ ] Create `pages/CollarList.tsx` (`/collars` route)
- [ ] Create `pages/MapView.tsx` (`/map` route with live location)
- [ ] Update `App.tsx` routing (add /collars and /map routes)
- [ ] Update navigation bar (add Collars and Map links)
- [ ] Implement real-time location polling (5-second interval)
- [ ] Update Dashboard with collar status section
- [ ] Add collar pairing modal (CollarPairing.tsx)

### Dependencies
- Phase 2 (collar must authenticate to upload data)

---

## Phase 4: Virtual Geofencing

**Goal:** Owner draws virtual fences on satellite map; collar evaluates fences locally; breach alerts.

### Collar Firmware Tasks
- [ ] Implement `geofence.cpp` (evaluation engine: polygon, circle, corridor)
- [ ] Implement equirectangular projection (GPS → local Cartesian)
- [ ] Implement ray-casting algorithm (point-in-polygon)
- [ ] Implement haversine distance (point-in-circle)
- [ ] Implement perpendicular distance to segment (point-in-corridor)
- [ ] Implement evaluation engine (deny-first, then allow-check)
- [ ] Implement hysteresis buffer (3m, prevents GPS flapping)
- [ ] Implement GPS quality gating (HDOP, satellite count, fix age)
- [ ] Implement breach response (escalating buzzer patterns)
- [ ] Implement proximity warnings (5m approach warning)
- [ ] Implement fence sync via WiFi (GET /geofences/sync)
- [ ] Implement fence sync via BLE (Command characteristic)
- [ ] Implement NVS storage for fence data
- [ ] Write unit tests: point-in-polygon (6 tests)
- [ ] Write unit tests: point-in-circle (4 tests)
- [ ] Write unit tests: point-in-corridor (4 tests)
- [ ] Write unit tests: evaluation engine (6 tests)
- [ ] Write unit tests: hysteresis (4 tests)
- [ ] Write unit tests: GPS quality gating (4 tests)
- [ ] Write unit tests: distance to boundary (4 tests)

### API Tasks
- [ ] Create `Geofence` EF Core model
- [ ] Create `GeofenceEvent` EF Core model
- [ ] Create `GeofenceCollarDevices` join table
- [ ] Create EF migration `AddGeofencing`
- [ ] Implement `GeofenceService` (CRUD, area/perimeter calculation, version management)
- [ ] Implement `GeofenceController` (CRUD, sync endpoint, events endpoint)
- [ ] Implement fence event ingestion (POST /geofences/events from collar)
- [ ] Implement fence sync endpoint (GET /geofences/sync with version check)
- [ ] Add geofence notification events to NotificationPreferences
- [ ] Create EF migration `AddGeofenceNotificationPrefs`
- [ ] Implement geofence breach notifications (SMS + email)
- [ ] Write integration tests: geofence CRUD (5 tests)
- [ ] Write integration tests: fence sync (3 tests)
- [ ] Write integration tests: fence events (3 tests)

### SPA Tasks
- [ ] Install leaflet-draw + react-leaflet-draw NPM packages
- [ ] Create `api/geofenceApi.ts` (geofence CRUD + events)
- [ ] Create `components/map/GeofenceLayer.tsx` (render fences on map)
- [ ] Create `components/map/DrawControls.tsx` (polygon/circle/corridor tools)
- [ ] Create `components/geofence/FenceList.tsx` (sidebar list)
- [ ] Create `components/geofence/FenceProperties.tsx` (edit form)
- [ ] Create `pages/GeofenceEditor.tsx` (`/map/geofences` route)
- [ ] Create `pages/GeofenceEvents.tsx` (`/geofence-events` route)
- [ ] Implement fence color coding (green=allow, red=deny, blue=corridor)
- [ ] Implement vertex dragging for polygon editing
- [ ] Implement circle resize by dragging edge
- [ ] Implement corridor width adjustment
- [ ] Implement per-collar fence assignment UI
- [ ] Update Notifications page with geofence alert toggles

### Dependencies
- Phase 3 (map + location must work before geofencing)

---

## Phase 5: Polish + Analytics

**Goal:** Movement history playback, heat maps, OTA updates, find-my-dog, daily stats.

### Collar Firmware Tasks
- [ ] Implement OTA update check during WiFi upload
- [ ] Implement OTA download with SHA256 verification
- [ ] Implement rollback protection (Secure Boot v2 app validation)
- [ ] Implement self-test suite (IMU, GPS, BLE, NFC, power)
- [ ] Implement daily stats reset (midnight rollover using GPS time)
- [ ] Implement find-my-dog buzzer command (30-second continuous tone)

### API Tasks
- [ ] Implement firmware management endpoints (upload, check, download, confirm)
- [ ] Create `CollarFirmware` and `CollarFirmwareDeployment` models
- [ ] Implement `CollarHealthMonitorService` (background: offline + battery alerts)
- [ ] Implement GPX export endpoint (`GET /collars/{id}/locations?format=gpx`)
- [ ] Implement daily movement summary generation
- [ ] Implement collar offline detection + notification
- [ ] Implement collar battery low + critical notifications

### SPA Tasks
- [ ] Create `pages/CollarDetail.tsx` (`/collars/:id` with mini map + stats)
- [ ] Create `pages/MovementHistory.tsx` (`/collars/:id/history`)
- [ ] Create `components/map/PlaybackControl.tsx` (timeline slider + play/pause)
- [ ] Create `components/map/HeatmapLayer.tsx` (time-density overlay)
- [ ] Create `components/collar/ActivityChart.tsx` (daily sparkline)
- [ ] Implement animated track playback on map
- [ ] Implement GPX export button
- [ ] Implement find-my-dog button (triggers buzzer via API → BLE)
- [ ] Implement collar settings page (GPS rate, upload interval, buzzer volume)

### Helm/Deployment Tasks
- [ ] Add tile cache PVC to Helm chart
- [ ] Add collar config to values.yaml
- [ ] Add collar background services to deployment
- [ ] Update Docker Compose with tile cache volume
- [ ] Add collar firmware CI workflow
- [ ] Update CI to run collar-category tests

### Dependencies
- Phase 4 (all core features must work before polish)

---

## Phase 6: Mobile Companion App (Future)

**Goal:** Native mobile app for push notifications, BLE provisioning, on-the-go monitoring.

### Tasks
- [ ] Set up React Native project with BLE and map libraries
- [ ] Implement push notification registration (FCM + APNs)
- [ ] Implement BLE collar provisioning flow
- [ ] Implement live map screen (satellite tiles + dog markers)
- [ ] Implement find-my-dog with compass (phone GPS → collar GPS bearing)
- [ ] Implement activity dashboard (daily/weekly stats)
- [ ] API: Add device registration endpoint for push tokens
- [ ] API: Implement push notification delivery (FCM/APNs)

### Dependencies
- Phase 5 (all API endpoints must be stable before mobile app)

---

## Effort Estimates (By Phase)

| Phase | Firmware | API | SPA | Hardware | Total |
|-------|----------|-----|-----|----------|-------|
| 1: Hardware + Basic FW | 40h | — | — | 20h | 60h |
| 2: NFC Integration | 30h | 20h | — | 5h | 55h |
| 3: Location Tracking | 15h | 25h | 30h | — | 70h |
| 4: Geofencing | 35h | 20h | 25h | — | 80h |
| 5: Polish + Analytics | 15h | 15h | 25h | 5h | 60h |
| 6: Mobile App | — | 10h | 40h | — | 50h |
| **Total** | **135h** | **90h** | **120h** | **30h** | **375h** |

---

## Milestone Checkpoints

| Milestone | Phase | Verification |
|-----------|-------|-------------|
| Collar boots and advertises via BLE | 1 | nRF Connect sees "SDD-Collar-*" |
| GPS fix acquired outdoors | 1 | Serial output shows lat/lng with < 5m accuracy |
| Deep sleep < 30uA measured | 1 | Bench multimeter reading |
| NFC handshake < 50ms | 2 | Serial output shows AUTH_OK timing |
| Fused access request works | 2 | API logs show identificationMethod="both" |
| 100 GPS points uploaded via WiFi | 3 | Database has 100 rows in location_points |
| Live dog position on SPA map | 3 | Map shows updating marker |
| Geofence breach buzzer sounds | 4 | Move collar outside fence, hear beep within 2s |
| Geofence breach event in SPA | 4 | Breach appears in /geofence-events table |
| OTA update completes | 5 | Collar reboots on new firmware, self-test passes |
| Heat map renders on history page | 5 | Color-coded density overlay on satellite map |
