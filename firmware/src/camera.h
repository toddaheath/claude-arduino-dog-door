#ifndef CAMERA_H
#define CAMERA_H

#include <Arduino.h>
#include "esp_camera.h"

// Initialize the OV2640 camera
bool camera_init();

// Capture a JPEG frame. Returns the framebuffer (caller must return with esp_camera_fb_return)
camera_fb_t* camera_capture();

// Return a framebuffer after use
void camera_release(camera_fb_t* fb);

#endif // CAMERA_H
