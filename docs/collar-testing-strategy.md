# Collar System — Comprehensive Testing Strategy

## Overview

Testing plan across all layers: collar firmware unit tests, door firmware integration, API unit/integration tests, SPA component tests, and end-to-end system tests.

---

## 1. Collar Firmware Tests (PlatformIO native)

Run with: `cd firmware/collar && pio test -e native`

### 1.1 Geofence Engine Unit Tests

```cpp
// test/test_geofence/test_geofence.cpp

// ── Point-in-Polygon ─────────────────────────────────────────
void test_point_in_simple_square() {
    // Square: (0,0), (10,0), (10,10), (0,10)
    double vertices[] = {0,0, 10,0, 10,10, 0,10};
    PolygonFence fence = {1, ACTION_ALLOW, BUZZ_SILENT, 4, {}};
    memcpy(fence.vertices, vertices, sizeof(vertices));

    TEST_ASSERT_TRUE(point_in_polygon(5, 5, &fence));     // Center
    TEST_ASSERT_TRUE(point_in_polygon(1, 1, &fence));     // Near corner
    TEST_ASSERT_FALSE(point_in_polygon(11, 5, &fence));   // Outside right
    TEST_ASSERT_FALSE(point_in_polygon(-1, 5, &fence));   // Outside left
    TEST_ASSERT_FALSE(point_in_polygon(5, -1, &fence));   // Outside bottom
    TEST_ASSERT_FALSE(point_in_polygon(5, 11, &fence));   // Outside top
}

void test_point_in_concave_polygon() {
    // L-shaped polygon
    double vertices[] = {0,0, 10,0, 10,5, 5,5, 5,10, 0,10};
    PolygonFence fence = {2, ACTION_ALLOW, BUZZ_SILENT, 6, {}};
    memcpy(fence.vertices, vertices, sizeof(vertices));

    TEST_ASSERT_TRUE(point_in_polygon(2, 2, &fence));     // In lower rect
    TEST_ASSERT_TRUE(point_in_polygon(2, 8, &fence));     // In upper rect
    TEST_ASSERT_FALSE(point_in_polygon(8, 8, &fence));    // In the gap
}

void test_point_in_triangle() {
    double vertices[] = {0,0, 10,0, 5,10};
    PolygonFence fence = {3, ACTION_ALLOW, BUZZ_SILENT, 3, {}};
    memcpy(fence.vertices, vertices, sizeof(vertices));

    TEST_ASSERT_TRUE(point_in_polygon(5, 3, &fence));     // Inside
    TEST_ASSERT_FALSE(point_in_polygon(0, 10, &fence));   // Outside
}

// ── Point-in-Circle ──────────────────────────────────────────
void test_point_in_circle_center() {
    CircleFence fence = {4, ACTION_DENY, BUZZ_CONTINUOUS, 33.4484, -112.0740, 10.0};

    TEST_ASSERT_TRUE(point_in_circle(33.4484, -112.0740, &fence));   // Center
}

void test_point_in_circle_boundary() {
    CircleFence fence = {5, ACTION_DENY, BUZZ_SHORT, 33.4484, -112.0740, 10.0};

    // Point ~11m away (just outside)
    double lat_offset = 11.0 / 111320.0;  // ~11m in degrees
    TEST_ASSERT_FALSE(point_in_circle(33.4484 + lat_offset, -112.0740, &fence));

    // Point ~9m away (just inside)
    lat_offset = 9.0 / 111320.0;
    TEST_ASSERT_TRUE(point_in_circle(33.4484 + lat_offset, -112.0740, &fence));
}

// ── Point-in-Corridor ────────────────────────────────────────
void test_point_in_corridor_on_centerline() {
    double waypoints[] = {0, 0, 10, 0};
    CorridorFence fence = {6, ACTION_ALLOW, BUZZ_SILENT, 2.0, 2, {}};
    memcpy(fence.waypoints, waypoints, sizeof(waypoints));

    TEST_ASSERT_TRUE(point_in_corridor(5, 0, &fence));    // On centerline
}

void test_point_in_corridor_at_edge() {
    double waypoints[] = {0, 0, 10, 0};
    CorridorFence fence = {7, ACTION_ALLOW, BUZZ_SILENT, 2.0, 2, {}};
    memcpy(fence.waypoints, waypoints, sizeof(waypoints));

    TEST_ASSERT_TRUE(point_in_corridor(5, 1.9, &fence));  // Just inside edge
    TEST_ASSERT_FALSE(point_in_corridor(5, 2.5, &fence)); // Just outside edge
}

void test_point_in_corridor_curved() {
    // L-shaped corridor: (0,0) → (5,0) → (5,5)
    double waypoints[] = {0, 0, 5, 0, 5, 5};
    CorridorFence fence = {8, ACTION_ALLOW, BUZZ_SILENT, 1.5, 3, {}};
    memcpy(fence.waypoints, waypoints, sizeof(waypoints));

    TEST_ASSERT_TRUE(point_in_corridor(2.5, 0, &fence));  // First segment
    TEST_ASSERT_TRUE(point_in_corridor(5, 2.5, &fence));  // Second segment
    TEST_ASSERT_FALSE(point_in_corridor(2.5, 3, &fence)); // In the gap
}

// ── Evaluation Engine ────────────────────────────────────────
void test_eval_in_allow_zone_only() {
    // Setup: one allow polygon, no deny zones
    // Point inside allow zone → FENCE_OK
}

void test_eval_deny_overrides_allow() {
    // Setup: allow polygon covering yard + deny circle inside
    // Point inside deny circle (which is also inside allow polygon) → BREACH_DENY
}

void test_eval_outside_all_allow_zones() {
    // Setup: one allow polygon
    // Point outside → BREACH_ALLOW
}

void test_eval_no_fences_defined() {
    // No fences at all → FENCE_OK (unrestricted)
}

void test_eval_only_deny_fences() {
    // Only deny zones, no allow zones
    // Point not in any deny zone → FENCE_OK
}

void test_eval_multiple_allow_zones() {
    // Two separate allow polygons
    // Point in second polygon → FENCE_OK
}

// ── Hysteresis ───────────────────────────────────────────────
void test_hysteresis_suppresses_near_boundary() {
    // Point crosses boundary by 1m (< 3m hysteresis) → suppressed
}

void test_hysteresis_triggers_deep_breach() {
    // Point crosses boundary by 5m (> 3m hysteresis) → breach triggered
}

void test_hysteresis_maintains_breach_state() {
    // In breach → return to 1m inside boundary → still in breach
}

void test_hysteresis_clears_far_inside() {
    // In breach → return to 5m inside boundary → breach cleared
}

// ── GPS Quality Gating ──────────────────────────────────────
void test_gps_no_fix_skips_eval() {
    GpsFix fix = {.fix_type = 0};
    TEST_ASSERT_FALSE(gps_quality_ok(&fix));
}

void test_gps_high_hdop_skips_eval() {
    GpsFix fix = {.fix_type = 3, .hdop = 6.0, .satellites = 8, .age_ms = 0, .accuracy = 5.0};
    TEST_ASSERT_FALSE(gps_quality_ok(&fix));
}

void test_gps_stale_fix_skips_eval() {
    GpsFix fix = {.fix_type = 3, .hdop = 2.0, .satellites = 8, .age_ms = 35000, .accuracy = 5.0};
    TEST_ASSERT_FALSE(gps_quality_ok(&fix));
}

void test_gps_good_fix_allows_eval() {
    GpsFix fix = {.fix_type = 3, .hdop = 1.5, .satellites = 10, .age_ms = 500, .accuracy = 3.0};
    TEST_ASSERT_TRUE(gps_quality_ok(&fix));
}

// ── Distance to Boundary ─────────────────────────────────────
void test_distance_to_polygon_inside() {
    // Point 3m inside a square → distance = 3m
}

void test_distance_to_polygon_outside() {
    // Point 5m outside a square → distance = 5m
}

void test_distance_to_circle_inside() {
    // Point 2m from center of r=5m circle → distance to boundary = 3m
}

void test_distance_to_circle_outside() {
    // Point 8m from center of r=5m circle → distance to boundary = 3m
}
```

