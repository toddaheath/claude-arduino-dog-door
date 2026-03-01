# Collar Mobile Companion App (Future)

## Overview

While the collar system works entirely through the web SPA and direct collar-to-door communication, a native mobile app adds value for real-time push notifications, BLE collar provisioning, and on-the-go monitoring.

This document outlines the mobile app concept as a future phase. The core collar system does NOT depend on this app.

---

## 1. Platform & Technology

| Approach | Pros | Cons |
|----------|------|------|
| React Native | Share code with web SPA, one codebase | BLE support via library (react-native-ble-plx) |
| Flutter | Good BLE support, single codebase | Different language (Dart), can't share web code |
| Native (Swift/Kotlin) | Best BLE support, best UX | Two codebases to maintain |

**Recommended: React Native** — maximizes code reuse with the existing React web SPA (shared types, API client, business logic). BLE is well-supported via `react-native-ble-plx`.

---

## 2. Core Features

### 2.1 Collar Provisioning via BLE
- Scan for nearby collar devices (BLE advertisements)
- Connect to collar and write WiFi credentials + API key
- Transfer shared secret and collar ID during pairing
- Configure collar settings (GPS rate, upload interval, buzzer volume)
- No need for the pairing QR code flow when using the app (direct BLE)

### 2.2 Real-Time Location
- View dog's live position on a map (satellite tiles)
- Breadcrumb trail showing recent movement
- Geofence boundaries overlaid
- Map centered on dog's current position

### 2.3 Push Notifications
- Geofence breach alerts (immediate push notification)
- Low battery warnings
- Collar offline alerts
- Door access events (dog entered/exited through door)
- Uses Firebase Cloud Messaging (FCM) for Android, APNs for iOS

### 2.4 Find My Dog
- Trigger collar buzzer remotely
- Show dog's last known GPS position
- Compass pointing toward dog's location
- Distance estimate (BLE RSSI when close, GPS when far)

### 2.5 Activity Dashboard
- Today's stats: distance, steps, active time
- Weekly/monthly trends
- Breed-appropriate activity goals

---

## 3. BLE Provisioning Flow (App-Based)

```
Owner (App)                              Collar (BLE)
    │                                        │
    │── Scan for BLE devices                 │
    │   Filter: name starts with "SDD-Collar"│
    │                                        │
    │◀── Advertisement: "SDD-Collar-a1b2c3d4"│
    │    RSSI: -35 dBm (very close)         │
    │                                        │
    │── BLE Connect ────────────────────────▶│
    │                                        │── Pairing (6-digit passkey)
    │◀── Passkey request ────────────────────│
    │── Enter passkey: 123456 ──────────────▶│
    │                                        │── Bond established
    │                                        │
    │── Write Config characteristic: ───────▶│
    │   { "ssid": "HomeWifi",               │
    │     "password": "...",                 │
    │     "apiKey": "...",                   │
    │     "collarId": "a1b2...",            │
    │     "secret": "7f8e..." }             │
    │                                        │── Store in NVS
    │                                        │── Connect to WiFi
    │                                        │── Verify with API
    │◀── Config ACK ─────────────────────────│
    │   { "status": "ok",                   │
    │     "wifi": true,                     │
    │     "api": true }                     │
    │                                        │
    │── Display "Collar paired!" ────────────│
```

---

## 4. Notification Architecture

```
Collar → WiFi → API → Notification Service → Push Provider → Phone App

Breach event flow:
1. Collar detects geofence breach
2. Collar buzzes locally (immediate, no network needed)
3. Collar WiFi uploads breach event to API (within 30s)
4. API NotificationService checks user preferences
5. If push enabled: send to FCM/APNs
6. If SMS enabled: send via Twilio
7. If email enabled: send via SendGrid
8. Phone receives push notification within ~2-5 seconds of API receiving event
```

### 4.1 Push Notification Payloads

**Geofence Breach:**
```json
{
    "title": "Luna left the backyard!",
    "body": "Breached 'Backyard' fence at 3:45 PM. Tap to view location.",
    "data": {
        "type": "geofence_breach",
        "collarId": 1,
        "fenceId": 1,
        "lat": 33.44860,
        "lng": -112.07395
    },
    "priority": "high",
    "sound": "breach_alert.wav"
}
```

**Low Battery:**
```json
{
    "title": "Luna's collar battery is low",
    "body": "Battery at 12%. Estimated 8 hours remaining.",
    "data": {
        "type": "collar_battery_low",
        "collarId": 1,
        "batteryPct": 12
    },
    "priority": "normal"
}
```

**Door Access:**
```json
{
    "title": "Luna came inside",
    "body": "Entry granted at 4:15 PM (confidence: 0.92)",
    "data": {
        "type": "door_access",
        "animalId": 5,
        "direction": "entering",
        "method": "both"
    },
    "priority": "normal"
}
```

---

## 5. App Screens (Concept)

### 5.1 Home Screen
```
┌──────────────────────────┐
│  Smart Dog Door      ⚙️  │
├──────────────────────────┤
│                          │
│  ┌────────────────────┐  │
│  │  [Mini map with    │  │
│  │   dog locations]   │  │
│  │      ● Luna        │  │
│  │      ○ Max         │  │
│  └────────────────────┘  │
│                          │
│  ● Luna    Backyard      │
│    Walking  🔋 78%       │
│    [Track] [Buzz]        │
│                          │
│  ○ Max     Indoor        │
│    Idle     🔋 23%⚡     │
│    [Track] [Buzz]        │
│                          │
│  Today                   │
│  Door uses: 6            │
│  Breaches: 0             │
│  Distance: 2.4 km        │
│                          │
├──────────────────────────┤
│  🏠 Home  🗺 Map  🔔 Log │
└──────────────────────────┘
```

### 5.2 Map Screen
```
┌──────────────────────────┐
│  ← Map        [🛰 / 🗺] │
├──────────────────────────┤
│                          │
│  ╔════════════════════╗  │
│  ║  Satellite view    ║  │
│  ║  with dog markers  ║  │
│  ║  and geofences     ║  │
│  ║                    ║  │
│  ║  ● Luna ╌╌╌ trail ║  │
│  ║  ◉ Pool (no-go)   ║  │
│  ║                    ║  │
│  ╚════════════════════╝  │
│                          │
│  [Center on Luna]        │
│  [Center on Max]         │
│  [Show all fences]       │
│                          │
├──────────────────────────┤
│  🏠 Home  🗺 Map  🔔 Log │
└──────────────────────────┘
```

---

## 6. API Changes for Mobile Push

The existing API needs a device registration endpoint for push tokens:

```
POST /api/v1/users/me/devices
{
    "platform": "ios",           // "ios" or "android"
    "pushToken": "fcm-or-apns-token-here",
    "deviceName": "Todd's iPhone"
}

DELETE /api/v1/users/me/devices/{id}
```

The `NotificationService` checks for registered devices and sends push notifications via FCM/APNs in addition to existing SMS/email channels.

---

## 7. Implementation Priority

The mobile app is **Phase 6** (after the core collar system is complete):

1. Start with push notifications (highest value — breach alerts)
2. Add BLE provisioning (better UX than QR code)
3. Add live map (parity with web SPA)
4. Add find-my-dog (unique to mobile — uses phone GPS for compass)
5. Add activity dashboard
