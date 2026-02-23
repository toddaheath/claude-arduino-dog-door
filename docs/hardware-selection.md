# Hardware Selection & Bill of Materials

## Why ESP32-CAM over Arduino

| Feature | Arduino Uno + Shields | ESP32-CAM |
|---------|----------------------|-----------|
| Camera | External module (~$15) | Built-in OV2640 |
| WiFi | WiFi shield (~$10) | Built-in 802.11 b/g/n |
| RAM | 2KB SRAM | 520KB SRAM + 4MB PSRAM |
| CPU | 16MHz single-core | 240MHz dual-core |
| ML Capable | No | Yes (TFLite Micro) |
| Cost | ~$30+ (board + shields) | ~$10 |
| Arduino Compatible | Native | Yes (via Arduino framework) |

The ESP32-CAM is the clear winner: it integrates camera and WiFi, has enough memory for TensorFlow Lite Micro inference, and costs a third of the Arduino equivalent.

## Bill of Materials

| # | Item | Purpose | Est. Cost |
|---|------|---------|-----------|
| 1 | ESP32-CAM (AI-Thinker) | Main controller with camera | $10 |
| 2 | RCWL-0516 Microwave Radar | Long-range motion detection (up to 7m) | $3 |
| 3 | HC-SR04 Ultrasonic Sensor | Proximity measurement (2-400cm) | $3 |
| 4 | 12V Linear Actuator (50mm stroke) | Physically opens/closes door flap | $25 |
| 5 | L298N Motor Driver Module | Controls actuator direction and speed | $7 |
| 6 | IR Break Beam Sensors (pair) | Safety interlock — detects animal in doorway | $5 |
| 7 | 12V 5A Power Supply | Powers actuator and system | $12 |
| 8 | LM2596 Buck Converter | Steps 12V down to 3.3V for ESP32 | $3 |
| 9 | Magnetic Reed Switch | Detects door fully open/closed position | $2 |
| 10 | LEDs (5-pack, assorted) | Status indicators (ready, processing, allow, deny, error) | $3 |
| 11 | Breadboard + Jumper Wires | Prototyping connections | $6 |
| 12 | Waterproof Enclosure | Protects electronics from weather | $5 |
| | **Total** | | **~$84** |

## Sensor Selection Rationale

### RCWL-0516 Microwave Radar ($3)
- Detects motion through walls/enclosures
- 7m range — triggers camera well before animal reaches door
- Digital output (HIGH/LOW) — simple to interface
- Works in all lighting conditions unlike PIR

### HC-SR04 Ultrasonic ($3)
- Confirms animal is within door proximity (< 50cm)
- Prevents false triggers from distant motion
- Provides distance measurement for approach detection

### IR Break Beam ($5)
- Safety interlock across door opening
- Prevents door from closing while animal is passing through
- Instant response time (~1ms)

### Magnetic Reed Switch ($2)
- Confirms actuator has fully opened or closed the door
- Prevents actuator from running past its limits
- Failsafe for position feedback

## Wiring Overview

```
12V PSU ──→ L298N Motor Driver ──→ Linear Actuator
    │
    └──→ LM2596 Buck ──→ 3.3V ──→ ESP32-CAM
                                      │
                                      ├── GPIO12 ← RCWL-0516 (OUT)
                                      ├── GPIO13 → HC-SR04 (TRIG)
                                      ├── GPIO14 ← HC-SR04 (ECHO)
                                      ├── GPIO15 ← IR Break Beam
                                      ├── GPIO2  → L298N (IN1)
                                      ├── GPIO4  → L298N (IN2)
                                      ├── GPIO16 ← Reed Switch
                                      ├── GPIO33 → Status LED (Green)
                                      └── GPIO32 → Status LED (Red)
```

Note: ESP32-CAM has limited exposed GPIO pins. GPIO0 and GPIO4 have special boot functions — GPIO4 also drives the onboard flash LED. Pin assignments may need adjustment based on the specific ESP32-CAM board revision.

> **Full wiring diagrams** with per-component connection details, resistor values, level-shifting, power distribution, and dual-camera setup: see [wiring-diagrams.md](wiring-diagrams.md).

## Power Budget

| Component | Voltage | Max Current |
|-----------|---------|-------------|
| ESP32-CAM (active + WiFi) | 3.3V | 310mA |
| OV2640 Camera | 3.3V | 140mA |
| RCWL-0516 | 3.3V | 3mA |
| HC-SR04 | 5V (tolerant) | 15mA |
| IR Break Beam | 3.3V | 20mA |
| LEDs (x2) | 3.3V | 40mA |
| Linear Actuator | 12V | 2A (stall) |
| **Total** | | **~2.5A @ 12V** |

A 12V 5A supply provides ample headroom for all components including actuator stall current.
