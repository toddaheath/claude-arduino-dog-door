#pragma once

#include <Arduino.h>

bool cellular_init();
bool cellular_is_registered();
int cellular_http_post(const char* url, const char* contentType, const uint8_t* body, size_t len);
