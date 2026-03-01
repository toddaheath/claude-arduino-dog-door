#pragma once

#include <stdint.h>

typedef enum {
    BUZZ_SHORT,       // Short double-beep (warning)
    BUZZ_LONG,        // Longer triple-beep (breach)
    BUZZ_CONTINUOUS,  // Sustained tone (extended breach)
    BUZZ_LOCATE       // Find-my-dog pattern
} BuzzPattern;

void buzzer_init();
void buzzer_play(BuzzPattern pattern);
void buzzer_play_continuous(uint32_t duration_ms);
void buzzer_stop();
void buzzer_update();  // Call from loop() to handle async patterns
bool buzzer_is_playing();
