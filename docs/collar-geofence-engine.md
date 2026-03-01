# Virtual Geofence Engine — Technical Specification

## Overview

The geofence engine runs on the collar's ESP32-S3, evaluating the dog's GPS position against owner-defined virtual boundaries. Evaluations happen locally for instant response without network dependency. Breach events are reported to the API when connectivity is available.

---

## 1. Coordinate System & Assumptions

- All coordinates are WGS84 (GPS native datum)
- Latitude/longitude stored as IEEE 754 `double` (64-bit) for full precision
- At yard scale (< 500m), the Earth's surface is treated as a flat plane using equirectangular projection
- Distances computed using the haversine formula for accuracy, with flat-earth approximation for hot-path containment checks

### 1.1 Equirectangular Projection (Fast Path)

For point-in-polygon and distance checks at yard scale, we project GPS coordinates to a local Cartesian plane:

```c
// Convert lat/lng to meters relative to a reference point
typedef struct {
    double x;  // meters east of reference
    double y;  // meters north of reference
} LocalPoint;

#define DEG_TO_RAD 0.01745329251994
#define EARTH_RADIUS 6371000.0  // meters

LocalPoint to_local(double lat, double lng, double ref_lat, double ref_lng) {
    LocalPoint p;
    p.x = (lng - ref_lng) * DEG_TO_RAD * EARTH_RADIUS * cos(ref_lat * DEG_TO_RAD);
    p.y = (lat - ref_lat) * DEG_TO_RAD * EARTH_RADIUS;
    return p;
}
```

The reference point is the centroid of the first defined geofence, cached on the device.

---

## 2. Fence Types

### 2.1 Polygon Fence

Arbitrary closed polygon defined by an ordered array of vertices. Used for yard boundaries, property lines, garden beds.

**Storage format:**
```c
typedef struct {
    uint16_t fence_id;
    uint8_t  action;         // 0=ALLOW, 1=DENY
    uint8_t  buzzer;         // 0=silent, 1=short, 2=long, 3=continuous
    uint8_t  vertex_count;   // 3-32 vertices
    double   vertices[];     // [lat0, lng0, lat1, lng1, ..., latN, lngN]
    // Total: 5 + (vertex_count * 16) bytes
} PolygonFence;
```

**Containment test: Ray-casting algorithm**

```c
bool point_in_polygon(double px, double py, const PolygonFence* fence) {
    bool inside = false;
    int n = fence->vertex_count;

    for (int i = 0, j = n - 1; i < n; j = i++) {
        double xi = fence->vertices[i * 2];      // lat_i → y
        double yi = fence->vertices[i * 2 + 1];  // lng_i → x
        double xj = fence->vertices[j * 2];
        double yj = fence->vertices[j * 2 + 1];

        // Convert to local coords for the test
        LocalPoint pi = to_local(xi, yi, ref_lat, ref_lng);
        LocalPoint pj = to_local(xj, yj, ref_lat, ref_lng);
        LocalPoint pp = to_local(px, py, ref_lat, ref_lng);

        if (((pi.y > pp.y) != (pj.y > pp.y)) &&
            (pp.x < (pj.x - pi.x) * (pp.y - pi.y) / (pj.y - pi.y) + pi.x)) {
            inside = !inside;
        }
    }
    return inside;
}
```

**Optimization**: Pre-compute local coordinates when fences are loaded (not on every GPS fix). Cache invalidated on fence update or reference point change.

### 2.2 Circle Fence

