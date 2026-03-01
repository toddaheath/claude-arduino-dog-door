# Collar Power Optimization Deep-Dive

## Overview

Detailed analysis of power consumption, battery life optimization strategies, and techniques to maximize runtime on a 500mAh LiPo.

---

## 1. Component Power Profiles

### 1.1 ESP32-S3-MINI-1

| Mode | Current | Notes |
|------|---------|-------|
| Active (WiFi TX) | 310 mA | Peak during WiFi upload burst |
| Active (WiFi RX) | 100 mA | Receiving data |
| Active (BLE TX) | 130 mA | BLE advertising at 0 dBm |
| Active (BLE RX) | 100 mA | BLE scanning |
| Active (CPU only) | 40 mA | Processing, no radio |
| Modem Sleep | 20 mA | CPU active, WiFi/BLE off |
| Light Sleep | 0.8 mA | CPU paused, wake on timer/GPIO |
| Deep Sleep | 10 uA | RTC + ULP only |

### 1.2 u-blox MAX-M10S GPS

| Mode | Current | Notes |
|------|---------|-------|
| Acquisition (cold start) | 22 mA | First fix, searching for satellites |
| Tracking (1Hz) | 5.5 mA | Continuous navigation |
| Power Save Mode (1Hz) | 2.2 mA | Cyclic tracking (ON 200ms, OFF 800ms) |
| Power Save Mode (0.1Hz) | 0.5 mA | One fix every 10 seconds |
| Backup mode | 15 uA | RTC running, hot start on wake |
| Off (power gate) | 0 uA | MOSFET cuts power completely |

### 1.3 PN532 NFC Module

| Mode | Current | Notes |
|------|---------|-------|
| Active (tag emulation) | 120 mA | RF field active, waiting for reader |
| Active (reader mode) | 100 mA | Scanning for tags |
| Low power polling | 0.5 mA | Periodic wake-and-check |
| Standby | 80 uA | I2C active, ready for commands |
| Power down | 10 uA | Minimal state retention |

### 1.4 LSM6DSO IMU

| Mode | Current | Notes |
|------|---------|-------|
| High Performance (416Hz) | 0.55 mA | Full accuracy |
| Normal (52Hz) | 0.17 mA | Step detection, motion classification |
| Low Power (12.5Hz) | 0.045 mA | Wake-on-motion detection |
| Power Down | 3 uA | Only I2C interface active |

### 1.5 Peripherals

| Component | Active | Standby |
|-----------|--------|---------|
| WS2812B RGB LED | 20 mA (white) | 1 mA (idle, internal regulator) |
| Piezo buzzer | 5 mA (during tone) | 0 mA |
| TP4056 (not charging) | 0.5 mA | — |
| TPS63001 regulator | 0.02 mA (quiescent) | — |
| Voltage divider (100k/100k) | 0.017 mA | — |

---

## 2. Power State Analysis

### 2.1 Deep Sleep (Overnight / Inactive)

```
Component          State                Current
─────────────────────────────────────────────────
ESP32-S3           Deep sleep           10 uA
MAX-M10S GPS       Off (power gated)    0 uA
PN532 NFC          Power down           10 uA
LSM6DSO IMU        Low Power (12.5Hz)   45 uA
                   Wake-on-motion INT
TPS63001           Quiescent            20 uA
Voltage divider    Always on            17 uA
WS2812B            Idle (data low)      ~1000 uA (*)
─────────────────────────────────────────────────
TOTAL                                   ~1.1 mA

(*) WS2812B has a 1mA idle current even with data line low.
    Optimization: Add a MOSFET to cut WS2812B power in deep sleep.
    With MOSFET: total drops to ~0.1 mA = 100 uA
```

**With LED power gate optimization:**
- Sleep current: 100 uA
- 500 mAh / 0.1 mA = 5,000 hours = **208 days**

**Without LED power gate:**
- Sleep current: 1.1 mA
- 500 mAh / 1.1 mA = 454 hours = **19 days** (still acceptable)

**Recommendation:** Add a small N-channel MOSFET (Si2302) to gate the WS2812B VCC. One extra component, massive sleep current improvement.

### 2.2 GPS Tracking (Active Outdoor Time)

