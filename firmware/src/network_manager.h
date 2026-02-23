#pragma once

#include <Arduino.h>

enum class NetworkTransport { None, WiFi, Cellular };

void network_manager_init();
void network_manager_ensure_connected();
NetworkTransport network_manager_get_transport();
bool network_manager_is_connected();
int network_manager_http_post_json(const char* url, const String& body);
int network_manager_http_post_multipart(const char* url, const uint8_t* body, size_t len, const char* contentType);
