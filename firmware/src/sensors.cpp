#include "sensors.h"
#include "config.h"

void sensors_init() {
    pinMode(PIN_RADAR, INPUT);
    pinMode(PIN_ULTRASONIC_TRIG, OUTPUT);
    pinMode(PIN_ULTRASONIC_ECHO, INPUT);
    pinMode(PIN_IR_BEAM, INPUT_PULLUP);
    pinMode(PIN_REED_SWITCH, INPUT_PULLUP);
    pinMode(PIN_LED_GREEN, OUTPUT);
    pinMode(PIN_LED_RED, OUTPUT);

    digitalWrite(PIN_ULTRASONIC_TRIG, LOW);
    digitalWrite(PIN_LED_GREEN, LOW);
    digitalWrite(PIN_LED_RED, LOW);
}

bool radar_detected() {
    return digitalRead(PIN_RADAR) == HIGH;
}

float ultrasonic_distance_cm() {
    // Send trigger pulse
    digitalWrite(PIN_ULTRASONIC_TRIG, LOW);
    delayMicroseconds(2);
    digitalWrite(PIN_ULTRASONIC_TRIG, HIGH);
    delayMicroseconds(10);
    digitalWrite(PIN_ULTRASONIC_TRIG, LOW);

    // Measure echo duration
    long duration = pulseIn(PIN_ULTRASONIC_ECHO, HIGH, 30000); // 30ms timeout

    if (duration == 0) {
        return -1.0f; // No echo received
    }

    // Calculate distance: speed of sound = 343 m/s = 0.0343 cm/us
    // Distance = (duration * 0.0343) / 2
    float distance = (duration * 0.0343f) / 2.0f;

    if (distance > ULTRASONIC_MAX_DISTANCE_CM) {
        return -1.0f;
    }

    return distance;
}

bool ir_beam_broken() {
    // IR beam sensor: LOW when beam is broken (active low)
    return digitalRead(PIN_IR_BEAM) == LOW;
}

bool door_is_closed() {
    // Reed switch: LOW when magnet is near (door closed)
    return digitalRead(PIN_REED_SWITCH) == LOW;
}
