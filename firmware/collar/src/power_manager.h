#pragma once

#include <stdbool.h>

void  power_init();
float power_get_voltage();
float power_get_percentage();
bool  power_is_charging();
bool  power_is_low();
bool  power_is_critical();
