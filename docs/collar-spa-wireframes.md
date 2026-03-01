# Collar SPA Wireframes & Component Design

## Overview

New React pages and components for the collar device system, integrated into the existing Smart Dog Door admin SPA.

---

## 1. Updated Navigation

```
┌─────────────────────────────────────────────────────────────────┐
│  🐕 Smart Dog Door                                    [Profile] │
├─────────────────────────────────────────────────────────────────┤
│  Dashboard │ Animals │ Access Log │ Map │ Collars │ Settings    │
└─────────────────────────────────────────────────────────────────┘
                                     ^^^    ^^^^^^^
                                     NEW      NEW
```

---

## 2. Collars Page (`/collars`)

### 2.1 Collar List View

```
┌─────────────────────────────────────────────────────────────────┐
│  Collars                                      [+ Pair New Collar]│
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  Luna's Collar                              ● Active     │    │
│  │  Animal: Luna  │  FW: 1.2.0  │  Last seen: 2 min ago   │    │
│  │                                                          │    │
│  │  ┌──────────┐  ┌──────────────┐  ┌──────────────────┐   │    │
│  │  │ 🔋 78%   │  │ 📍 Backyard  │  │ 1,523m today     │   │    │
│  │  │ ~4d left │  │ In bounds    │  │ 47 min active    │   │    │
│  │  └──────────┘  └──────────────┘  └──────────────────┘   │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  Max's Collar                               ○ Charging   │    │
│  │  Animal: Max   │  FW: 1.1.3  │  Last seen: 15 min ago  │    │
│  │                                                          │    │
│  │  ┌──────────┐  ┌──────────────┐  ┌──────────────────┐   │    │
│  │  │ 🔋 23%   │  │ 📍 Indoor    │  │ 892m today       │   │    │
│  │  │ charging │  │ No GPS fix   │  │ 22 min active    │   │    │
│  │  └──────────┘  └──────────────┘  └──────────────────┘   │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Pair New Collar Modal

```
┌─────────────────────────────────────────────────────────────────┐
│  Pair New Collar                                         [✕]    │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Step 1: Select Animal                                           │
│  ┌───────────────────────────────────────────────────┐           │
│  │  ▼ Select an animal...                            │           │
│  │    Luna                                           │           │
│  │    Max                                            │           │
│  └───────────────────────────────────────────────────┘           │
│                                                                  │
│  Collar Name:                                                    │
│  ┌───────────────────────────────────────────────────┐           │
│  │  Luna's Collar                                    │           │
│  └───────────────────────────────────────────────────┘           │
│                                                                  │
│                        [Generate Pairing Code]                   │
│                                                                  │
│  ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─          │
│                                                                  │
│  Step 2: Provision Collar                                        │
│                                                                  │
│  Scan this QR code with the collar's BLE provisioning app:       │
│                                                                  │
│  ┌─────────────────┐   Pairing Code: 847293                     │
│  │                  │                                             │
│  │   ██ █  ██ █    │   Collar ID:                                │
│  │   █ ██ █ ██     │   a1b2c3d4e5f6...                          │
│  │   ██ █  ██ █    │                                             │
│  │   █ ██ █ ██     │   Status: Waiting for collar...             │
│  │                  │                                             │
│  └─────────────────┘   ⟳ Expires in 9:42                        │
│                                                                  │
│  Or enter the pairing code manually on the collar's              │
│  Bluetooth setup screen.                                         │
│                                                                  │
│                               [Cancel]  [Done — Collar Paired]   │
└─────────────────────────────────────────────────────────────────┘
```

### 2.3 Collar Detail Page (`/collars/:id`)

```
┌─────────────────────────────────────────────────────────────────┐
│  ← Back to Collars                                               │
│                                                                  │
│  Luna's Collar                           ● Active    [Settings]  │
│  Linked to: Luna  │  FW: 1.2.0  │  Paired: Feb 15, 2026        │
│                                                                  │
├──────────┬───────────┬────────────┬──────────────────────────────┤
│ Battery  │ Location  │ Activity   │ Geofence                     │
│ 78%      │ Backyard  │ Walking    │ In bounds                    │
│ ~4d left │ 2.5m acc  │ 1.2 m/s   │ 15m from Pool               │
├──────────┴───────────┴────────────┴──────────────────────────────┤
│                                                                  │
│  ┌───────────────────────────────────────────────────────────┐   │
│  │                                                            │   │
│  │              [Mini satellite map]                          │   │
│  │              Showing current location                      │   │
│  │              + last 30 min trail                           │   │
│  │              + geofence boundaries                         │   │
│  │                         ● Luna                             │   │
│  │              ┌─ ─ ─ ─ ─ ─ ─ ─ ─ ┐                        │   │
│  │              │   Backyard (allow) │                        │   │
│  │              │    ◉ Pool (deny)   │                        │   │
│  │              └─ ─ ─ ─ ─ ─ ─ ─ ─ ┘                        │   │
│  │                                                            │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│  Today's Activity                                                │
│  ┌───────────────────────────────────────────────────────────┐   │
│  │  Distance: 1,523m  │  Steps: 3,204  │  Active: 47 min    │   │
│  │  ▁▂▃▅▇█▅▃▁▁▂▃▅▇▆▃▁▁▁▁▂▃▅                                │   │
│  │  6am    9am    12pm   3pm    6pm    9pm                   │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│  Recent Events                                                   │
│  ┌───────────────────────────────────────────────────────────┐   │
│  │  3:45 PM  NFC door access — Entry granted (both: 0.92)   │   │
│  │  2:30 PM  Approaching Pool boundary — Warning sent        │   │
│  │  1:15 PM  GPS tracking started (motion detected)          │   │
│  │  7:00 AM  Collar powered on (battery 82%)                 │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│  [View Full History]  [View on Map]  [Find My Dog 🔔]           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. Map Page (`/map`)