### 1.2 NFC Protocol Unit Tests

```cpp
// test/test_nfc/test_nfc_protocol.cpp

void test_hmac_sha256_known_vector() {
    // RFC 4231 Test Vector #1
    uint8_t key[20];
    memset(key, 0x0b, 20);
    const char* msg = "Hi There";
    uint8_t expected[] = {0xb0, 0x34, 0x4c, 0x61, ...};

    uint8_t result[32];
    compute_hmac_sha256(key, 20, (uint8_t*)msg, 8, result);
    TEST_ASSERT_EQUAL_MEMORY(expected, result, 32);
}

void test_tlv_encode_collar_announce() {
    uint8_t collar_id[16] = {0xa1, 0xb2, ...};
    uint8_t buffer[64];
    int len = encode_collar_announce(buffer, collar_id, 0x01, 0x07, "1.0.0", 78, 1234);

    TEST_ASSERT_EQUAL(0x01, buffer[0]);  // Type
    TEST_ASSERT_EQUAL(27, (buffer[1] << 8) | buffer[2]);  // Length
}

void test_tlv_decode_auth_challenge() {
    uint8_t challenge[57] = {0x02, 0x00, 54, /* nonce + door_id + timestamp */};
    AuthChallenge parsed;
    bool ok = decode_auth_challenge(challenge, 57, &parsed);

    TEST_ASSERT_TRUE(ok);
    TEST_ASSERT_EQUAL(32, sizeof(parsed.nonce));
    TEST_ASSERT_EQUAL(16, sizeof(parsed.door_id));
}

void test_timestamp_validation_in_range() {
    uint32_t now = time(NULL);
    TEST_ASSERT_TRUE(validate_timestamp(now - 15, now));   // 15s ago: OK
    TEST_ASSERT_TRUE(validate_timestamp(now + 15, now));   // 15s future: OK
}

void test_timestamp_validation_out_of_range() {
    uint32_t now = time(NULL);
    TEST_ASSERT_FALSE(validate_timestamp(now - 45, now));  // 45s ago: expired
    TEST_ASSERT_FALSE(validate_timestamp(now + 45, now));  // 45s future: suspicious
}

void test_constant_time_compare_equal() {
    uint8_t a[32], b[32];
    memset(a, 0xAA, 32);
    memset(b, 0xAA, 32);
    TEST_ASSERT_TRUE(constant_time_compare(a, b, 32));
}

void test_constant_time_compare_different() {
    uint8_t a[32], b[32];
    memset(a, 0xAA, 32);
    memset(b, 0xBB, 32);
    TEST_ASSERT_FALSE(constant_time_compare(a, b, 32));
}

void test_constant_time_compare_single_bit_diff() {
    uint8_t a[32], b[32];
    memset(a, 0xAA, 32);
    memcpy(b, a, 32);
    b[15] ^= 0x01;  // Flip one bit
    TEST_ASSERT_FALSE(constant_time_compare(a, b, 32));
}

void test_crc32_computation() {
    uint8_t data[] = {0x01, 0x00, 0x1B, /* COLLAR_ANNOUNCE payload */};
    uint32_t crc = compute_crc32(data, sizeof(data));
    TEST_ASSERT_NOT_EQUAL(0, crc);
    TEST_ASSERT_EQUAL(crc, compute_crc32(data, sizeof(data)));  // Deterministic
}
```

