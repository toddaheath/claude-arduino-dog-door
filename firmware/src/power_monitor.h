#pragma once

void power_monitor_init();
void power_monitor_update(const char* apiKey, const char* baseUrl);
float power_monitor_read_voltage();
int power_monitor_battery_percent();
