#pragma once

#include <stdbool.h>
#include <stdint.h>

bool wifi_connect(uint32_t timeout_ms);
void wifi_disconnect();
int  wifi_upload_locations();
void wifi_upload_geofence_events();
void wifi_sync_geofences();
bool wifi_check_ota_available();
bool wifi_perform_ota();