### 1.3 Power Manager Unit Tests

```cpp
// test/test_power/test_power.cpp

void test_battery_percentage_full() {
    TEST_ASSERT_EQUAL_FLOAT(100.0, voltage_to_percentage(4.20));
}

void test_battery_percentage_empty() {
    TEST_ASSERT_EQUAL_FLOAT(0.0, voltage_to_percentage(3.20));
}

void test_battery_percentage_nominal() {
    float pct = voltage_to_percentage(3.80);
    TEST_ASSERT_FLOAT_WITHIN(5.0, 40.0, pct);  // ~40% at 3.80V
}

void test_battery_percentage_clamped() {
    TEST_ASSERT_EQUAL_FLOAT(100.0, voltage_to_percentage(4.50));  // Over-voltage
    TEST_ASSERT_EQUAL_FLOAT(0.0, voltage_to_percentage(2.50));    // Under-voltage
}
```

---

## 2. API Integration Tests (xUnit)

Run with: `dotnet test src/DogDoor.Api.Tests --filter "Category=Collar"`

### 2.1 Collar CRUD Tests

```csharp
[Trait("Category", "Collar")]
public class CollarControllerTests : IClassFixture<CustomWebAppFactory>
{
    [Fact]
    public async Task CreateCollar_ReturnsSharedSecret()
    {
        // Arrange
        var animal = await CreateTestAnimal();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/collars",
            new { animalId = animal.Id, name = "Test Collar" });

        // Assert
        response.EnsureSuccessStatusCode();
        var collar = await response.Content.ReadFromJsonAsync<CollarPairingDto>();
        Assert.NotNull(collar.SharedSecret);
        Assert.Equal(64, collar.SharedSecret.Length);  // 256-bit hex
        Assert.Equal(32, collar.CollarId.Length);       // 128-bit hex
        Assert.Equal(6, collar.PairingCode.Length);     // 6-digit code
    }

    [Fact]
    public async Task CreateCollar_SharedSecretNotReturnedOnGet()
    {
        // Arrange: Create collar (returns secret)
        var createResponse = await CreateTestCollar();

        // Act: Get collar (should NOT return secret)
        var getResponse = await _client.GetFromJsonAsync<CollarDto>(
            $"/api/v1/collars/{createResponse.Id}");

        // Assert: No secret field in response
        Assert.Null(getResponse.SharedSecret);  // Field doesn't exist on CollarDto
    }

    [Fact]
    public async Task ListCollars_ReturnsOnlyUsersCollars()
    {
        // Arrange: Create collars for two different users
        // Act: List as user 1
        // Assert: Only user 1's collars returned
    }

    [Fact]
    public async Task DeleteCollar_DeactivatesAndRemovesSecret()
    {
        // Arrange
        var collar = await CreateTestCollar();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/collars/{collar.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        // Verify collar is deactivated in DB
    }
}
```