```
Typical active duty cycle (per second):

GPS tracking at 1Hz:
  MAX-M10S PSM:        2.2 mA (continuous)
  ESP32-S3 processing: 40 mA × 50ms = 2.0 mA average

BLE advertising (500ms interval):
  ESP32-S3 BLE TX:     130 mA × 2ms = 0.26 mA average

IMU at 52Hz:
  LSM6DSO:             0.17 mA (continuous)

Always on:
  Regulator + divider: 0.037 mA
  WS2812B idle:        1.0 mA
─────────────────────────────────────────────────
TOTAL (active tracking): ~5.7 mA average

Battery life at continuous tracking:
  500 mAh / 5.7 mA = 87 hours = 3.6 days
```

### 2.3 WiFi Upload Burst

```
WiFi burst every 30 seconds, lasting ~2 seconds:

During burst (2s):
  WiFi connect:        310 mA × 500ms = 155 mAh-equiv
  WiFi TX (POST):      310 mA × 1000ms = 310 mAh-equiv
  WiFi disconnect:     100 mA × 500ms = 50 mAh-equiv
  Total per burst:     ~515 mAh-equiv for 2s = ~0.286 mAh per burst

Per minute (2 bursts):
  0.286 mAh × 2 = 0.572 mAh/min

Per hour:
  0.572 × 60 = 34.3 mAh/hour (from WiFi alone)

WiFi contribution to average current:
  34.3 mAh/hour ÷ 1 hour = 34.3 mA average (DOMINANT consumer)
```

**This is the biggest power drain!** WiFi bursts dominate the power budget.

### 2.4 NFC Handshake (Brief, Infrequent)

```
NFC handshake (when near door):
  Duration: ~40ms
  PN532 active: 120 mA × 40ms = 0.0013 mAh per handshake

  PN532 standby while waiting: 80 uA (negligible)

  Per door visit: < 0.01 mAh (insignificant)
```

NFC is not a meaningful power consumer.

---

## 3. Daily Power Budget Scenarios

### 3.1 Active Yard Dog (4 hours outdoor, 20 hours sleep)

```
Phase         Duration  Avg Current  Energy Used
──────────────────────────────────────────────────
Deep sleep    20h       0.1 mA       2.0 mAh
GPS tracking  4h        5.7 mA       22.8 mAh
WiFi uploads  4h worth  34.3 mA      137.2 mAh (*)
NFC (2 door   ~80ms     120 mA       0.003 mAh
 visits)
──────────────────────────────────────────────────
DAILY TOTAL                          ~162 mAh

Battery life: 500 mAh / 162 mAh/day = 3.1 days

(*) WiFi bursts happen every 30s during active tracking = 480 bursts/day
```

**Problem:** 3.1 days is below the 5-day target. WiFi uploads are too frequent.

### 3.2 Optimized: Reduce WiFi Upload Frequency

**Change WiFi upload interval from 30s to 120s:**

```
WiFi bursts during 4h active: 4 × 3600 / 120 = 120 bursts
Energy: 120 × 0.286 mAh = 34.3 mAh

Phase         Duration  Avg Current  Energy Used
──────────────────────────────────────────────────
Deep sleep    20h       0.1 mA       2.0 mAh
GPS tracking  4h        5.7 mA       22.8 mAh
WiFi uploads  120 bursts             34.3 mAh
NFC           2 visits               0.003 mAh
──────────────────────────────────────────────────
DAILY TOTAL                          ~59 mAh

Battery life: 500 mAh / 59 mAh/day = 8.5 days ✓
```

### 3.3 Optimized: Adaptive WiFi Scheduling

Instead of fixed-interval WiFi bursts, upload only when:
1. Buffer has > 50 points (avoid tiny uploads)
2. Geofence breach event needs immediate delivery
3. WiFi RSSI is good (> -60 dBm, closer to AP = faster transfer = less energy)

```
Adaptive strategy for 4h outdoor time:
  GPS 1Hz × 4h = 14,400 points
  Points per upload batch: 200 (configurable)
  Uploads needed: 14,400 / 200 = 72 uploads
  Energy: 72 × 0.286 mAh = 20.6 mAh

Phase         Duration  Avg Current  Energy Used
──────────────────────────────────────────────────
Deep sleep    20h       0.1 mA       2.0 mAh
GPS tracking  4h        5.7 mA       22.8 mAh
WiFi uploads  72 bursts              20.6 mAh
NFC           2 visits               0.003 mAh
──────────────────────────────────────────────────
DAILY TOTAL                          ~45.4 mAh

Battery life: 500 mAh / 45.4 mAh/day = 11 days ✓✓
```

### 3.4 Indoor Dog (1 hour outdoor, 23 hours sleep)

