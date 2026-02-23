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

// Send camera image to API for dog identification (uses direct HTTPClient with response parsing)
// side: "inside" or "outside" indicating which camera triggered the request
AccessResponse api_request_access(camera_fb_t* fb, const char* side);
AccessResponse api_request_access_direct(camera_fb_t* fb, const char* side);

// Post an approach photo to the API — logs an AnimalApproach event with the captured image.
// Called for every motion+proximity detection regardless of TFLite result.
// Returns true if the HTTP POST succeeded (204 No Content).
bool api_post_approach_photo(camera_fb_t* fb, const char* side);

// Post a firmware event (door opened/closed, power events, etc.)
void api_post_firmware_event(const char* apiKey, const char* eventType, const char* notes, double batteryVoltage);

#endif // API_CLIENT_H
