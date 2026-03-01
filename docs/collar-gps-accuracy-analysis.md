# GPS Accuracy Analysis for Yard-Scale Geofencing

## Overview

Analysis of GPS accuracy limitations and their impact on geofence reliability at yard scale (10-100m boundaries), with techniques to improve position accuracy.

---

## 1. GPS Error Sources at Yard Scale

### 1.1 Error Budget

| Error Source | Magnitude | Impact on Geofencing |
|-------------|-----------|---------------------|
| Ionospheric delay | 2-5m | Consistent bias, correctable with SBAS |
| Tropospheric delay | 0.5-2m | Weather-dependent, slowly varying |
| Satellite geometry (HDOP) | 1-4m | Variable, worse near buildings |
| Multipath (reflections) | 1-10m | Worst near walls, fences, vehicles |
| Receiver noise | 0.5-1m | Constant, irreducible |
| Satellite clock error | < 1m | Corrected by navigation message |
| **Typical total (RMS)** | **2-5m** | **3m hysteresis handles most cases** |

### 1.2 Real-World Accuracy by Conditions

| Conditions | Horizontal Accuracy (95%) | Suitable Fence Size |
|-----------|--------------------------|---------------------|
| Open sky, clear day | 1.5-3m | Fences > 5m from boundary |
| Suburban yard (some sky view) | 2-5m | Fences > 8m from boundary |
| Near house wall (one side blocked) | 3-8m | Fences > 12m from boundary |
| Under tree canopy | 4-10m | Fences > 15m from boundary |
| Between buildings (urban canyon) | 5-20m | Unreliable; consider BLE |
| Indoor | No reliable fix | Not applicable |

### 1.3 SBAS Correction (WAAS/EGNOS)

The u-blox MAX-M10S supports SBAS (Satellite-Based Augmentation System):
- **WAAS** (North America): Corrections via geostationary satellites
- **EGNOS** (Europe): Same concept, European coverage
- **MSAS** (Japan): Japanese coverage

SBAS typically reduces horizontal error by 30-50%:
- Without SBAS: 2-5m accuracy
- With SBAS: 1.5-3m accuracy

```c
// Enable SBAS in firmware
gnss.setSBAS(true);
gnss.setSBASsystem(0);  // 0 = auto-detect (WAAS/EGNOS/MSAS based on location)
```

---

## 2. Impact on Geofence Reliability

### 2.1 False Breach Rate

When GPS accuracy is 3m (95% confidence) and the dog is standing 3m inside the fence boundary:

```
Dog position:    3m inside boundary
GPS error:       3m (95% confidence = 2σ)
                 → 5% of readings could show position outside boundary

With 1Hz GPS:    ~3 false positions per minute (3 out of 60)
With 3m hysteresis: Need to be 3m OUTSIDE boundary to trigger
  → Dog at true 3m inside boundary:
    GPS might show 0m inside (on boundary) — no breach (hysteresis)
    GPS might show 1m outside — no breach (within 3m hysteresis)
    GPS rarely shows 4m outside — would need 6m total error (< 0.1%)
```

**Conclusion:** The 3-meter hysteresis almost completely eliminates false breaches for dogs that are at least 3m inside the boundary. Dogs lingering right at the boundary edge will experience occasional false warnings.

### 2.2 Missed Breach Rate

When the dog is 3m OUTSIDE the boundary:

```
Dog position:    3m outside boundary
GPS error:       3m
With 3m hysteresis: Need 3m outside + 3m hysteresis = 6m apparent displacement

  → GPS shows dog 3m outside: but 3m < (3m hysteresis from boundary) → suppressed
  → GPS shows dog 6m outside: BREACH (but only ~5% of readings)
  → Most readings show 2-4m outside: still within hysteresis
```

**Problem:** A dog 3m outside the boundary might not trigger a breach for several seconds (until a reading exceeds the hysteresis).

**Mitigation:** Use consecutive-reading confirmation instead of single-sample hysteresis:

```c
// Alternative: require N consecutive readings outside boundary
#define BREACH_CONFIRM_COUNT  3   // 3 consecutive readings (~3s at 1Hz)
#define BREACH_CONFIRM_DIST   1.0 // Each reading must be >1m outside

static int consecutive_outside = 0;

bool confirm_breach(float distance_outside) {
    if (distance_outside > BREACH_CONFIRM_DIST) {
        consecutive_outside++;
        if (consecutive_outside >= BREACH_CONFIRM_COUNT) {
            return true;  // Confirmed breach
        }
    } else {
        consecutive_outside = 0;  // Reset
    }
    return false;
}
```

This approach: 3 consecutive readings outside the boundary (even by 1m each) confirms a breach. Single GPS jumps are filtered. Detection delay: 3 seconds.

### 2.3 Minimum Fence Size

Given GPS accuracy limitations, fences below a certain size become unreliable:

| Fence Type | Minimum Useful Size | Reasoning |
|-----------|-------------------|-----------|
| Polygon (yard) | 20m × 20m | 5m error on each side; need clear margin |
| Circle (keep-away) | Radius > 5m | With 3m GPS error + 3m hysteresis = 6m dead zone |
| Corridor | Width > 4m | 2m GPS error on each side of centerline |

