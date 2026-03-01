# Collar System — Helm & Deployment Changes

## Overview

Deployment changes needed to support the collar system's additional API services, database tables, background jobs, and map tile caching.

---

## 1. Database Migration

### 1.1 New EF Core Migration

```bash
cd src/DogDoor.Api
dotnet ef migrations add AddCollarSystem
```

This migration creates:
- `collar_devices` table
- `location_points` table (with time-based index)
- `geofences` table
- `geofence_collar_devices` join table
- `geofence_events` table
- `movement_summaries` table
- Adds `collar_device_id`, `identification_method`, `camera_confidence`, `collar_confidence` columns to `door_events`
- Adds 6 new notification preference columns to `notification_preferences`

### 1.2 TimescaleDB Consideration

For production deployments with many collars, the `location_points` table benefits from time-series optimizations. TimescaleDB is a PostgreSQL extension that provides:
- Automatic time-based partitioning (hypertables)
- Efficient time-range queries
- Built-in downsampling functions
- Compression for older data

**Optional**: Add TimescaleDB to the PostgreSQL deployment:

```yaml
# helm/dog-door/values.yaml (additions)
postgresql:
  image: timescale/timescaledb:latest-pg16
  extensions:
    - timescaledb
  initScripts:
    create-hypertable.sql: |
      -- Run after EF Core migration creates the table
      SELECT create_hypertable('location_points', 'timestamp',
        chunk_time_interval => INTERVAL '1 day');
      -- Enable compression on chunks older than 7 days
      ALTER TABLE location_points SET (
        timescaledb.compress,
        timescaledb.compress_segmentby = 'collar_device_id'
      );
      SELECT add_compression_policy('location_points', INTERVAL '7 days');
```

Without TimescaleDB, the standard PostgreSQL table with proper indexing handles moderate loads (< 10 collars) fine.

---

## 2. Helm Chart Changes

### 2.1 Updated ConfigMap

```yaml
# helm/dog-door/templates/configmap.yaml (additions)

# Collar configuration
Collar__MaxDevicesPerUser: "5"
Collar__LocationBatchMaxSize: "1000"
Collar__LocationRetentionDays: "30"
Collar__GeofenceMaxPerUser: "20"

# Map tile proxy
Maps__TileCachePath: "/data/tiles"
Maps__TileCacheMaxAgeDays: "30"
Maps__SatelliteProvider: "esri"

# Background services
BackgroundServices__LocationRetentionCron: "0 2 * * *"  # 2 AM daily
BackgroundServices__CollarHealthCheckIntervalMinutes: "5"
```

### 2.2 Updated Secrets

```yaml
# helm/dog-door/templates/secrets.yaml (additions)

# No new secrets needed — collar auth uses existing DB-stored BCrypt hashes
# FCM/APNs keys (future, for mobile push notifications)
# Firebase__ServerKey: ""
# Apple__APNs__KeyId: ""
# Apple__APNs__TeamId: ""
```

### 2.3 Persistent Volume for Tile Cache

```yaml
# helm/dog-door/templates/pvc-tiles.yaml (NEW)

apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: {{ include "dog-door.fullname" . }}-tiles
  labels:
    {{- include "dog-door.labels" . | nindent 4 }}
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 5Gi  # ~5GB for cached satellite tiles
  storageClassName: {{ .Values.tiles.storageClassName | default "standard" }}
```

### 2.4 Updated API Deployment

```yaml
# helm/dog-door/templates/deployment-api.yaml (additions to volumes/volumeMounts)

spec:
  template:
    spec:
      containers:
        - name: api
          volumeMounts:
            # Existing
            - name: uploads
              mountPath: /app/uploads
            # New: tile cache
            - name: tiles
              mountPath: /data/tiles
      volumes:
        # Existing
        - name: uploads
          persistentVolumeClaim:
            claimName: {{ include "dog-door.fullname" . }}-uploads
        # New
        - name: tiles
          persistentVolumeClaim:
            claimName: {{ include "dog-door.fullname" . }}-tiles
```

### 2.5 Updated values.yaml

