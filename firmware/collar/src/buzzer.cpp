#include "buzzer.h"
#include "config.h"

#ifndef UNIT_TEST
#include <Arduino.h>

static bool playing = false;
static uint32_t pattern_start = 0;
static uint32_t pattern_duration = 0;
static BuzzPattern current_pattern = BUZZ_SHORT;
static uint8_t pattern_step = 0;

void buzzer_init() {
    pinMode(PIN_BUZZER, OUTPUT);
    digitalWrite(PIN_BUZZER, LOW);
    playing = false;
}

static void tone_on(uint16_t freq) {
    ledcAttach(PIN_BUZZER, freq, 8);
    ledcWrite(PIN_BUZZER, BUZZER_MAX_DUTY);
}

static void tone_off() {
    ledcDetach(PIN_BUZZER);
    digitalWrite(PIN_BUZZER, LOW);
}

void buzzer_play(BuzzPattern pattern) {
    current_pattern = pattern;
    pattern_start = millis();
    pattern_step = 0;
    playing = true;
}

void buzzer_play_continuous(uint32_t duration_ms) {
    current_pattern = BUZZ_CONTINUOUS;
    pattern_start = millis();
    pattern_duration = duration_ms;
    pattern_step = 0;
    playing = true;
}

void buzzer_stop() {
    tone_off();
    playing = false;
}

bool buzzer_is_playing() {
    return playing;
}

void buzzer_update() {
    if (!playing) return;

    uint32_t elapsed = millis() - pattern_start;

    switch (current_pattern) {
        case BUZZ_SHORT:
            // Double beep: 100ms on, 100ms off, 100ms on
            if (elapsed < 100)       tone_on(BUZZER_FREQ_WARNING);
            else if (elapsed < 200)  tone_off();
            else if (elapsed < 300)  tone_on(BUZZER_FREQ_WARNING);
            else { tone_off(); playing = false; }
            break;

        case BUZZ_LONG:
            // Triple beep: 500ms on, 200ms off, 500ms on, 200ms off, 500ms on
            if (elapsed < 500)        tone_on(BUZZER_FREQ_BREACH);
            else if (elapsed < 700)   tone_off();
            else if (elapsed < 1200)  tone_on(BUZZER_FREQ_BREACH);
            else if (elapsed < 1400)  tone_off();
            else if (elapsed < 1900)  tone_on(BUZZER_FREQ_BREACH);
            else { tone_off(); playing = false; }
            break;

        case BUZZ_CONTINUOUS:
            // 2s on / 1s off, repeating
            if (pattern_duration > 0 && elapsed > pattern_duration) {
                tone_off();
                playing = false;
                break;
            }
            {
                uint32_t cycle_pos = elapsed % 3000;
                if (cycle_pos < 2000) tone_on(BUZZER_FREQ_CONTINUOUS);
                else                  tone_off();
            }
            break;

        case BUZZ_LOCATE:
            // Ascending chirps for find-my-dog
            if (pattern_duration > 0 && elapsed > pattern_duration) {
                tone_off();
                playing = false;
                break;
            }
            {
                uint32_t cycle_pos = elapsed % 1000;
                if (cycle_pos < 200)       tone_on(2000);
                else if (cycle_pos < 300)  tone_off();
                else if (cycle_pos < 500)  tone_on(3000);
                else if (cycle_pos < 600)  tone_off();
                else if (cycle_pos < 800)  tone_on(4000);
                else                       tone_off();
            }
            break;
    }
}

#endif  // !UNIT_TEST