### 2.2 NFC Verification Tests

```csharp
[Trait("Category", "Collar")]
public class CollarVerifyTests : IClassFixture<CustomWebAppFactory>
{
    [Fact]
    public async Task Verify_WithValidHmac_ReturnsAnimalInfo()
    {
        // Arrange
        var collar = await CreateTestCollarWithKnownSecret("test-secret-256-bit-hex");
        var nonce = GenerateRandomNonce();
        var doorId = GenerateTestDoorId();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var hmac = ComputeHmac("test-secret-256-bit-hex", nonce, doorId, timestamp);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/collars/{collar.Id}/verify",
            new {
                apiKey = TestApiKey,
                collarId = collar.CollarId,
                nonce = Convert.ToBase64String(nonce),
                doorId = Convert.ToBase64String(doorId),
                timestamp = timestamp,
                hmacResponse = Convert.ToBase64String(hmac)
            });

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<VerifyResultDto>();
        Assert.True(result.Verified);
        Assert.Equal(collar.AnimalId, result.AnimalId);
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task Verify_WithInvalidHmac_ReturnsFalse()
    {
        // Same as above but with wrong HMAC
        // Assert: verified=false, reason="hmac_mismatch"
    }

    [Fact]
    public async Task Verify_WithExpiredTimestamp_ReturnsFalse()
    {
        // Timestamp 60 seconds ago (> 30s window)
        // Assert: verified=false, reason="timestamp_expired"
    }

    [Fact]
    public async Task Verify_WithUnknownCollarId_Returns404()
    {
        // Non-existent collar ID
    }

    [Fact]
    public async Task Verify_WithDeactivatedCollar_ReturnsFalse()
    {
        // Collar with isActive=false
        // Assert: verified=false, reason="collar_deactivated"
    }
}
```

