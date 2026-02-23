#pragma once

#include <Arduino.h>

struct QueuedEvent {
    String eventType;
    String notes;
    double batteryVoltage;
    String apiKey;
    unsigned long timestamp;
};

void offline_queue_init();
bool offline_queue_push(const QueuedEvent& event);
int offline_queue_size();
int offline_queue_flush(const char* baseUrl, const char* endpoint);
