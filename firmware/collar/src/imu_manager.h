#pragma once

#include <stdint.h>
#include <stdbool.h>

typedef enum {
    MOTION_NONE,
    MOTION_BRIEF,     // < 2s, likely a jostle
    MOTION_WALKING,   // Rhythmic, moderate acceleration
    MOTION_RUNNING,   // High acceleration, fast cadence
    MOTION_UNKNOWN    // Sustained but unclassified
} MotionType;

typedef struct {
    MotionType type;
    uint32_t   steps_today;
    float      distance_m_today;
    uint16_t   active_minutes_today;
} ActivityData;

void imu_init();
MotionType imu_classify_motion(uint32_t sample_duration_ms);
bool imu_is_stationary();
void imu_configure_wakeup(uint16_t threshold_mg);
ActivityData imu_get_activity();
void imu_reset_daily_stats();
const char* motion_type_name(MotionType type);