### 3.1 Live Map View

```
┌─────────────────────────────────────────────────────────────────┐
│  Map                    [Satellite ▼] [Geofences] [History]     │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌───────────────────────────────────────────────────────────┐   │
│  │                                                            │   │
│  │  ╔══════════════════════════════════════════════════════╗  │   │
│  │  ║                                                      ║  │   │
│  │  ║    Satellite imagery of property                     ║  │   │
│  │  ║                                                      ║  │   │
│  │  ║    ┌─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┐                 ║  │   │
│  │  ║    │     Backyard (green border)   │                 ║  │   │
│  │  ║    │                                │                 ║  │   │
│  │  ║    │  ● Luna (live)                 │  [House]       ║  │   │
│  │  ║    │  ╌╌╌ trail                     │                 ║  │   │
│  │  ║    │       ◉ Pool (red circle)      │                 ║  │   │
│  │  ║    │                                │                 ║  │   │
│  │  ║    │            ○ Max (last known)  │                 ║  │   │
│  │  ║    └─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┘                 ║  │   │
│  │  ║                                                      ║  │   │
│  │  ║  [+]                                   © Esri        ║  │   │
│  │  ║  [-]                                                 ║  │   │
│  │  ╚══════════════════════════════════════════════════════╝  │   │
│  │                                                            │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│  ┌─ Dog Status Panel ────────────────────────────────────────┐   │
│  │  ● Luna  │ Backyard │ Walking │ 🔋78% │ [Track] [Locate] │   │
│  │  ○ Max   │ Indoor   │ Idle    │ 🔋23% │ [Track] [Locate] │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Geofence Editor (`/map/geofences`)

```
┌─────────────────────────────────────────────────────────────────┐
│  Geofence Editor                    [Save All] [Discard Changes]│
├────────────────────┬────────────────────────────────────────────┤
│                    │                                             │
│  Fences            │  ╔═══════════════════════════════════════╗  │
│                    │  ║                                        ║  │
│  ┌──────────────┐  │  ║  Satellite imagery with editable      ║  │
│  │ ✓ Backyard   │  │  ║  fence overlays                       ║  │
│  │   Allow      │  │  ║                                        ║  │
│  │   Polygon    │  │  ║  ┌─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┐            ║  │
│  │   [Edit]     │  │  ║  │  ■─────────────■       │            ║  │
│  └──────────────┘  │  ║  │  │  Backyard    │       │            ║  │
│                    │  ║  │  │              │       │            ║  │
│  ┌──────────────┐  │  ║  │  │    ◉ Pool    │       │            ║  │
│  │ ✓ Pool       │  │  ║  │  │   (r=3m)    │       │            ║  │
│  │   Deny       │  │  ║  │  │              │       │            ║  │
│  │   Circle     │  │  ║  │  ■─────────────■       │            ║  │
│  │   [Edit]     │  │  ║  └─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┘            ║  │
│  └──────────────┘  │  ║                                        ║  │
│                    │  ║  Vertices are draggable.                ║  │
│  ┌──────────────┐  │  ║  Click + drag circle edge to resize.  ║  │
│  │ ✓ Side Path  │  │  ║                                        ║  │
│  │   Allow      │  │  ║  Drawing mode:                         ║  │
│  │   Corridor   │  │  ║  [Polygon] [Circle] [Corridor]        ║  │
│  │   [Edit]     │  │  ║  [Select] [Delete]                    ║  │
│  └──────────────┘  │  ║                                        ║  │
│                    │  ╚═══════════════════════════════════════╝  │
│  [+ Add Fence]     │                                             │
│                    │                                             │
├────────────────────┤  Fence Properties (when selected):         │
│  Applies to:       │  ┌──────────────────────────────────────┐  │
│  ☑ All collars     │  │  Name: [Pool                       ] │  │
│  ☐ Luna's Collar   │  │  Action: [Deny ▼]                   │  │
│  ☐ Max's Collar    │  │  Buzzer: [Continuous ▼]              │  │
│                    │  │  Enabled: [✓]                        │  │
│                    │  │  Radius: [3.0] meters                │  │
│                    │  │  Area: 28.3 m²                       │  │
│                    │  └──────────────────────────────────────┘  │
└────────────────────┴────────────────────────────────────────────┘
```

### 3.3 History Playback (`/collars/:id/history`)

```
┌─────────────────────────────────────────────────────────────────┐
│  ← Luna's Movement History                                      │
│                                                                  │
│  Date: [Feb 28, 2026 ▼]  Time: [All Day ▼]         [Export GPX]│
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ╔═══════════════════════════════════════════════════════════╗   │
│  ║                                                            ║   │
│  ║  Satellite map with track overlay                          ║   │
│  ║                                                            ║   │
│  ║  Start ●━━━━━━━━━━━━━━━━━━━━━━━━━━━━● End                ║   │
│  ║        (green)   track path     (red)                      ║   │
│  ║                                                            ║   │
│  ║  Color gradient: green (start) → yellow → red (end)        ║   │
│  ║  Width varies with speed (thicker = faster)                ║   │
│  ║                                                            ║   │
│  ║  ★ = breach events (red markers)                           ║   │
│  ║  ◆ = door access events (blue markers)                     ║   │
│  ║                                                            ║   │
│  ╚═══════════════════════════════════════════════════════════╝   │
│                                                                  │
│  Playback:  [|◀] [◀◀] [▶ Play] [▶▶] [▶|]   Speed: [1x ▼]      │
│  ━━━━━━━━━━━━━━━━━━━●━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━          │
│  7:15 AM                2:30 PM                      6:45 PM     │
│                                                                  │
│  ┌─ Day Summary ─────────────────────────────────────────────┐   │
│  │  Distance: 1,523m  │  Active: 47min  │  Steps: 3,204     │   │
│  │  Top speed: 3.2m/s │  Breaches: 0    │  Door uses: 4     │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│  ┌─ Heat Map Toggle ─────────────────────────────────────────┐   │
│  │  [Show Heat Map]  Time spent density overlay               │   │
│  │  Low ░░░▒▒▒▓▓▓███ High                                    │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Component Architecture

