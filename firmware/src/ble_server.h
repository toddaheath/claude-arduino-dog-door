#pragma once

#include <Arduino.h>

void ble_server_init();
void ble_server_update();
void ble_server_set_status(bool doorOpen, const char* lastEvent, bool wifiConnected, int batteryPct);
bool ble_server_get_command(bool* openDoor);
bool ble_server_get_wifi_update(char* ssid, char* pass, size_t maxLen);