```yaml
# helm/dog-door/values.yaml (additions)

# Tile cache storage
tiles:
  storageClassName: standard
  size: 5Gi

# Collar system feature flag
collar:
  enabled: true
  maxDevicesPerUser: 5
  locationRetentionDays: 30
  geofenceMaxPerUser: 20

# Map configuration
maps:
  tileProvider: esri
  tileCacheMaxAgeDays: 30
```

---

## 3. API Service Registration

New services to register in `Program.cs`:

```csharp
// Collar services
builder.Services.AddScoped<ICollarService, CollarService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IGeofenceService, GeofenceService>();
builder.Services.AddScoped<IMapTileService, MapTileService>();

// Background services
builder.Services.AddHostedService<LocationRetentionService>();
builder.Services.AddHostedService<CollarHealthMonitorService>();

// Rate limiting policies
builder.Services.AddRateLimiter(options =>
{
    // Existing auth rate limit
    options.AddFixedWindowLimiter("auth", opt => { /* ... */ });

    // New collar rate limits
    options.AddFixedWindowLimiter("collar-upload", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
    });

    options.AddFixedWindowLimiter("tile-proxy", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});
```

---

## 4. Docker Compose Changes (Local Dev)

```yaml
# docker-compose.yml (additions)

services:
  api:
    volumes:
      - ./uploads:/app/uploads
      - ./tiles:/data/tiles       # NEW: tile cache
    environment:
      - Collar__MaxDevicesPerUser=5
      - Maps__TileCachePath=/data/tiles

  # Optional: TimescaleDB instead of plain PostgreSQL
  # postgres:
  #   image: timescale/timescaledb:latest-pg16
  #   (rest same as existing postgres service)
```

---

## 5. CI/CD Additions

### 5.1 New Test Categories

```yaml
# .github/workflows/ci.yml (additions)

- name: Run collar integration tests
  run: dotnet test src/DogDoor.Api.Tests --filter "Category=Collar"

- name: Validate Helm templates with collar values
  run: |
    helm template dog-door helm/dog-door \
      --set collar.enabled=true \
      --set maps.tileProvider=esri
```

### 5.2 Firmware CI (Collar)

```yaml
# .github/workflows/firmware-collar.yml (NEW)

name: Collar Firmware CI
on:
  push:
    paths:
      - 'firmware/collar/**'
  pull_request:
    paths:
      - 'firmware/collar/**'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/cache@v4
        with:
          path: ~/.platformio
          key: pio-collar-${{ hashFiles('firmware/collar/platformio.ini') }}
      - name: Install PlatformIO
        run: pip install platformio
      - name: Build collar firmware
        run: cd firmware/collar && pio run -e collar
      - name: Run collar unit tests
        run: cd firmware/collar && pio test -e native
```

---

## 6. Resource Estimates

### 6.1 Database Storage (Per Collar Per Day)

| Data | Raw Size | After Retention |
|------|----------|----------------|
| GPS points (1Hz, 4hr active) | ~3.5 MB | ~350 KB (after 1-day downsample) |
| Geofence events | ~1 KB | ~1 KB (kept indefinitely) |
| Movement summary | ~200 B | ~200 B (kept indefinitely) |
| **Daily total** | **~3.5 MB** | **~350 KB** |

### 6.2 Annual Storage (Per Collar)

| Period | Size |
|--------|------|
| Last 24 hours (raw) | ~3.5 MB |
| Last 7 days (downsampled 10s) | ~2.5 MB |
| Last 30 days (downsampled 1min) | ~180 KB |
| Historical (daily summaries) | ~73 KB/year |
| **Total ~1 year** | **~6.3 MB** |

For 10 collars: ~63 MB/year — trivial for PostgreSQL.

### 6.3 API Memory

- LocationRetentionService: ~50 MB peak during daily compaction (batch delete)
- CollarHealthMonitorService: ~5 MB (lightweight queries)
- MapTileService: Disk-backed cache, minimal memory

### 6.4 Tile Cache Disk

| Zoom Level | Tiles per km² | Size per km² |
|-----------|--------------|-------------|
| 18 (street level) | ~4,000 | ~40 MB |
| 19 (yard level) | ~16,000 | ~160 MB |
| 20 (max detail) | ~64,000 | ~640 MB |

For a typical yard view (one property): ~5-20 MB of tiles cached. 5 GB PVC handles hundreds of properties.