Center point + radius. Used for keep-away zones (pool, road, neighbor's property) or keep-in zones.

**Storage format:**
```c
typedef struct {
    uint16_t fence_id;
    uint8_t  action;       // 0=ALLOW, 1=DENY
    uint8_t  buzzer;
    double   center_lat;
    double   center_lng;
    float    radius_m;     // meters
    // Total: 24 bytes
} CircleFence;
```

**Containment test: Haversine distance**

```c
double haversine_distance(double lat1, double lng1, double lat2, double lng2) {
    double dlat = (lat2 - lat1) * DEG_TO_RAD;
    double dlng = (lng2 - lng1) * DEG_TO_RAD;
    double a = sin(dlat / 2) * sin(dlat / 2) +
               cos(lat1 * DEG_TO_RAD) * cos(lat2 * DEG_TO_RAD) *
               sin(dlng / 2) * sin(dlng / 2);
    double c = 2 * atan2(sqrt(a), sqrt(1 - a));
    return EARTH_RADIUS * c;
}

bool point_in_circle(double px, double py, const CircleFence* fence) {
    double dist = haversine_distance(px, py, fence->center_lat, fence->center_lng);
    return dist <= fence->radius_m;
}
```

### 2.3 Corridor Fence

A polyline (sequence of waypoints) with a width. Creates a path-shaped allowed zone. Used for designated walkways, paths to specific areas.

**Storage format:**
```c
typedef struct {
    uint16_t fence_id;
    uint8_t  action;
    uint8_t  buzzer;
    float    width_m;       // Half-width (distance from centerline to edge)
    uint8_t  waypoint_count; // 2-16 waypoints
    double   waypoints[];    // [lat0, lng0, lat1, lng1, ...]
    // Total: 8 + (waypoint_count * 16) bytes
} CorridorFence;
```

**Containment test: Perpendicular distance to nearest segment**

```c
double point_to_segment_distance(LocalPoint p, LocalPoint a, LocalPoint b) {
    double dx = b.x - a.x;
    double dy = b.y - a.y;
    double len_sq = dx * dx + dy * dy;

    if (len_sq == 0.0) {
        // Segment is a point
        double ex = p.x - a.x;
        double ey = p.y - a.y;
        return sqrt(ex * ex + ey * ey);
    }

    // Project point onto segment, clamped to [0,1]
    double t = ((p.x - a.x) * dx + (p.y - a.y) * dy) / len_sq;
    if (t < 0.0) t = 0.0;
    if (t > 1.0) t = 1.0;

    double proj_x = a.x + t * dx;
    double proj_y = a.y + t * dy;

    double ex = p.x - proj_x;
    double ey = p.y - proj_y;
    return sqrt(ex * ex + ey * ey);
}

bool point_in_corridor(double px, double py, const CorridorFence* fence) {
    LocalPoint p = to_local(px, py, ref_lat, ref_lng);

    for (int i = 0; i < fence->waypoint_count - 1; i++) {
        LocalPoint a = to_local(fence->waypoints[i*2], fence->waypoints[i*2+1],
                                ref_lat, ref_lng);
        LocalPoint b = to_local(fence->waypoints[(i+1)*2], fence->waypoints[(i+1)*2+1],
                                ref_lat, ref_lng);

        double dist = point_to_segment_distance(p, a, b);
        if (dist <= fence->width_m) {
            return true;  // Inside corridor at this segment
        }
    }
    return false;
}
```

---

## 3. Fence Evaluation Engine

### 3.1 Evaluation Order and Logic

```c
typedef enum {
    FENCE_OK,          // In bounds (all checks pass)
    FENCE_BREACH_DENY, // Inside a DENY zone
    FENCE_BREACH_ALLOW,// Outside all ALLOW zones
    FENCE_NO_FIX,      // No valid GPS fix
    FENCE_DISABLED      // Geofencing disabled
} FenceResult;

typedef struct {
    FenceResult result;
    uint16_t    fence_id;          // Offending fence (if breach)
    float       distance_to_fence; // Meters to nearest boundary
    uint8_t     breach_direction;  // 0=entered deny, 1=exited allow
} FenceEvaluation;

FenceEvaluation evaluate_fences(double lat, double lng) {
    FenceEvaluation eval;
    eval.result = FENCE_OK;
    eval.distance_to_fence = 9999.0;

    // Phase 1: Check all DENY fences (highest priority)
    for (int i = 0; i < deny_fence_count; i++) {
        bool inside = check_containment(&deny_fences[i], lat, lng);
        float dist = distance_to_boundary(&deny_fences[i], lat, lng);

        if (inside) {
            eval.result = FENCE_BREACH_DENY;
            eval.fence_id = deny_fences[i].fence_id;
            eval.distance_to_fence = dist;
            eval.breach_direction = 0;  // Entered deny zone
            return eval;  // Immediate breach — no need to check further
        }

        // Track nearest deny fence for proximity warning
        if (dist < eval.distance_to_fence) {
            eval.distance_to_fence = dist;
            eval.fence_id = deny_fences[i].fence_id;
        }
    }

    // Phase 2: Check ALLOW fences (if any defined)
    if (allow_fence_count == 0) {
        return eval;  // No allow fences = unrestricted
    }

    bool in_any_allow = false;
    float nearest_allow_dist = 9999.0;
    uint16_t nearest_allow_id = 0;

    for (int i = 0; i < allow_fence_count; i++) {
        bool inside = check_containment(&allow_fences[i], lat, lng);
        float dist = distance_to_boundary(&allow_fences[i], lat, lng);

        if (inside) {
            in_any_allow = true;
            break;  // In at least one allow zone — OK
        }

        if (dist < nearest_allow_dist) {
            nearest_allow_dist = dist;
            nearest_allow_id = allow_fences[i].fence_id;
        }
    }

    if (!in_any_allow) {
        eval.result = FENCE_BREACH_ALLOW;
        eval.fence_id = nearest_allow_id;
        eval.distance_to_fence = nearest_allow_dist;
        eval.breach_direction = 1;  // Exited allow zone
    }

    return eval;
}
```

### 3.2 Hysteresis Buffer

GPS noise can cause the position to oscillate across a fence boundary. A 3-meter hysteresis buffer prevents flapping:

```c
#define HYSTERESIS_M 3.0

typedef struct {
    bool     was_in_breach;
    uint16_t breach_fence_id;
    uint32_t breach_start_time;
} HysteresisState;

HysteresisState hysteresis;

bool apply_hysteresis(FenceEvaluation* eval) {
    if (eval->result == FENCE_BREACH_DENY || eval->result == FENCE_BREACH_ALLOW) {
        if (!hysteresis.was_in_breach) {
            // New breach — require position to be at least HYSTERESIS_M inside the zone
            // (for deny) or outside the zone (for allow) before triggering
            if (eval->distance_to_fence < HYSTERESIS_M) {
                // Too close to boundary — could be GPS noise
                eval->result = FENCE_OK;  // Suppress breach
                return false;
            }
            hysteresis.was_in_breach = true;
            hysteresis.breach_fence_id = eval->fence_id;
            hysteresis.breach_start_time = millis();
            return true;  // Confirmed breach
        }
        return true;  // Still in breach
    } else {
        if (hysteresis.was_in_breach) {
            // Was in breach, now appears OK — require HYSTERESIS_M buffer before clearing
            float dist = distance_to_boundary_by_id(hysteresis.breach_fence_id,
                                                      current_lat, current_lng);
            if (dist < HYSTERESIS_M) {
                eval->result = hysteresis.breach_fence_id < 0x8000 ?
                    FENCE_BREACH_DENY : FENCE_BREACH_ALLOW;
                return true;  // Still in breach (within hysteresis)
            }
            hysteresis.was_in_breach = false;  // Cleared breach
        }
        return false;
    }
}
```

### 3.3 GPS Quality Gating

Geofence evaluation is suppressed when GPS quality is poor:

```c
bool gps_quality_ok(const GpsFix* fix) {
    // Suppress geofencing if:
    if (fix->fix_type < 2) return false;           // No 2D/3D fix
    if (fix->hdop > 5.0) return false;             // Poor horizontal accuracy
    if (fix->satellites < 4) return false;          // Too few satellites
    if (fix->age_ms > 30000) return false;          // Fix is stale (>30s)
    if (fix->accuracy_m > 15.0) return false;       // Reported accuracy too poor

    return true;
}
```

---

## 4. Breach Response System

### 4.1 Alert Escalation

```
Time since breach    Action
──────────────────────────────────────────────
0s                   Piezo: short double-beep
                     BLE notify: breach event
                     LED: flash red
                     WiFi: queue high-priority event

10s                  Piezo: longer triple-beep
                     (if still in breach)

30s                  Piezo: continuous tone (2s on, 1s off)
                     WiFi: force-upload breach event NOW
                     API: trigger owner notification (push/SMS)

60s                  Continuous tone persists
                     API: escalate (second notification)

5min                 Tone reduces to periodic beeps (battery saving)
                     API: log sustained breach
```

### 4.2 Buzzer Patterns

```c
typedef enum {
    BUZZ_SILENT    = 0,
    BUZZ_SHORT     = 1,  // 100ms on, 100ms off, 100ms on
    BUZZ_LONG      = 2,  // 500ms on, 200ms off, 500ms on
    BUZZ_CONTINUOUS = 3  // 2000ms on, 1000ms off, repeat
} BuzzerPattern;

void play_buzz_pattern(BuzzerPattern pattern) {
    switch (pattern) {
        case BUZZ_SHORT:
            tone(BUZZER_PIN, 2700, 100); delay(200);
            tone(BUZZER_PIN, 2700, 100);
            break;
        case BUZZ_LONG:
            tone(BUZZER_PIN, 2200, 500); delay(700);
            tone(BUZZER_PIN, 2200, 500);
            break;
        case BUZZ_CONTINUOUS:
            tone(BUZZER_PIN, 2500, 2000); delay(3000);
            break;
        case BUZZ_SILENT:
        default:
            break;
    }
}
```

### 4.3 Proximity Warnings

Before a breach occurs, the collar can warn the dog (and owner) when approaching a boundary:

```c
#define WARNING_DISTANCE_M  5.0   // Warn when within 5m of boundary
#define WARNING_INTERVAL_MS 5000  // Max one warning every 5s

void check_proximity_warnings(double lat, double lng) {
    static uint32_t last_warning = 0;
    if (millis() - last_warning < WARNING_INTERVAL_MS) return;

    for (int i = 0; i < deny_fence_count; i++) {
        float dist = distance_to_boundary(&deny_fences[i], lat, lng);
        if (dist < WARNING_DISTANCE_M && dist > 0) {
            play_buzz_pattern(BUZZ_SHORT);
            last_warning = millis();
            // BLE notify: approaching fence boundary
            ble_notify_proximity(deny_fences[i].fence_id, dist);
            return;
        }
    }

    for (int i = 0; i < allow_fence_count; i++) {
        float dist = distance_to_boundary(&allow_fences[i], lat, lng);
        // For ALLOW fences, warn when approaching the EXIT (boundary from inside)
        bool inside = check_containment(&allow_fences[i], lat, lng);
        if (inside && dist < WARNING_DISTANCE_M) {
            play_buzz_pattern(BUZZ_SHORT);
            last_warning = millis();
            ble_notify_proximity(allow_fences[i].fence_id, dist);
            return;
        }
    }
}
```

---

## 5. Fence Synchronization

### 5.1 Fence Data Format (API → Collar)

The API sends fences as a compact JSON array that the collar parses and stores in NVS:

```json
{
    "version": 3,
    "reference": { "lat": 33.4484, "lng": -112.0740 },
    "fences": [
        {
            "id": 1,
            "name": "Backyard",
            "type": "polygon",
            "action": "allow",
            "buzzer": "short",
            "vertices": [
                [33.44850, -112.07410],
                [33.44850, -112.07380],
                [33.44830, -112.07380],
                [33.44830, -112.07410]
            ]
        },
        {
            "id": 2,
            "name": "Pool",
            "type": "circle",
            "action": "deny",
            "buzzer": "continuous",
            "center": [33.44842, -112.07395],
            "radius": 3.0
        },
        {
            "id": 3,
            "name": "Side Path",
            "type": "corridor",
            "action": "allow",
            "buzzer": "silent",
            "width": 2.0,
            "waypoints": [
                [33.44850, -112.07410],
                [33.44855, -112.07420],
                [33.44860, -112.07430]
            ]
        }
    ]
}
```

### 5.2 Sync Protocol

```
Collar                                    API
  │                                        │
  │── GET /geofences?collarId=X           │
  │   &updatedSince=2026-02-28T00:00:00Z  │
  │                                        │
  │◀── 200 { version: 3, fences: [...] }  │
  │                                        │
  │  (if version > local_version)          │
  │  Parse and store in NVS                │
  │  Pre-compute local coordinates         │
  │  Update local_version = 3              │
  │                                        │
  │── POST /collars/{id}/fence-sync-ack   │
  │   { version: 3, fence_count: 3 }      │
  │                                        │
  │◀── 200 OK                              │
```

### 5.3 NVS Storage Layout

```
NVS Namespace: "geofence"
├── "version"    → uint32_t (fence set version number)
├── "ref_lat"    → double (reference latitude)
├── "ref_lng"    → double (reference longitude)
├── "count"      → uint8_t (number of fences)
├── "fence_0"    → blob (compact binary fence data)
├── "fence_1"    → blob
├── ...
└── "fence_19"   → blob (max 20 fences)

Total NVS usage: ~2-4 KB for typical yard setup
```

---

## 6. Performance Budget

| Operation | Time | Frequency |
|-----------|------|-----------|
| GPS fix parse (NMEA) | < 1ms | 1-10 Hz |
| Point-in-polygon (8 vertices) | ~5us | Per fix per fence |
| Point-in-circle (haversine) | ~2us | Per fix per fence |
| Point-in-corridor (4 segments) | ~8us | Per fix per fence |
| Full evaluation (10 fences) | ~50us | Per fix |
| Hysteresis check | ~1us | Per fix |
| Buzzer tone generation | 0ms (hardware PWM) | On breach |
| BLE notify | ~5ms | On state change |
| WiFi upload (batch) | ~200ms | Every 30s |

Total per-fix CPU time: **< 100us** — negligible compared to GPS fix interval (100ms-1000ms).

---

## 7. Distance-to-Boundary Calculation

For proximity warnings and hysteresis, we need to compute the shortest distance from the current position to a fence boundary.

### 7.1 Polygon Distance

```c
float distance_to_polygon_boundary(const PolygonFence* fence, double lat, double lng) {
    LocalPoint p = to_local(lat, lng, ref_lat, ref_lng);
    float min_dist = FLT_MAX;

    for (int i = 0, j = fence->vertex_count - 1; i < fence->vertex_count; j = i++) {
        LocalPoint a = fence->local_vertices[j];  // Pre-computed
        LocalPoint b = fence->local_vertices[i];

        float dist = point_to_segment_distance(p, a, b);
        if (dist < min_dist) {
            min_dist = dist;
        }
    }
    return min_dist;
}
```

### 7.2 Circle Distance

```c
float distance_to_circle_boundary(const CircleFence* fence, double lat, double lng) {
    double dist = haversine_distance(lat, lng, fence->center_lat, fence->center_lng);
    return fabs(dist - fence->radius_m);  // Distance to the ring
}
```

### 7.3 Corridor Distance

```c
float distance_to_corridor_boundary(const CorridorFence* fence, double lat, double lng) {
    LocalPoint p = to_local(lat, lng, ref_lat, ref_lng);
    float min_centerline_dist = FLT_MAX;

    for (int i = 0; i < fence->waypoint_count - 1; i++) {
        LocalPoint a = fence->local_waypoints[i];
        LocalPoint b = fence->local_waypoints[i + 1];
        float dist = point_to_segment_distance(p, a, b);
        if (dist < min_centerline_dist) {
            min_centerline_dist = dist;
        }
    }

    // Distance to corridor boundary = distance to centerline - half-width
    // If inside corridor: how far from the edge
    // If outside corridor: how far from the edge
    return fabs(min_centerline_dist - fence->width_m);
}
```

---

## 8. Unit Test Cases

```
test_point_in_polygon_simple_square
  ✓ Point inside square → true
  ✓ Point outside square → false
  ✓ Point on edge → true (or false, consistent behavior)
  ✓ Point on vertex → true

test_point_in_polygon_concave
  ✓ Point in concave region → true
  ✓ Point in concavity gap → false

test_point_in_polygon_complex
  ✓ L-shaped polygon, point in each wing → true
  ✓ L-shaped polygon, point in the gap → false

test_point_in_circle
  ✓ Point at center → true
  ✓ Point at radius → true
  ✓ Point at radius + 1m → false
  ✓ Point at radius - 1m → true

test_point_in_corridor
  ✓ Point on centerline → true
  ✓ Point at width boundary → true
  ✓ Point outside width → false
  ✓ Point beyond segment end, within width → true (segment capping)
  ✓ Point beyond segment end, outside width → false

test_fence_evaluation
  ✓ In allow zone, no deny zones → OK
  ✓ In allow zone AND deny zone → BREACH (deny overrides)
  ✓ Outside all allow zones → BREACH
  ✓ No fences defined → OK (unrestricted)
  ✓ Only deny fences, not in any → OK
  ✓ Multiple allow zones, in one → OK

test_hysteresis
  ✓ Cross boundary by 1m → suppressed (within hysteresis)
  ✓ Cross boundary by 5m → breach triggered
  ✓ Return to 1m inside boundary → still in breach (hysteresis)
  ✓ Return to 5m inside boundary → breach cleared

test_gps_quality_gating
  ✓ No fix → evaluation skipped
  ✓ HDOP > 5 → evaluation skipped
  ✓ Fix age > 30s → evaluation skipped
  ✓ Good fix → evaluation runs
```