### 2.3 Location Upload Tests

```csharp
[Trait("Category", "Collar")]
public class LocationControllerTests : IClassFixture<CustomWebAppFactory>
{
    [Fact]
    public async Task BatchUpload_AcceptsValidPoints()
    {
        var collar = await CreateTestCollar();
        var points = GenerateTestLocationPoints(50);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/collars/{collar.Id}/locations",
            new { apiKey = TestApiKey, points });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UploadResultDto>();
        Assert.Equal(50, result.Accepted);
        Assert.Equal(0, result.Rejected);
    }

    [Fact]
    public async Task BatchUpload_RejectsStalePoints()
    {
        // Points with timestamps > 24h old
        // Assert: rejected count > 0
    }

    [Fact]
    public async Task BatchUpload_RejectsOverSizeLimit()
    {
        // > 1000 points in one request
        // Assert: 400 Bad Request
    }

    [Fact]
    public async Task QueryLocations_ReturnsDownsampled()
    {
        // Upload 1000 points, query with maxPoints=100
        // Assert: <= 100 points returned, shape preserved
    }

    [Fact]
    public async Task QueryLocations_ReturnsGeoJson()
    {
        // Query with format=geojson
        // Assert: valid GeoJSON FeatureCollection with LineString
    }

    [Fact]
    public async Task CurrentLocation_ReturnsLatestPoint()
    {
        // Upload multiple points with different timestamps
        // Assert: returns the most recent one
    }
}
```

### 2.4 Geofence Tests

```csharp
[Trait("Category", "Collar")]
public class GeofenceControllerTests : IClassFixture<CustomWebAppFactory>
{
    [Fact]
    public async Task CreatePolygonFence_ReturnsWithArea()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/geofences", new {
            name = "Test Yard",
            type = "polygon",
            action = "allow",
            buzzerPattern = "short",
            isEnabled = true,
            boundary = new {
                type = "Polygon",
                coordinates = new[] { new[] {
                    new[] {-112.074, 33.449},
                    new[] {-112.073, 33.449},
                    new[] {-112.073, 33.448},
                    new[] {-112.074, 33.448},
                    new[] {-112.074, 33.449}
                }}
            },
            collarIds = Array.Empty<int>()
        });

        response.EnsureSuccessStatusCode();
        var fence = await response.Content.ReadFromJsonAsync<GeofenceDto>();
        Assert.Equal("polygon", fence.Type);
        Assert.True(fence.AreaM2 > 0);
        Assert.Equal(1, fence.Version);
    }

    [Fact]
    public async Task CreateCircleFence_WithRadiusExtension()
    {
        // Circle fence with custom radius field
    }

    [Fact]
    public async Task UpdateFence_IncrementsVersion()
    {
        // Create fence (version=1), update boundary (version=2)
    }

    [Fact]
    public async Task SyncEndpoint_Returns304WhenUpToDate()
    {
        // Request with sinceVersion=current → 304 Not Modified
    }

    [Fact]
    public async Task SyncEndpoint_ReturnsNewFencesWhenOutdated()
    {
        // Request with sinceVersion=old → 200 with all fences
    }

    [Fact]
    public async Task DeleteFence_IncrementsSetVersion()
    {
        // Deleting a fence should increment the fence set version
        // so collars remove it on next sync
    }
}
```

### 2.5 Fused Access Request Tests

