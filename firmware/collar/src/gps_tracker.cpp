#include "gps_tracker.h"
#include "config.h"

#ifndef UNIT_TEST
#include <Arduino.h>
#include <SparkFun_u-blox_GNSS_v3.h>

static SFE_UBLOX_GNSS gnss;
static bool gps_powered = false;
static uint8_t current_rate = 1;

void gps_power_on() {
    if (gps_powered) return;

    digitalWrite(PIN_GPS_EN, HIGH);
    delay(500);  // GPS module startup time

    Serial1.begin(GPS_BAUD_RATE, SERIAL_8N1, PIN_GPS_RX, PIN_GPS_TX);

    if (!gnss.begin(Serial1)) {
        Serial.println("[GPS] u-blox not detected, check wiring");
        return;
    }

    gnss.setUART1Output(COM_TYPE_UBX);
    gnss.setNavigationFrequency(1);
    gnss.setAutoPVT(true);
    gnss.setDynamicModel(DYN_MODEL_PEDESTRIAN);
    gnss.setSBAS(true);
    gnss.powerSaveMode(true);

    gps_powered = true;
    Serial.println("[GPS] Powered on, waiting for fix...");
}

void gps_power_off() {
    if (!gps_powered) return;

    gnss.powerOff(0);
    digitalWrite(PIN_GPS_EN, LOW);
    Serial1.end();

    gps_powered = false;
    Serial.println("[GPS] Powered off");
}

void gps_set_rate(uint8_t rate_hz) {
    if (!gps_powered || rate_hz == current_rate) return;

    if (rate_hz == 0) {
        gnss.setMeasurementRate(10000);  // 10s = 0.1 Hz
    } else {
        gnss.setNavigationFrequency(rate_hz);
    }
    current_rate = rate_hz;
}

bool gps_get_fix(GpsFix* fix) {
    if (!gps_powered) return false;
    if (!gnss.getPVT()) return false;

    fix->fix_type = gnss.getFixType();
    if (fix->fix_type < 2) return false;

    fix->lat = gnss.getLatitude() / 1e7;
    fix->lng = gnss.getLongitude() / 1e7;
    fix->altitude = gnss.getAltitudeMSL() / 1000.0f;
    fix->accuracy = gnss.getHorizontalAccEst() / 1000.0f;
    fix->speed = gnss.getGroundSpeed() / 1000.0f;
    fix->heading = gnss.getHeading() / 1e5;
    fix->satellites = gnss.getSIV();
    fix->hdop = gnss.getPDOP() / 100.0f;
    fix->age_ms = 0;

    struct tm t;
    t.tm_year = gnss.getYear() - 1900;
    t.tm_mon = gnss.getMonth() - 1;
    t.tm_mday = gnss.getDay();
    t.tm_hour = gnss.getHour();
    t.tm_min = gnss.getMinute();
    t.tm_sec = gnss.getSecond();
    t.tm_isdst = 0;
    fix->timestamp = mktime(&t);

    return true;
}

bool gps_has_fix() {
    return gps_powered && gnss.getFixType() >= 2;
}

#endif  // !UNIT_TEST

// Quality check is testable without hardware
bool gps_quality_ok(const GpsFix* fix) {
    if (fix->fix_type < 2) return false;
    if (fix->hdop > GPS_HDOP_THRESHOLD) return false;
    if (fix->satellites < GPS_MIN_SATELLITES) return false;
    if (fix->age_ms > 30000) return false;
    if (fix->accuracy > 15.0f) return false;
    return true;
}