```
Phase         Duration  Avg Current  Energy Used
──────────────────────────────────────────────────
Deep sleep    23h       0.1 mA       2.3 mAh
GPS tracking  1h        5.7 mA       5.7 mAh
WiFi uploads  18 bursts              5.1 mAh
NFC           4 visits               0.005 mAh
──────────────────────────────────────────────────
DAILY TOTAL                          ~13.1 mAh

Battery life: 500 mAh / 13.1 mAh/day = 38 days ✓✓✓
```

---

## 4. Optimization Techniques

### 4.1 GPS Power Save Mode

The u-blox MAX-M10S supports cyclic tracking:
- ON period: 200ms (enough for one fix)
- OFF period: varies based on desired rate

```c
void gps_set_power_save_mode(bool enable) {
    if (enable) {
        // Cyclic tracking: wake 200ms, sleep 800ms (1Hz effective)
        gnss.powerSaveMode(true);
        gnss.setMeasurementRate(1000);  // 1 Hz
    } else {
        gnss.powerSaveMode(false);
    }
}
```

Savings: 5.5 mA (continuous) → 2.2 mA (PSM) = **60% reduction in GPS current**

### 4.2 Adaptive GPS Rate

```c
void update_gps_rate() {
    static uint32_t stationary_start = 0;

    if (imu_is_stationary()) {
        if (stationary_start == 0) {
            stationary_start = millis();
        }

        uint32_t stationary_duration = millis() - stationary_start;

        if (stationary_duration > 300000) {
            // Stationary > 5 min: turn GPS off entirely
            gps_power_off();
        } else if (stationary_duration > 60000) {
            // Stationary > 1 min: slow to 0.1 Hz (one fix per 10s)
            gps_set_rate(0);  // 0 = 0.1Hz mode
        } else if (stationary_duration > 10000) {
            // Stationary > 10s: slow to 0.2 Hz
            gnss.setMeasurementRate(5000);
        }
    } else {
        stationary_start = 0;

        // Moving: determine speed from IMU
        float speed = imu_get_speed_estimate();
        if (speed > 3.0) {
            // Running: 2 Hz for accurate tracking
            gps_set_rate(2);
        } else {
            // Walking: 1 Hz
            gps_set_rate(1);
        }
    }
}
```

### 4.3 WiFi Connection Optimization

```c
// Cache WiFi connection parameters for fast reconnect
void wifi_fast_connect() {
    // If we have cached channel and BSSID, skip full scan
    if (cached_channel > 0 && cached_bssid_valid) {
        WiFi.begin(ssid, password, cached_channel, cached_bssid, true);
        // Fast connect: ~500ms (vs ~2-3s for full scan)
    } else {
        WiFi.begin(ssid, password);
        // Full scan: 2-3 seconds
    }
}

// After successful connect, cache the AP info:
void wifi_cache_ap_info() {
    cached_channel = WiFi.channel();
    memcpy(cached_bssid, WiFi.BSSID(), 6);
    cached_bssid_valid = true;
}
```

**Savings:** Fast reconnect saves ~1.5s × 310mA = 0.13 mAh per connection. Over 72 daily connections: **9.4 mAh saved**.

### 4.4 BLE Advertising Interval Optimization

```c
// When no phone/door is connected:
pAdvertising->setMinInterval(800);   // 500ms minimum
pAdvertising->setMaxInterval(1600);  // 1000ms maximum
// Current: ~0.05 mA average (very low)

// When door proximity detected (RSSI > -70):
pAdvertising->setMinInterval(160);   // 100ms (faster discovery)
pAdvertising->setMaxInterval(320);   // 200ms
// Current: ~0.26 mA average (acceptable for short duration)
```

### 4.5 NFC Power Gating

The PN532 draws 80 uA even in standby. Since NFC is only needed near the door:

```c
// PN532 is powered OFF by default
// Only powered ON when:
// 1. BLE scan detects door unit RSSI > -60 dBm (dog within ~2m of door)

void check_nfc_needed() {
    static bool nfc_powered = false;
    int door_rssi = ble_check_door_rssi();

    if (door_rssi > -60 && !nfc_powered) {
        nfc_power_on();     // Enable PN532 via I2C wakeup command
        nfc_powered = true;
    } else if (door_rssi <= -70 && nfc_powered) {
        nfc_power_off();    // Disable PN532
        nfc_powered = false;
    }
}
```

### 4.6 Dynamic CPU Frequency

