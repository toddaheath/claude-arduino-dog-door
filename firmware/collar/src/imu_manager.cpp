#include "imu_manager.h"
#include "config.h"

#ifndef UNIT_TEST
#include <Arduino.h>
#include <LSM6DSOSensor.h>

static LSM6DSOSensor imu(&Wire, I2C_ADDR_LSM6DSO);
#endif

static ActivityData activity = { MOTION_NONE, 0, 0.0f, 0 };
static bool is_stationary = true;
static uint32_t stationary_since = 0;

const char* motion_type_name(MotionType type) {
    switch (type) {
        case MOTION_NONE:    return "none";
        case MOTION_BRIEF:   return "brief";
        case MOTION_WALKING: return "walking";
        case MOTION_RUNNING: return "running";
        default:             return "unknown";
    }
}

#ifndef UNIT_TEST

void imu_init() {
    imu.begin();
    imu.Enable_X();
    imu.Enable_G();

    imu.Set_X_ODR(52.0f);
    imu.Set_G_ODR(52.0f);
    imu.Set_X_FS(4);
    imu.Set_G_FS(500);
    imu.Enable_Pedometer();

    activity.steps_today = 0;
    activity.distance_m_today = 0.0f;
    activity.active_minutes_today = 0;

    Serial.println("[IMU] LSM6DSO initialized");
}

MotionType imu_classify_motion(uint32_t sample_duration_ms) {
    uint32_t start = millis();
    float max_accel = 0;
    int sample_count = 0;
    float accel_sum = 0;

    while (millis() - start < sample_duration_ms) {
        int32_t accel[3];
        if (imu.Get_X_Axes(accel) == LSM6DSO_OK) {
            float magnitude = sqrt(
                (float)(accel[0] * accel[0]) +
                (float)(accel[1] * accel[1]) +
                (float)(accel[2] * accel[2])
            ) / 1000.0f;
            float dynamic = fabs(magnitude - 1.0f);
            if (dynamic > max_accel) max_accel = dynamic;
            accel_sum += dynamic;
            sample_count++;
        }
        delay(20);
    }

    if (sample_count == 0) return MOTION_NONE;

    float avg_dynamic = accel_sum / sample_count;

    if (avg_dynamic < 0.05f) return MOTION_NONE;
    if (avg_dynamic < 0.1f)  return MOTION_BRIEF;
    if (avg_dynamic < 0.5f) {
        is_stationary = false;
        return MOTION_WALKING;
    }
    is_stationary = false;
    return MOTION_RUNNING;
}

bool imu_is_stationary() {
    int32_t accel[3];
    if (imu.Get_X_Axes(accel) != LSM6DSO_OK) return is_stationary;

    float magnitude = sqrt(
        (float)(accel[0] * accel[0]) +
        (float)(accel[1] * accel[1]) +
        (float)(accel[2] * accel[2])
    ) / 1000.0f;
    float dynamic = fabs(magnitude - 1.0f);

    if (dynamic < 0.08f) {
        if (!is_stationary) {
            stationary_since = millis();
            is_stationary = true;
        }
    } else {
        is_stationary = false;
        stationary_since = millis();

        uint16_t steps;
        if (imu.Get_Step_Count(&steps) == LSM6DSO_OK) {
            if (steps > activity.steps_today) {
                uint32_t new_steps = steps - activity.steps_today;
                activity.steps_today = steps;
                activity.distance_m_today += new_steps * 0.5f;
            }
        }

        static uint32_t last_active_minute = 0;
        uint32_t current_minute = millis() / 60000;
        if (current_minute != last_active_minute) {
            activity.active_minutes_today++;
            last_active_minute = current_minute;
        }
    }

    return is_stationary;
}

void imu_configure_wakeup(uint16_t threshold_mg) {
    imu.Enable_Wake_Up_Detection(LSM6DSO_INT1_PIN);
    imu.Set_Wake_Up_Threshold(threshold_mg);
    imu.Set_Wake_Up_Duration(2);
    imu.Set_X_ODR(12.5f);

    Serial.printf("[IMU] Wake-up configured: %dmg threshold\n", threshold_mg);
}

ActivityData imu_get_activity() {
    int32_t accel[3];
    if (imu.Get_X_Axes(accel) == LSM6DSO_OK) {
        float magnitude = sqrt(
            (float)(accel[0] * accel[0]) +
            (float)(accel[1] * accel[1]) +
            (float)(accel[2] * accel[2])
        ) / 1000.0f;
        float dynamic = fabs(magnitude - 1.0f);

        if (dynamic < 0.05f)      activity.type = MOTION_NONE;
        else if (dynamic < 0.3f)  activity.type = MOTION_WALKING;
        else                      activity.type = MOTION_RUNNING;
    }
    return activity;
}

void imu_reset_daily_stats() {
    activity.steps_today = 0;
    activity.distance_m_today = 0.0f;
    activity.active_minutes_today = 0;
    imu.Reset_Step_Count();
}

#endif  // !UNIT_TEST
