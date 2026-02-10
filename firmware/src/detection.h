#ifndef DETECTION_H
#define DETECTION_H

#include <Arduino.h>
#include "esp_camera.h"

// Initialize TFLite Micro interpreter with the dog detection model
bool detection_init();

// Run inference on a camera frame. Returns confidence score (0.0 - 1.0)
// that the image contains a dog. Returns -1.0 on error.
float detection_run(camera_fb_t* fb);

#endif // DETECTION_H
