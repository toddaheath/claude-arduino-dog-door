#pragma once

#include "gps_tracker.h"
#include "imu_manager.h"

void ble_init();
void ble_start_advertising();
void ble_stop_advertising();
void ble_process();

void ble_update_location(const GpsFix& fix);
void ble_update_battery(float percentage, bool charging);
void ble_update_activity(const ActivityData& activity);

int  ble_check_door_rssi();