### 4.1 New Components

```
web/src/
├── pages/
│   ├── CollarList.tsx            # /collars - list of paired collars
│   ├── CollarDetail.tsx          # /collars/:id - collar dashboard
│   ├── CollarPairing.tsx         # Pairing modal/wizard
│   ├── MapView.tsx               # /map - live satellite map
│   ├── GeofenceEditor.tsx        # /map/geofences - draw/edit fences
│   ├── MovementHistory.tsx       # /collars/:id/history - track playback
│   └── GeofenceEvents.tsx        # /geofence-events - breach log
│
├── components/
│   ├── map/
│   │   ├── SatelliteMap.tsx      # Leaflet map wrapper with tile layers
│   │   ├── DogMarker.tsx         # Animated dog location marker
│   │   ├── TrackOverlay.tsx      # GPS track line with gradient
│   │   ├── GeofenceLayer.tsx     # Renders fence boundaries
│   │   ├── DrawControls.tsx      # Leaflet.draw polygon/circle tools
│   │   ├── HeatmapLayer.tsx      # Time-density heat map
│   │   └── PlaybackControl.tsx   # Timeline slider + play/pause
│   │
│   ├── collar/
│   │   ├── CollarCard.tsx        # Summary card for collar list
│   │   ├── BatteryIndicator.tsx  # Battery level + estimate
│   │   ├── ActivityChart.tsx     # Daily activity sparkline
│   │   ├── CollarStatus.tsx      # Active/charging/offline badge
│   │   └── PairingQRCode.tsx     # QR code display for pairing
│   │
│   └── geofence/
│       ├── FenceList.tsx         # Sidebar fence list
│       ├── FenceProperties.tsx   # Edit form for selected fence
│       └── FenceEventRow.tsx     # Single event in breach log
│
├── api/
│   ├── collarApi.ts              # Collar CRUD + verify endpoints
│   ├── locationApi.ts            # Location upload/query endpoints
│   ├── geofenceApi.ts            # Geofence CRUD + events endpoints
│   └── mapApi.ts                 # Tile proxy endpoint
│
└── types/
    └── collar.ts                 # CollarDevice, LocationPoint, Geofence, etc.
```

