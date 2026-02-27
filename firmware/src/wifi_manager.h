#ifndef WIFI_MANAGER_H
#define WIFI_MANAGER_H

#include <Arduino.h>

// Connect to WiFi. Returns true if connected.
bool wifi_connect();

// Check if WiFi is connected
bool wifi_is_connected();

// Reconnect if disconnected
void wifi_ensure_connected();

// Get local IP address as string
String wifi_get_ip();

// Save WiFi credentials to NVS (encrypted storage)
bool wifi_save_credentials(const char* ssid, const char* pass);

#endif // WIFI_MANAGER_H