**For small zones (< 5m):** GPS alone is insufficient. Consider supplementing with:
- BLE proximity beacons placed at the zone (e.g., a BLE beacon near the pool)
- UWB ranging (future, when ESP32-S3 UWB is available)
- NFC tags embedded in the ground near restricted areas

---

## 3. Position Filtering Algorithms

### 3.1 Simple Moving Average

```c
#define POSITION_FILTER_SIZE  5

typedef struct {
    double lat[POSITION_FILTER_SIZE];
    double lng[POSITION_FILTER_SIZE];
    int index;
    int count;
} PositionFilter;

void filter_add(PositionFilter* f, double lat, double lng) {
    f->lat[f->index] = lat;
    f->lng[f->index] = lng;
    f->index = (f->index + 1) % POSITION_FILTER_SIZE;
    if (f->count < POSITION_FILTER_SIZE) f->count++;
}

void filter_get_average(PositionFilter* f, double* lat, double* lng) {
    double sum_lat = 0, sum_lng = 0;
    for (int i = 0; i < f->count; i++) {
        sum_lat += f->lat[i];
        sum_lng += f->lng[i];
    }
    *lat = sum_lat / f->count;
    *lng = sum_lng / f->count;
}
```

**Pros:** Simple, removes random noise.
**Cons:** Introduces lag when moving (averaged position trails actual position).
**Best for:** Stationary or slow-moving dogs.

### 3.2 Speed-Adaptive Filter

```c
void filter_position(double raw_lat, double raw_lng, float speed,
                     double* filtered_lat, double* filtered_lng) {
    // Blending factor: trust raw GPS more when moving fast
    // (fast movement means old averaged positions are stale)
    float alpha;
    if (speed > 3.0) {
        alpha = 0.9;  // Trust raw GPS (running dog)
    } else if (speed > 0.5) {
        alpha = 0.5;  // Blend 50/50 (walking dog)
    } else {
        alpha = 0.1;  // Trust average (stationary dog, GPS noise)
    }

    *filtered_lat = alpha * raw_lat + (1 - alpha) * prev_lat;
    *filtered_lng = alpha * raw_lng + (1 - alpha) * prev_lng;

    prev_lat = *filtered_lat;
    prev_lng = *filtered_lng;
}
```

**Pros:** Minimal lag when moving, good smoothing when stationary.
**Cons:** Needs reliable speed estimate (from IMU).

### 3.3 IMU-Assisted Dead Reckoning

When GPS is temporarily unavailable (under dense tree canopy, passing behind a building):

```c
// Use IMU to estimate position change since last good GPS fix
typedef struct {
    double base_lat;
    double base_lng;
    uint32_t base_time;
    float accumulated_north_m;  // Meters north of base
    float accumulated_east_m;   // Meters east of base
} DeadReckon;

void dead_reckon_update(DeadReckon* dr, float heading_deg, float speed_ms, float dt_s) {
    float heading_rad = heading_deg * DEG_TO_RAD;
    dr->accumulated_north_m += speed_ms * cos(heading_rad) * dt_s;
    dr->accumulated_east_m  += speed_ms * sin(heading_rad) * dt_s;
}

void dead_reckon_get_position(DeadReckon* dr, double* lat, double* lng) {
    *lat = dr->base_lat + (dr->accumulated_north_m / 111320.0);
    *lng = dr->base_lng + (dr->accumulated_east_m /
                           (111320.0 * cos(dr->base_lat * DEG_TO_RAD)));
}

// Accuracy degrades over time:
// After 10s: ~2m drift (IMU integration error)
// After 30s: ~10m drift (unreliable, suppress geofence evaluation)
// After 60s: abandon dead reckoning, wait for GPS fix
```

---

## 4. Geofence Design Recommendations for Users

### 4.1 Setting Appropriate Margins

When drawing geofences in the SPA, users should account for GPS accuracy:

```
Physical fence line
     │
     │ ← 3m margin (GPS error buffer)
     │
     │ ← Geofence polygon edge drawn HERE
     │
     │ ← 3m hysteresis zone
     │
     │ ← Warning zone (5m before geofence edge)
     │
```

**Rule of thumb:** Draw geofences at least 3 meters inside the actual boundary you want to enforce.

### 4.2 Avoid Narrow Peninsulas

GPS noise can cause position to "jump" in and out of narrow fence extensions:

```
BAD: Narrow peninsula fence           GOOD: Rounded fence
┌────────────────┐                    ┌────────────────┐
│                │                    │                │
│    ┌──┐        │                    │    ┌────┐      │
│    │  │ ← 2m wide (too narrow,     │    │    │      │
│    │  │   constant false alerts)    │    │    │ ← 6m wide (GPS can
│    └──┘        │                    │    └────┘   track reliably)
│                │                    │                │
└────────────────┘                    └────────────────┘
```

### 4.3 Use Circles for Small Exclusion Zones

For small areas (pool, garden bed), circles are better than tight polygons:

```
GOOD: Circle with generous radius     BAD: Tight polygon around small area

    ╭─────────╮                        ┌─┐
   ╱           ╲                       │ │ ← 2m × 3m (GPS noise
  │    Pool     │  ← r=5m (includes    │ │   makes this useless)
  │   (3m×5m)   │    GPS margin)       └─┘
   ╲           ╱
    ╰─────────╯
```

### 4.4 SPA Warning System

The geofence editor should warn users about problematic configurations:

```javascript
function validateFence(fence) {
    const warnings = [];

    if (fence.type === 'polygon') {
        const area = computeArea(fence.boundary);
        if (area < 50) {  // m²
            warnings.push("This fence covers less than 50m². GPS accuracy " +
                          "may cause frequent false alerts. Consider using " +
                          "a larger area or a BLE proximity beacon.");
        }

        const minEdgeLength = computeMinEdgeLength(fence.boundary);
        if (minEdgeLength < 4) {
            warnings.push("This fence has edges shorter than 4m. GPS noise " +
                          "may cause the position to jump across narrow sections.");
        }
    }

    if (fence.type === 'circle' && fence.boundary.radius < 5) {
        warnings.push("A radius of " + fence.boundary.radius + "m is close to " +
                      "GPS accuracy limits. Consider increasing to at least 5m.");
    }

    if (fence.type === 'corridor' && fence.boundary.width < 4) {
        warnings.push("A corridor width of " + fence.boundary.width + "m may " +
                      "be too narrow for reliable GPS tracking.");
    }

    return warnings;
}
```

---

## 5. Future Accuracy Improvements

### 5.1 RTK (Real-Time Kinematic) GPS

RTK provides centimeter-level accuracy using a local base station:
- **Base station**: u-blox ZED-F9P ($200) at a fixed known position (on the house)
- **Rover**: u-blox ZED-F9P on the collar (replaces MAX-M10S, $200 vs $12)
- **Accuracy**: 2-3 cm (100x improvement!)
- **Limitation**: Expensive ($400 total), higher power draw, base station needed

**Verdict:** Overkill for most users. Worth it for commercial deployments or very small geofences.

### 5.2 BLE Proximity Beacons

For small exclusion zones (pool, garden):
- Place a BLE beacon (ESP32-C3, $5) at the zone
- Collar measures RSSI to the beacon
- RSSI < -40 dBm → within ~2m → trigger warning
- No GPS accuracy issues at close range
- Power draw: minimal (BLE scanning already active)

```c
// Check proximity to pool beacon
int pool_rssi = ble_get_beacon_rssi("pool-beacon-mac");
if (pool_rssi > -50) {
    // Very close to pool beacon (< 2m)
    trigger_proximity_warning("Pool");
}
```

### 5.3 UWB (Ultra-Wideband) Ranging

ESP32-S3 may support UWB in future revisions:
- **Accuracy**: 10-30 cm
- **Range**: 10-50m
- **Power**: Moderate (~30 mA during ranging)
- **Requires**: UWB anchor points (on door unit, on house corners)
- **Status**: Not available on current ESP32-S3; future hardware revision expected

### 5.4 Dual-Frequency GPS (L1 + L5)

Next-generation GPS chips (u-blox F10 series) support L1 + L5 dual frequency:
- **Accuracy**: Sub-meter (0.5-1m)
- **Multipath rejection**: Much better than L1-only
- **Cost**: ~$20 (vs $12 for MAX-M10S)
- **Status**: Available but not yet in compact collar-sized modules

---

## 6. Accuracy Testing Protocol

### 6.1 Static Accuracy Test

1. Place the collar at a known surveyed point (e.g., corner of driveway, property pin)
2. Record GPS positions for 30 minutes (1800 readings at 1Hz)
3. Compute:
   - Mean position (should be close to true position)
   - CEP50 (Circular Error Probable, 50% of readings within this radius)
   - CEP95 (95% of readings within this radius)
   - Max error (worst single reading)

Expected results:
```
CEP50: 1.0-1.5m (good GPS conditions)
CEP95: 2.0-3.5m
Max:   4.0-8.0m
```

### 6.2 Dynamic Accuracy Test

1. Walk a known path (e.g., along a fence line with known GPS coordinates)
2. Record collar GPS track at 1Hz
3. Compare recorded track to known path
4. Compute cross-track error (perpendicular distance from true path)

Expected results:
```
Average cross-track error: 1.5-3.0m
95th percentile: 3.0-5.0m
Max deviation: 5.0-10.0m
```

### 6.3 Geofence Reliability Test

1. Define a test polygon (e.g., a 20m × 20m square in the yard)
2. Walk the dog on a leash along a path that:
   - Stays 5m inside the boundary for 5 minutes (no warnings expected)
   - Approaches to 2m inside the boundary (warnings expected)
   - Crosses the boundary by 3m (breach expected within 5 seconds)
   - Returns to 5m inside (breach clear expected within 5 seconds)
3. Log all geofence events and compare with actual positions

Success criteria:
- Zero false breaches when 5m inside boundary
- Breach detected within 5 seconds when 3m outside boundary
- Breach cleared within 5 seconds when returning to 5m inside
- Warning sounds when approaching within 5m of boundary