### 4.2 Type Definitions

```typescript
// types/collar.ts

export interface CollarDevice {
    id: number;
    collarId: string;
    animalId: number;
    animalName: string;
    name: string;
    firmwareVersion: string | null;
    batteryLevel: number | null;
    lastSeenAt: string | null;
    isActive: boolean;
    lastLocation: LocationPoint | null;
    createdAt: string;
}

export interface CollarDetail extends CollarDevice {
    stats: CollarStats;
}

export interface CollarStats {
    distanceTodayM: number;
    activeMinutesToday: number;
    stepsToday: number;
    breachesToday: number;
    avgDailyDistanceM: number;
    avgDailyActiveMinutes: number;
}

export interface LocationPoint {
    latitude: number;
    longitude: number;
    altitude?: number;
    accuracy?: number;
    speed?: number;
    heading?: number;
    satellites?: number;
    batteryVoltage?: number;
    timestamp: string;
}

export interface LocationTrack {
    collarId: number;
    animalName: string;
    from: string;
    to: string;
    pointCount: number;
    points: LocationPoint[];
}

export interface Geofence {
    id: number;
    name: string;
    type: 'polygon' | 'circle' | 'corridor';
    action: 'allow' | 'deny';
    buzzerPattern: 'silent' | 'short' | 'long' | 'continuous';
    isEnabled: boolean;
    boundary: GeoJSON.Geometry & { radius?: number; width?: number };
    collarIds: number[];
    areaM2?: number;
    perimeterM?: number;
    createdAt: string;
    updatedAt: string;
    version: number;
}

export interface GeofenceEvent {
    id: number;
    collarId: number;
    animalName: string;
    fenceId: number;
    fenceName: string;
    eventType: 'entered' | 'exited' | 'breach';
    latitude: number;
    longitude: number;
    timestamp: string;
    durationSeconds?: number;
    resolved: boolean;
    resolvedAt?: string;
}

export interface MovementSummary {
    date: string;
    distanceM: number;
    activeMinutes: number;
    steps: number | null;
    maxSpeedMs: number | null;
    breachCount: number;
    firstActiveAt: string | null;
    lastActiveAt: string | null;
}

export interface CollarPairingResult {
    id: number;
    collarId: string;
    sharedSecret: string;
    pairingCode: string;
    animalId: number;
    animalName: string;
    name: string;
}
```

