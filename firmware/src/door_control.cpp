#include "door_control.h"
#include "config.h"
#include "sensors.h"

static bool _door_open = false;

void door_init() {
    pinMode(PIN_MOTOR_IN1, OUTPUT);
    pinMode(PIN_MOTOR_IN2, OUTPUT);
    door_stop();
    _door_open = !door_is_closed();
}

static void motor_forward() {
    digitalWrite(PIN_MOTOR_IN1, HIGH);
    digitalWrite(PIN_MOTOR_IN2, LOW);
}

static void motor_reverse() {
    digitalWrite(PIN_MOTOR_IN1, LOW);
    digitalWrite(PIN_MOTOR_IN2, HIGH);
}

void door_stop() {
    digitalWrite(PIN_MOTOR_IN1, LOW);
    digitalWrite(PIN_MOTOR_IN2, LOW);
}

bool door_open() {
    if (_door_open) {
        Serial.println("Door already open");
        return true;
    }

    Serial.println("Opening door...");
    led_allow();
    motor_forward();

    unsigned long start = millis();
    while (millis() - start < DOOR_OPEN_TIME_MS) {
        // Safety: check IR beam during opening
        delay(DOOR_SAFETY_CHECK_INTERVAL_MS);
    }

    door_stop();
    _door_open = true;
    Serial.println("Door opened");
    return true;
}

bool door_close() {
    if (!_door_open) {
        Serial.println("Door already closed");
        return true;
    }

    Serial.println("Closing door...");

    // Safety interlock: don't close if IR beam is broken
    if (ir_beam_broken()) {
        Serial.println("IR beam broken - animal in doorway, aborting close");
        return false;
    }

    motor_reverse();

    unsigned long start = millis();
    while (millis() - start < DOOR_CLOSE_TIME_MS) {
        // Safety: if IR beam breaks during closing, stop immediately
        if (ir_beam_broken()) {
            door_stop();
            Serial.println("IR beam broken during close - emergency stop");
            // Reopen for safety
            door_open();
            return false;
        }
        delay(DOOR_SAFETY_CHECK_INTERVAL_MS);
    }

    door_stop();
    _door_open = false;
    led_off();
    Serial.println("Door closed");
    return true;
}

bool door_is_open() {
    return _door_open;
}

void led_allow() {
    digitalWrite(PIN_LED_GREEN, HIGH);
    digitalWrite(PIN_LED_RED, LOW);
}

void led_deny() {
    digitalWrite(PIN_LED_GREEN, LOW);
    digitalWrite(PIN_LED_RED, HIGH);
}

void led_processing() {
    // Blink green to indicate processing
    digitalWrite(PIN_LED_GREEN, HIGH);
    digitalWrite(PIN_LED_RED, HIGH);
}

void led_off() {
    digitalWrite(PIN_LED_GREEN, LOW);
    digitalWrite(PIN_LED_RED, LOW);
}