```csharp
[Trait("Category", "Collar")]
public class FusedAccessTests : IClassFixture<CustomWebAppFactory>
{
    [Fact]
    public async Task AccessRequest_CameraAndCollarAgree_BoostsConfidence()
    {
        // Camera identifies animal X, collar also identifies animal X
        // Assert: confidence > camera_only_confidence
        // Assert: identificationMethod = "both"
    }

    [Fact]
    public async Task AccessRequest_CameraAndCollarDisagree_ReducesConfidence()
    {
        // Camera identifies animal X, collar identifies animal Y
        // Assert: confidence < camera_only_confidence
        // Assert: allowed = false
    }

    [Fact]
    public async Task AccessRequest_CollarOnlyNoCamera_UsesCollarConfidence()
    {
        // Camera doesn't match any animal, collar verified
        // Assert: confidence = 0.95
        // Assert: identificationMethod = "collar"
    }

    [Fact]
    public async Task AccessRequest_NoCollar_ExistingBehavior()
    {
        // No collar fields in request
        // Assert: same as before collar feature
        // Assert: identificationMethod = "camera"
    }

    [Fact]
    public async Task AccessRequest_InvalidCollarHmac_FallsBackToCamera()
    {
        // Collar HMAC invalid, camera match present
        // Assert: collarConfidence = 0
        // Assert: identificationMethod = "camera"
    }
}
```

---

## 3. SPA Component Tests (Vitest + React Testing Library)

### 3.1 CollarCard Component

```typescript
describe('CollarCard', () => {
    it('renders collar name and animal', () => {
        render(<CollarCard collar={mockCollar} />);
        expect(screen.getByText("Luna's Collar")).toBeInTheDocument();
        expect(screen.getByText("Luna")).toBeInTheDocument();
    });

    it('shows battery indicator with color coding', () => {
        const lowBattery = { ...mockCollar, batteryLevel: 10 };
        render(<CollarCard collar={lowBattery} />);
        expect(screen.getByText('10%')).toHaveClass('battery--critical');
    });

    it('shows offline indicator when lastSeen > 1 hour', () => {
        const offline = { ...mockCollar, lastSeenAt: '2026-02-28T12:00:00Z' };
        render(<CollarCard collar={offline} />);
        expect(screen.getByText(/offline/i)).toBeInTheDocument();
    });
});
```

### 3.2 GeofenceEditor Component

```typescript
describe('GeofenceEditor', () => {
    it('renders map with existing fences', () => {
        render(<GeofenceEditor fences={mockFences} />);
        // Verify Leaflet map is rendered
        // Verify fence polygons are drawn
    });

    it('allows drawing new polygon fence', async () => {
        const onSave = vi.fn();
        render(<GeofenceEditor fences={[]} onSave={onSave} />);
        // Simulate draw interaction
        // Assert onSave called with GeoJSON polygon
    });

    it('color-codes allow vs deny fences', () => {
        render(<GeofenceEditor fences={[allowFence, denyFence]} />);
        // Verify allow fence has green styling
        // Verify deny fence has red styling
    });
});
```

---

## 4. End-to-End System Tests

### 4.1 Hardware-in-the-Loop Tests

These require physical hardware (2x PN532, 1x ESP32-S3, 1x ESP32-CAM):

| Test | Setup | Expected |
|------|-------|----------|
| NFC handshake happy path | Collar + door PN532s on bench | AUTH_OK in < 50ms |
| NFC handshake invalid secret | Collar with wrong secret | AUTH_FAILED, logged |
| NFC range test | Move collar away from door | Detection drops at ~4cm |
| BLE proximity detection | Collar advertising, door scanning | RSSI threshold triggers NFC |
| WiFi upload under load | Collar uploads 100 points | All points stored in DB |
| GPS tracking accuracy | Collar outdoors with clear sky | Fix accuracy < 3m |
| Deep sleep current | Collar in sleep mode, bench multimeter | < 30uA measured |
| OTA update | Upload new firmware via API | Collar updates and reboots |
| Geofence breach buzz | Define fence, move collar outside | Buzzer sounds within 2s |

### 4.2 Integration Scenario Tests