### 4.3 API Client Functions

```typescript
// api/collarApi.ts
import client from './client';
import type { CollarDevice, CollarDetail, CollarPairingResult } from '../types/collar';

export const collarApi = {
    list: () =>
        client.get<CollarDevice[]>('/collars'),

    get: (id: number) =>
        client.get<CollarDetail>(`/collars/${id}`),

    pair: (animalId: number, name: string) =>
        client.post<CollarPairingResult>('/collars', { animalId, name }),

    update: (id: number, data: { name?: string; isActive?: boolean; animalId?: number }) =>
        client.put<CollarDevice>(`/collars/${id}`, data),

    remove: (id: number) =>
        client.delete(`/collars/${id}`),

    rotateSecret: (id: number) =>
        client.post<{ newSharedSecret: string; rotatedAt: string }>(
            `/collars/${id}/rotate-secret`
        ),

    findMyDog: (id: number) =>
        client.post(`/collars/${id}/command`, { command: 'buzz' }),
};

// api/locationApi.ts
import client from './client';
import type { LocationTrack, LocationPoint, MovementSummary } from '../types/collar';

export const locationApi = {
    getTrack: (collarId: number, from: string, to: string, maxPoints = 500) =>
        client.get<LocationTrack>(`/collars/${collarId}/locations`, {
            params: { from, to, maxPoints }
        }),

    getCurrent: (collarId: number) =>
        client.get<LocationPoint>(`/collars/${collarId}/location`),

    getStats: (collarId: number, from: string, to: string) =>
        client.get<{
            summary: CollarStats;
            daily: MovementSummary[];
            heatmap: HeatmapData;
        }>(`/collars/${collarId}/stats`, {
            params: { from, to }
        }),
};

// api/geofenceApi.ts
import client from './client';
import type { Geofence, GeofenceEvent } from '../types/collar';

export const geofenceApi = {
    list: (collarId?: number) =>
        client.get<Geofence[]>('/geofences', {
            params: collarId ? { collarId } : {}
        }),

    get: (id: number) =>
        client.get<Geofence>(`/geofences/${id}`),

    create: (data: Omit<Geofence, 'id' | 'createdAt' | 'updatedAt' | 'version' | 'areaM2' | 'perimeterM'>) =>
        client.post<Geofence>('/geofences', data),

    update: (id: number, data: Partial<Geofence>) =>
        client.put<Geofence>(`/geofences/${id}`, data),

    remove: (id: number) =>
        client.delete(`/geofences/${id}`),

    getEvents: (params: {
        collarId?: number;
        fenceId?: number;
        eventType?: string;
        from?: string;
        to?: string;
        page?: number;
        pageSize?: number;
    }) =>
        client.get<{ items: GeofenceEvent[]; totalCount: number }>('/geofences/events', { params }),
};
```

---

## 5. Leaflet Map Integration

### 5.1 SatelliteMap Component

