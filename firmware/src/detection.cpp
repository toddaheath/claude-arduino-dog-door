#include "detection.h"
#include "config.h"

// TFLite Micro includes
#include <TensorFlowLite_ESP32.h>
#include "tensorflow/lite/micro/all_ops_resolver.h"
#include "tensorflow/lite/micro/micro_interpreter.h"
#include "tensorflow/lite/schema/schema_generated.h"
#include "tensorflow/lite/micro/micro_error_reporter.h"

// Model data (placeholder - replace with trained model)
#include "../model/dog_detect_model.h"

// TFLite globals
static tflite::AllOpsResolver resolver;
static const tflite::Model* model = nullptr;
static tflite::MicroInterpreter* interpreter = nullptr;
static TfLiteTensor* input_tensor = nullptr;
static TfLiteTensor* output_tensor = nullptr;

// Tensor arena (allocated in PSRAM if available)
static uint8_t* tensor_arena = nullptr;
static const int kTensorArenaSize = TFLITE_ARENA_SIZE;

bool detection_init() {
    // Allocate tensor arena in PSRAM for better performance
    if (psramFound()) {
        tensor_arena = (uint8_t*)ps_malloc(kTensorArenaSize);
    } else {
        tensor_arena = (uint8_t*)malloc(kTensorArenaSize);
    }

    if (!tensor_arena) {
        Serial.println("Failed to allocate tensor arena");
        return false;
    }

    // Load the model
    model = tflite::GetModel(dog_detect_model);
    if (model->version() != TFLITE_SCHEMA_VERSION) {
        Serial.printf("Model schema version mismatch: %d vs %d\n",
                      model->version(), TFLITE_SCHEMA_VERSION);
        return false;
    }

    // Build the interpreter
    static tflite::MicroErrorReporter micro_error_reporter;
    static tflite::MicroInterpreter static_interpreter(
        model, resolver, tensor_arena, kTensorArenaSize, &micro_error_reporter);
    interpreter = &static_interpreter;

    TfLiteStatus allocate_status = interpreter->AllocateTensors();
    if (allocate_status != kTfLiteOk) {
        Serial.println("AllocateTensors() failed");
        return false;
    }

    input_tensor = interpreter->input(0);
    output_tensor = interpreter->output(0);

    Serial.printf("TFLite initialized. Input: [%d, %d, %d, %d], Output: [%d, %d]\n",
                  input_tensor->dims->data[0], input_tensor->dims->data[1],
                  input_tensor->dims->data[2], input_tensor->dims->data[3],
                  output_tensor->dims->data[0], output_tensor->dims->data[1]);

    return true;
}

float detection_run(camera_fb_t* fb) {
    if (!interpreter || !input_tensor || !output_tensor) {
        Serial.println("Detection not initialized");
        return -1.0f;
    }

    if (!fb || !fb->buf || fb->len == 0) {
        Serial.println("Invalid frame buffer");
        return -1.0f;
    }

    // The model expects a specific input size (e.g., 96x96x3 uint8).
    // In a real implementation, you would:
    // 1. Decode the JPEG to RGB
    // 2. Resize to model input dimensions
    // 3. Normalize pixel values
    //
    // For this placeholder, we fill the input tensor with the raw bytes
    // (actual implementation needs JPEG decode + resize)
    int input_size = input_tensor->bytes;
    const uint8_t* src = fb->buf;
    int src_len = fb->len;

    for (int i = 0; i < input_size; i++) {
        input_tensor->data.uint8[i] = src[i % src_len];
    }

    // Run inference
    TfLiteStatus invoke_status = interpreter->Invoke();
    if (invoke_status != kTfLiteOk) {
        Serial.println("Invoke failed");
        return -1.0f;
    }

    // Output tensor: [1, 2] = [not_dog, dog] probabilities
    // For uint8 quantized model, dequantize:
    float scale = output_tensor->params.scale;
    int zero_point = output_tensor->params.zero_point;

    float dog_score;
    if (output_tensor->type == kTfLiteUInt8) {
        dog_score = (output_tensor->data.uint8[1] - zero_point) * scale;
    } else {
        dog_score = output_tensor->data.f[1];
    }

    Serial.printf("Detection score: %.3f\n", dog_score);
    return dog_score;
}
