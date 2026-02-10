#ifndef DOOR_CONTROL_H
#define DOOR_CONTROL_H

#include <Arduino.h>

void door_init();

// Open the door (with safety checks)
bool door_open();

// Close the door (with safety checks)
bool door_close();

// Emergency stop
void door_stop();

// Check if door is currently open
bool door_is_open();

// Set status LEDs
void led_allow();
void led_deny();
void led_processing();
void led_off();

#endif // DOOR_CONTROL_H