```typescript
// components/map/SatelliteMap.tsx
import { MapContainer, TileLayer, LayersControl } from 'react-leaflet';

const TILE_LAYERS = {
    satellite: {
        url: '/api/v1/maps/tile/satellite/{z}/{x}/{y}',
        attribution: 'Tiles &copy; Esri &mdash; Source: Esri, Maxar, Earthstar Geographics',
        maxZoom: 20,
    },
    street: {
        url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
        attribution: '&copy; OpenStreetMap contributors',
        maxZoom: 19,
    },
    terrain: {
        url: '/api/v1/maps/tile/terrain/{z}/{x}/{y}',
        attribution: 'Map tiles by Stamen Design, under ODbL',
        maxZoom: 18,
    },
};

// Default center: user's first geofence centroid, or first collar's last location
// Default zoom: 19 (individual yard level)
```

### 5.2 Real-Time Location Updates

```typescript
// Polling approach (simple, reliable):
// Poll GET /collars/{id}/location every 5 seconds when map is visible

// Future WebSocket approach:
// ws://api/v1/collars/live → { collarId, lat, lng, speed, heading, battery }
// Server pushes on each new location batch from collar WiFi upload

const useCollarLocations = (collarIds: number[], intervalMs = 5000) => {
    const [locations, setLocations] = useState<Map<number, LocationPoint>>(new Map());

    useEffect(() => {
        const poll = async () => {
            const results = await Promise.all(
                collarIds.map(id => locationApi.getCurrent(id).catch(() => null))
            );
            const newLocations = new Map<number, LocationPoint>();
            collarIds.forEach((id, i) => {
                if (results[i]) newLocations.set(id, results[i]!);
            });
            setLocations(newLocations);
        };

        poll(); // Initial fetch
        const interval = setInterval(poll, intervalMs);
        return () => clearInterval(interval);
    }, [collarIds, intervalMs]);

    return locations;
};
```

### 5.3 Geofence Drawing

```typescript
// Uses react-leaflet-draw for interactive fence creation
// Polygon: click vertices, double-click to close
// Circle: click center, drag to set radius
// Corridor: click waypoints, set width in properties panel

// On draw complete:
// 1. Extract GeoJSON geometry from Leaflet layer
// 2. Open properties panel (name, action, buzzer, collars)
// 3. On save: POST /geofences with geometry + properties
// 4. On map: render with color coding (green=allow, red=deny, blue=corridor)
```

---

## 6. Dashboard Integration

The existing Dashboard page gets a new "Collar Status" section:

```
┌─ Collar Status ──────────────────────────────────────────────┐
│                                                               │
│  ● Luna's Collar    Backyard, walking    🔋 78%    2 min ago │
│  ○ Max's Collar     Indoor, idle         🔋 23%⚡  15 min ago│
│                                                               │
│  Today: 2 dogs tracked │ 2,415m total │ 0 breaches           │
│                                                [View Map →]   │
└───────────────────────────────────────────────────────────────┘
```

---

## 7. Notification Preferences Integration

The existing Notifications page (`/notifications`) gets new toggles:

```
┌─ Collar Alerts ──────────────────────────────────────────────┐
│                                                               │
│  Geofence Breach          [Email ✓] [SMS ✓]                 │
│  Low Battery (< 15%)      [Email ✓] [SMS ✓]                 │
│  Critical Battery (< 5%)  [Email ✓] [SMS ✓]                 │
│  Collar Offline (> 1hr)   [Email ✓] [SMS ○]                 │
│  Sustained Breach (> 5m)  [Email ✓] [SMS ✓]                 │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

---

## 8. NPM Dependencies

New packages needed for the map features:

```json
{
    "dependencies": {
        "leaflet": "^1.9.4",
        "react-leaflet": "^4.2.1",
        "leaflet-draw": "^1.0.4",
        "react-leaflet-draw": "^0.20.4",
        "leaflet.heat": "^0.2.0",
        "qrcode.react": "^3.1.0",
        "@types/leaflet": "^1.9.8",
        "@types/leaflet-draw": "^1.0.11"
    }
}
```
