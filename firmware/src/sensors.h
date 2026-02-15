#ifndef SENSORS_H
#define SENSORS_H

// Dual-board architecture: each ESP32-CAM board runs its own set of sensors.
// The board's THIS_SIDE config determines which side of the door it monitors.

#include <Arduino.h>

void sensors_init();

// Radar: returns true if motion detected
bool radar_detected();

// Ultrasonic: returns distance in cm, -1 on error
float ultrasonic_distance_cm();

// IR break beam: returns true if beam is broken (something in doorway)
bool ir_beam_broken();

// Reed switch: returns true if door is in closed position
bool door_is_closed();

#endif // SENSORS_H
