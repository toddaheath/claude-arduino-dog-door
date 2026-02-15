#ifndef API_CLIENT_H
#define API_CLIENT_H

#include <Arduino.h>
#include "esp_camera.h"

struct AccessResponse {
    bool allowed;
    int animalId;
    String animalName;
    float confidenceScore;
    String reason;
    String direction;
    bool success;  // true if API call succeeded
};

// Send camera image to API for dog identification
// side: "inside" or "outside" indicating which camera triggered the request
AccessResponse api_request_access(camera_fb_t* fb, const char* side);

#endif // API_CLIENT_H
