#include "camera.h"
#include "config.h"

bool camera_init() {
    camera_config_t config;
    config.ledc_channel = LEDC_CHANNEL_0;
    config.ledc_timer = LEDC_TIMER_0;
    config.pin_d0 = Y2_GPIO_NUM;
    config.pin_d1 = Y3_GPIO_NUM;
    config.pin_d2 = Y4_GPIO_NUM;
    config.pin_d3 = Y5_GPIO_NUM;
    config.pin_d4 = Y6_GPIO_NUM;
    config.pin_d5 = Y7_GPIO_NUM;
    config.pin_d6 = Y8_GPIO_NUM;
    config.pin_d7 = Y9_GPIO_NUM;
    config.pin_xclk = XCLK_GPIO_NUM;
    config.pin_pclk = PCLK_GPIO_NUM;
    config.pin_vsync = VSYNC_GPIO_NUM;
    config.pin_href = HREF_GPIO_NUM;
    config.pin_sccb_sda = SIOD_GPIO_NUM;
    config.pin_sccb_scl = SIOC_GPIO_NUM;
    config.pin_pwdn = PWDN_GPIO_NUM;
    config.pin_reset = RESET_GPIO_NUM;
    config.xclk_freq_hz = 20000000;
    config.pixel_format = PIXFORMAT_JPEG;
    config.grab_mode = CAMERA_GRAB_LATEST;

    // Use PSRAM for higher resolution
    if (psramFound()) {
        config.frame_size = FRAMESIZE_QVGA;  // 320x240
        config.jpeg_quality = 12;
        config.fb_count = 2;
        config.fb_location = CAMERA_FB_IN_PSRAM;
    } else {
        config.frame_size = FRAMESIZE_QQVGA;  // 160x120
        config.jpeg_quality = 15;
        config.fb_count = 1;
        config.fb_location = CAMERA_FB_IN_DRAM;
    }

    esp_err_t err = esp_camera_init(&config);
    if (err != ESP_OK) {
        Serial.printf("Camera init failed with error 0x%x\n", err);
        return false;
    }

    // Adjust camera settings for better dog detection
    sensor_t *s = esp_camera_sensor_get();
    if (s) {
        s->set_brightness(s, 1);     // Slightly brighter
        s->set_contrast(s, 1);       // Slightly more contrast
        s->set_saturation(s, 0);     // Normal saturation
        s->set_whitebal(s, 1);       // Enable auto white balance
        s->set_awb_gain(s, 1);       // Enable AWB gain
        s->set_exposure_ctrl(s, 1);  // Enable auto exposure
        s->set_aec2(s, 1);           // Enable AEC DSP
        s->set_gain_ctrl(s, 1);      // Enable AGC
    }

    Serial.println("Camera initialized successfully");
    return true;
}

camera_fb_t* camera_capture() {
    camera_fb_t *fb = esp_camera_fb_get();
    if (!fb) {
        Serial.println("Camera capture failed");
        return nullptr;
    }

    Serial.printf("Captured image: %dx%d, %d bytes\n", fb->width, fb->height, fb->len);
    return fb;
}

void camera_release(camera_fb_t* fb) {
    if (fb) {
        esp_camera_fb_return(fb);
    }
}