```c
// Lower CPU frequency when not doing intensive work
void set_cpu_for_task(TaskType task) {
    switch (task) {
        case TASK_GPS_PARSE:
        case TASK_GEOFENCE_EVAL:
        case TASK_BLE_PROCESS:
            setCpuFrequencyMhz(80);   // 80 MHz: sufficient for light processing
            break;

        case TASK_WIFI_UPLOAD:
        case TASK_NFC_HANDSHAKE:
        case TASK_HMAC_COMPUTE:
            setCpuFrequencyMhz(240);  // 240 MHz: needed for crypto + networking
            break;

        case TASK_IDLE:
            setCpuFrequencyMhz(40);   // 40 MHz: minimal for BLE stack
            break;
    }
}
```

CPU at 80 MHz draws ~60% of 240 MHz current. CPU at 40 MHz draws ~40%.

---

## 5. Battery Life Summary Table

| Scenario | Daily Energy | Battery Life (500mAh) |
|----------|-------------|----------------------|
| Always tracking, WiFi every 30s | 162 mAh | 3.1 days |
| Active 4h, WiFi every 120s | 59 mAh | 8.5 days |
| Active 4h, adaptive WiFi | 45 mAh | 11 days |
| Active 1h (indoor dog) | 13 mAh | 38 days |
| Deep sleep only | 2.4 mAh | 208 days |

**Recommended default configuration:**
- WiFi upload: adaptive (batch of 200 points)
- GPS rate: adaptive (1Hz moving, 0.1Hz stationary, off after 5min idle)
- BLE: slow advertising (500ms) by default, fast (100ms) near door
- NFC: powered only when door proximity detected
- LED: powered off in sleep (MOSFET gate)
- CPU: 80 MHz default, 240 MHz for WiFi/crypto

**Expected battery life for typical yard dog:** **8-12 days**

---

## 6. Charging Behavior

### 6.1 TP4056 Charge Profile

```
USB-C input: 5V
TP4056 charge current: 500mA (set by PROG resistor: 2kΩ)
Charge time (0% → 100%): ~1.5 hours
Trickle charge cutoff: 4.2V
Protection: DW01 IC (overcharge, overdischarge, overcurrent, short)
```

### 6.2 Charging While Tracking

The collar can continue GPS tracking and BLE advertising while charging:
- USB-C provides 5V, regulated to 3.3V
- TP4056 charges battery from USB, powers system from battery
- Net charge rate: 500mA - ~6mA (tracking load) = 494mA
- Charge time while tracking: ~1.5 hours (negligible increase)

### 6.3 Charge Indication

```c
// LED behavior during charging:
// - Solid red: charging
// - Solid green: fully charged
// - Blinking red: charge error (temperature, overcurrent)

void update_charge_led() {
    if (tp4056_is_charging()) {
        led_set_color(255, 0, 0);  // Red
    } else if (power_get_voltage() > 4.15) {
        led_set_color(0, 255, 0);  // Green
    }
}
```

---

## 7. Battery Health Management

### 7.1 Cycle Count Tracking

```c
// Track charge cycles in NVS
void record_charge_cycle() {
    static float prev_percentage = 100;
    float current = power_get_percentage();

    // Detect charging edge (percentage going up after being < 90%)
    static bool was_low = false;
    if (current < 20) was_low = true;
    if (was_low && current > 95) {
        was_low = false;
        uint32_t cycles = nvs_read_u32("power", "cycles");
        nvs_write_u32("power", "cycles", cycles + 1);

        // LiPo rated for ~500 full cycles
        if (cycles > 400) {
            // Battery approaching end of life
            // Report to API for owner notification
        }
    }
}
```

### 7.2 Temperature Protection

```c
// ESP32-S3 has internal temperature sensor
float get_chip_temperature() {
    return temperatureRead();  // Built-in, approximate
}

void check_thermal_limits() {
    float temp = get_chip_temperature();

    if (temp > 60.0) {
        // Too hot (direct sun exposure, heavy processing)
        // Reduce power: drop to 80MHz, increase WiFi interval, dim LED
        setCpuFrequencyMhz(80);
        Serial.println("[POWER] Thermal throttle: CPU reduced to 80MHz");
    }

    if (temp > 70.0) {
        // Critical: stop all activity except BLE beacon
        gps_power_off();
        nfc_power_off();
        Serial.println("[POWER] Thermal emergency: non-essential peripherals off");
    }

    if (temp < -10.0) {
        // Cold weather: LiPo capacity reduced
        // Battery percentage estimate needs cold compensation
        // Actual capacity at -10°C: ~60-70% of rated
    }
}
```
