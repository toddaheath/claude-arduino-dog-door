#include "power_manager.h"
#include "config.h"

#ifndef UNIT_TEST
#include <Arduino.h>
#endif

static float smoothed_voltage = 0;
static float prev_voltage = 0;
static const int ADC_SAMPLES = 16;

static float read_raw_voltage() {
#ifndef UNIT_TEST
    uint32_t sum = 0;
    for (int i = 0; i < ADC_SAMPLES; i++) {
        sum += analogRead(PIN_BATTERY_ADC);
        delayMicroseconds(100);
    }
    float adc_avg = (float)sum / ADC_SAMPLES;
    float voltage = (adc_avg / 4095.0f) * 3.3f * BATTERY_VOLTAGE_DIVIDER;
    return voltage;
#else
    return smoothed_voltage;  // In tests, use set value directly
#endif
}

void power_init() {
#ifndef UNIT_TEST
    analogReadResolution(12);
    analogSetAttenuation(ADC_11db);
#endif
    smoothed_voltage = read_raw_voltage();
    prev_voltage = smoothed_voltage;
}

float power_get_voltage() {
    float raw = read_raw_voltage();
    smoothed_voltage = smoothed_voltage * 0.9f + raw * 0.1f;
    return smoothed_voltage;
}

// Piecewise linear LiPo discharge curve approximation
// 4.20V=100%, 4.10V=90%, 3.95V=70%, 3.80V=40%, 3.70V=20%, 3.50V=5%, 3.20V=0%
float voltage_to_percentage(float voltage_v) {
    float v_mv = voltage_v * 1000.0f;

    if (v_mv >= BATTERY_FULL_MV)  return 100.0f;
    if (v_mv <= BATTERY_EMPTY_MV) return 0.0f;

    float pct;
    if (v_mv > 4100)      pct = 90.0f + (v_mv - 4100.0f) / (4200.0f - 4100.0f) * 10.0f;
    else if (v_mv > 3950) pct = 70.0f + (v_mv - 3950.0f) / (4100.0f - 3950.0f) * 20.0f;
    else if (v_mv > 3800) pct = 40.0f + (v_mv - 3800.0f) / (3950.0f - 3800.0f) * 30.0f;
    else if (v_mv > 3700) pct = 20.0f + (v_mv - 3700.0f) / (3800.0f - 3700.0f) * 20.0f;
    else if (v_mv > 3500) pct = 5.0f  + (v_mv - 3500.0f) / (3700.0f - 3500.0f) * 15.0f;
    else                  pct = (v_mv - 3200.0f) / (3500.0f - 3200.0f) * 5.0f;

    if (pct < 0.0f) pct = 0.0f;
    if (pct > 100.0f) pct = 100.0f;
    return pct;
}

float power_get_percentage() {
    float v = power_get_voltage();
    return voltage_to_percentage(v);
}

bool power_is_charging() {
    float current = power_get_voltage();
    bool rising = (current - prev_voltage) > 0.01f;
    prev_voltage = current;
    return rising && current > 4.0f;
}

bool power_is_low() {
    return power_get_percentage() < BATTERY_LOW_PCT;
}

bool power_is_critical() {
    return power_get_percentage() < BATTERY_CRITICAL_PCT;
}

#ifdef UNIT_TEST
// Test helpers
void power_set_voltage_for_test(float voltage) {
    smoothed_voltage = voltage;
    prev_voltage = voltage;
}
#endif