| Scenario | Steps | Expected Outcome |
|----------|-------|-----------------|
| Dog approaches door with collar | 1. Collar BLE detected → 2. NFC handshake → 3. Camera capture → 4. API fused decision | Access granted with method="both", boosted confidence |
| Dog approaches without collar | 1. No BLE detected → 2. Camera capture → 3. API camera-only decision | Access granted with method="camera", normal confidence |
| Unknown dog with stolen collar | 1. Collar NFC OK → 2. Camera doesn't match collar's animal | Access denied, disagreement alert |
| Dog enters restricted zone | 1. GPS fix in deny fence → 2. Buzzer sounds → 3. WiFi uploads breach | Owner gets push/SMS notification |
| Collar battery dies during tracking | 1. GPS tracking → 2. Battery < 5% → 3. Enter low power | BLE advertising continues for find-my-dog |
| Collar goes offline | 1. Collar loses WiFi → 2. > 1 hour no check-in | API sends collar offline notification |

---

## 5. Performance & Load Tests

### 5.1 API Load Test Scenarios

```
Tool: k6 or Artillery

Scenario 1: Location upload burst
  - 10 collars uploading 100 points each simultaneously
  - Expected: < 200ms p95 response time
  - Expected: 0% error rate

Scenario 2: Map tile loading
  - 5 concurrent users loading map (200 tiles each)
  - Expected: < 100ms p95 for cached tiles
  - Expected: < 2s p95 for uncached tiles

Scenario 3: Live location polling
  - 10 users polling every 5 seconds
  - Expected: < 50ms p95 response time

Scenario 4: Geofence sync
  - 20 collars syncing simultaneously
  - Expected: < 100ms p95 response time
```

### 5.2 Collar Firmware Stress Tests

```
Test 1: Continuous GPS for 4 hours
  - Verify no memory leaks (heap never drops below 50KB free)
  - Verify no watchdog resets
  - Verify battery discharge matches estimate

Test 2: Rapid WiFi reconnect
  - Toggle WiFi availability every 30 seconds for 1 hour
  - Verify no point loss (all buffered and eventually uploaded)
  - Verify no memory leaks from HTTP client

Test 3: NFC rapid auth
  - 100 consecutive NFC handshakes
  - Verify all succeed with < 50ms latency
  - Verify no memory leaks from PN532 driver

Test 4: Maximum geofences
  - Load 20 fences with 32 vertices each
  - Verify evaluation completes in < 1ms per GPS fix
  - Verify NVS storage doesn't overflow
```

---

## 6. Test Environment

### 6.1 CI Pipeline

```yaml
# Required test gates before merge:
- Collar firmware build: pio run -e collar (compile check)
- Collar firmware tests: pio test -e native (unit tests)
- API tests: dotnet test --filter "Category=Collar" (integration tests)
- SPA tests: npm run test (component tests)
- Helm validation: helm template with collar values
```

### 6.2 Hardware Test Bench

```
┌─────────────────────────────────────────────────────────┐
│  Test Bench                                              │
│                                                          │
│  ┌─────────────┐    NFC     ┌─────────────┐             │
│  │ Collar PCB  │◄──~4cm───►│ Door Unit   │             │
│  │ (ESP32-S3)  │           │ (ESP32-CAM) │             │
│  │ + PN532     │    BLE     │ + PN532     │             │
│  │ + GPS sim   │◄──────────►│             │             │
│  │ + LSM6DSO   │           │             │             │
│  └──────┬──────┘           └──────┬──────┘             │
│         │ USB                      │ USB                 │
│         ▼                          ▼                     │
│  ┌──────────────────────────────────────┐               │
│  │  Dev Laptop                          │               │
│  │  - PlatformIO (flash + debug)        │               │
│  │  - Docker (API + PostgreSQL)         │               │
│  │  - Test runner (xUnit + Vitest)      │               │
│  └──────────────────────────────────────┘               │
│                                                          │
│  Optional: GPS simulator (u-blox u-center)               │
│  for repeatable location tests                           │
└─────────────────────────────────────────────────────────┘
```
