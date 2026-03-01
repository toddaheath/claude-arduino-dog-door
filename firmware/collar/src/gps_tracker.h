#pragma once

#include <stdint.h>
#include <stdbool.h>

typedef struct {
    double   lat;
    double   lng;
    float    altitude;
    float    accuracy;    // Horizontal accuracy (meters)
    float    speed;       // Ground speed (m/s)
    float    heading;     // Course over ground (degrees)
    uint8_t  satellites;
    float    hdop;
    uint8_t  fix_type;    // 0=none, 2=2D, 3=3D
    uint32_t age_ms;      // Time since this fix was computed
    uint32_t timestamp;   // Unix epoch
} GpsFix;

void gps_power_on();
void gps_power_off();
void gps_set_rate(uint8_t rate_hz);
bool gps_get_fix(GpsFix* fix);
bool gps_quality_ok(const GpsFix* fix);
bool gps_has_fix();
