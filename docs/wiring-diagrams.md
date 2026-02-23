# Wiring Diagrams

This document provides detailed wiring instructions for every electrical component in the Smart Dog Door system.

---

## System Overview

The system is powered by a single 12V 5A supply. A buck converter steps this down to 3.3V for the ESP32-CAM and low-voltage sensors. All sensor signals are 3.3V logic. The 12V rail feeds the linear actuator through the L298N motor driver.

```
┌─────────────────────────────────────────────────────────────┐
│                    12V 5A Power Supply                       │
└──────────────┬───────────────────────────────┬──────────────┘
               │ 12V                           │ 12V
               ▼                               ▼
     ┌─────────────────┐             ┌──────────────────┐
     │  LM2596 Buck    │             │  L298N Motor     │
     │  Converter      │             │  Driver          │
     │  (12V → 3.3V)   │             │                  │
     └────────┬────────┘             └────────┬─────────┘
              │ 3.3V                          │
              ▼                               ▼
     ┌─────────────────┐             ┌──────────────────┐
     │   ESP32-CAM     │             │  12V Linear      │
     │  (AI-Thinker)   │             │  Actuator        │
     └──────┬──────────┘             └──────────────────┘
            │
   ┌────────┼─────────────────────────────────┐
   │        │                                 │
   ▼        ▼                                 ▼
RCWL-0516  HC-SR04                  IR Break Beam
 Radar    Ultrasonic                  Sensors
```

---

## 1. LM2596 Buck Converter (12V → 3.3V)

The LM2596 steps the 12V supply down to 3.3V for the ESP32-CAM and sensors. **Trim the output voltage to exactly 3.3V before connecting anything.**

```
12V PSU (+) ──────────────────────── LM2596 IN+
12V PSU (-) ──────────────────────── LM2596 IN-  (GND)

LM2596 OUT+ ─────────────────────── 3.3V rail  ──→ ESP32-CAM 3.3V
LM2596 OUT- ─────────────────────── GND rail   ──→ ESP32-CAM GND

                 ┌────────────────────────────────┐
                 │         LM2596 Module          │
                 │                                │
    12V (+) ──►  │ IN+             OUT+ ├──► 3.3V │
    12V (-) ──►  │ IN-  (GND)      OUT- ├──► GND  │
                 │                                │
                 │          [Trim Pot]            │
                 └────────────────────────────────┘

Adjustment: Use a multimeter on OUT+ / OUT- while turning the trim pot
clockwise until the output reads 3.30V (±0.05V).
```

**Pin summary:**

| LM2596 Pin | Connects To      |
|------------|-----------------|
| IN+        | 12V PSU positive |
| IN-        | 12V PSU negative (GND) |
| OUT+       | 3.3V power rail  |
| OUT-       | Common GND rail  |

---

## 2. ESP32-CAM (AI-Thinker)

The ESP32-CAM is the main controller. It runs on 3.3V. The built-in OV2640 camera is connected internally via the board's flex cable — no external wiring is needed for the camera.

```
                  ┌──────────────────────┐
                  │    ESP32-CAM         │
                  │    AI-Thinker        │
          3.3V ──►│ 3V3                  │
           GND ──►│ GND                  │
                  │                      │
RCWL-0516 OUT ──►│ GPIO12 (PIN_RADAR)   │
  HC-SR04 TRIG ◄─│ GPIO13 (PIN_US_TRIG) │
  HC-SR04 ECHO ──►│ GPIO14 (PIN_US_ECHO) │
 IR Break Beam ──►│ GPIO15 (PIN_IR_BEAM) │
L298N IN1      ◄─│ GPIO2  (PIN_MOT_IN1) │
L298N IN2      ◄─│ GPIO4  (PIN_MOT_IN2) │
 Reed Switch   ──►│ GPIO16 (PIN_REED)    │
  Green LED+   ◄─│ GPIO33 (PIN_LED_GRN) │
    Red LED+   ◄─│ GPIO32 (PIN_LED_RED) │
                  │                      │
                  │ GPIO0   (boot/prog)  │ ← Leave floating in production
                  │ GPIO4   (flash LED)  │ ← Shared with PIN_MOT_IN2 — see note
                  └──────────────────────┘
```

> **GPIO4 Note:** GPIO4 drives both the onboard white flash LED and `PIN_MOT_IN2`. When the motor driver IN2 is pulled HIGH the flash LED will illuminate. This is cosmetically inconvenient but electrically harmless. If the flash causes interference, replace the LED with a 470 Ω pull-down on GPIO4's flash LED pad.

> **GPIO32 Note:** GPIO32 is also `PWDN_GPIO_NUM` for the camera power-down pin. In the AI-Thinker schematic this is dedicated to camera power management, so use GPIO33 for the red status LED instead of GPIO32 if camera stability issues appear.

---

## 3. RCWL-0516 Microwave Radar Sensor

Detects motion through enclosure walls at up to 7 m. Single digital output.

```
                  ┌──────────────────────┐
                  │     RCWL-0516        │
          3.3V ──►│ VCC                  │
           GND ──►│ GND                  │
      ESP32 12 ◄─│ OUT   (HIGH on motion)│
                  │ CDS   (light-dep opt) │ ← Leave unconnected
                  │ RADAR                │ ← Antenna (onboard trace)
                  └──────────────────────┘
```

**Wiring table:**

| RCWL-0516 Pin | Wire Color | Connects To         |
|---------------|-----------|---------------------|
| VCC           | Red        | 3.3V rail            |
| GND           | Black      | GND rail             |
| OUT           | Yellow     | ESP32-CAM GPIO12     |
| CDS           | —          | Not connected        |

**Sensitivity note:** There are no resistors on the RCWL-0516 OUT line required. The module's output is 3.3V-compatible when powered at 3.3V.

---

## 4. HC-SR04 Ultrasonic Distance Sensor

Confirms animal is within 50 cm before triggering full detection. Requires a 5V supply but its ECHO line must be level-shifted down to 3.3V.

```
                  ┌──────────────────────┐
                  │      HC-SR04         │
           5V  ──►│ VCC                  │
          GND  ──►│ GND                  │
     ESP32 13  ──►│ TRIG  (10 µs pulse)  │
     [divider] ──►│ ECHO  (3.3V limited) │──► ESP32-CAM GPIO14
                  └──────────────────────┘

Level Shifter for ECHO line (voltage divider):

  HC-SR04 ECHO ──┬── 1kΩ ──┬── ESP32 GPIO14
                 │         │
                 │        2kΩ
                 │         │
                           GND
```

**ECHO level-shift resistor values:** 1 kΩ (series, between ECHO and GPIO14) + 2 kΩ (shunt, between junction and GND). This divides 5V ECHO → ~3.33V, within 3.3V GPIO tolerance.

**Wiring table:**

| HC-SR04 Pin | Connects To                            |
|-------------|----------------------------------------|
| VCC         | 5V (from L298N onboard 5V reg or PSU)  |
| GND         | GND rail                               |
| TRIG        | ESP32-CAM GPIO13 (direct, 3.3V OK)    |
| ECHO        | 1kΩ → GPIO14; junction shunted to GND via 2kΩ |

> **5V source:** The L298N motor driver module includes a 5V linear regulator. Use its 5V output pin to power the HC-SR04 rather than adding a separate regulator.

---

## 5. L298N Dual H-Bridge Motor Driver

Controls the direction and power delivery to the 12V linear actuator.

```
                  ┌───────────────────────────────┐
                  │          L298N Module          │
          12V  ──►│ 12V IN                         │
          GND  ──►│ GND                            │
                  │ 5V OUT ──────────────────────► HC-SR04 VCC
                  │                               │
    ESP32 GPIO2 ──►│ IN1       OUT1 ─────────────► Actuator (+)
    ESP32 GPIO4 ──►│ IN2       OUT2 ─────────────► Actuator (-)
                  │                               │
                  │ ENA  (enable A) ──────────────► 3.3V or jumper
                  └───────────────────────────────┘

Motor direction truth table:
┌──────┬──────┬──────────────────────────┐
│ IN1  │ IN2  │ Actuator Action          │
├──────┼──────┼──────────────────────────┤
│ HIGH │ LOW  │ Extend (open door)       │
│ LOW  │ HIGH │ Retract (close door)     │
│ LOW  │ LOW  │ Coast (off)              │
│ HIGH │ HIGH │ Brake (avoid)            │
└──────┴──────┴──────────────────────────┘
```

**Wiring table:**

| L298N Pin | Connects To                       |
|-----------|-----------------------------------|
| 12V IN    | 12V PSU positive                  |
| GND       | Common GND rail                   |
| 5V OUT    | HC-SR04 VCC                       |
| IN1       | ESP32-CAM GPIO2                   |
| IN2       | ESP32-CAM GPIO4                   |
| OUT1      | Linear actuator positive wire     |
| OUT2      | Linear actuator negative wire     |
| ENA       | Jumper to 5V (always enabled) or PWM from ESP32 for speed control |

> **Flyback diodes:** The L298N module already includes internal flyback diodes for motor protection. No external diodes are required.

---

## 6. 12V Linear Actuator (50 mm stroke)

Physically opens and closes the door flap. It is a DC motor inside a screw drive — reversing polarity reverses direction.

```
                  Linear Actuator
                  ┌──────────────┐
  L298N OUT1 ──►  │  (+) Red     │
  L298N OUT2 ──►  │  (-) Black   │
                  └──────────────┘

  Stroke: 50 mm extends = door open
          50 mm retracts = door closed

  ┌────────────────────────────────────────────────────────┐
  │ Door fully closed                       Door fully open │
  │   ◄── 0 mm                                 50 mm ──►   │
  │   [Retracted]          [Extended]                       │
  └────────────────────────────────────────────────────────┘
```

**Wiring table:**

| Actuator Wire | Connects To    | Action when HIGH |
|---------------|---------------|-----------------|
| Red (+)       | L298N OUT1    | Extends (opens)  |
| Black (-)     | L298N OUT2    | —                |

> **Stall current:** The actuator draws up to 2A at stall. The L298N supports 2A peak per channel. Ensure the 12V PSU is rated ≥ 5A.

---

## 7. IR Break Beam Sensors (Safety Interlock)

A transmitter/receiver pair strung across the door opening. If the beam is broken while the door is closing, the ESP32 halts the actuator.

```
 Door Frame Left                        Door Frame Right
      │                                       │
  ┌───┴────────┐                      ┌───────┴────┐
  │ Transmitter│ ═══════ IR beam ═══► │  Receiver  │
  │            │                      │            │
  │ VCC ◄── 3.3V                      │ VCC ◄── 3.3V
  │ GND ◄── GND                       │ GND ◄── GND
  └────────────┘                      │ OUT ──────► ESP32 GPIO15
                                      └────────────┘

GPIO15 logic:
  HIGH (beam intact) = doorway clear
  LOW  (beam broken) = animal in doorway → inhibit close
```

**Wiring table:**

| IR Sensor     | Pin  | Connects To       |
|---------------|------|-------------------|
| Transmitter   | VCC  | 3.3V rail         |
| Transmitter   | GND  | GND rail          |
| Receiver      | VCC  | 3.3V rail         |
| Receiver      | GND  | GND rail          |
| Receiver      | OUT  | ESP32-CAM GPIO15  |

> **Pull-up:** Enable the internal pull-up on GPIO15 (`pinMode(PIN_IR_BEAM, INPUT_PULLUP)`) so that a disconnected receiver reads HIGH (fail-safe: treat as beam intact).

---

## 8. Magnetic Reed Switch (Door Position Feedback)

A normally-open reed switch. When the magnet (mounted on the actuator arm) aligns with the switch (mounted on the door frame), the circuit closes.

```
  Door frame (fixed)          Actuator arm (moving)
  ┌──────────────┐            ┌──────────────┐
  │ Reed Switch  │  ◄─────►  │    Magnet    │
  │              │            └──────────────┘
  │ Pin A ──────────────────► ESP32 GPIO16
  │ Pin B ──────────────────► GND
  └──────────────┘

With 10kΩ pull-up on GPIO16:
  GPIO16 HIGH = door open (magnet away, switch open)
  GPIO16 LOW  = door closed (magnet present, switch closed)
```

**Wiring table:**

| Reed Switch Pin | Connects To            |
|-----------------|------------------------|
| A               | ESP32-CAM GPIO16       |
| B               | GND rail               |
| (pull-up)       | 10 kΩ from GPIO16 to 3.3V |

> Use `INPUT_PULLUP` in firmware to avoid the need for an external resistor.

---

## 9. Status LEDs

Two LEDs indicate the door's current state. Each needs a current-limiting resistor. At 3.3V and a typical LED forward voltage of ~2.0V, a 68 Ω resistor gives ~19 mA.

```
ESP32 GPIO33 ──► [68Ω] ──► Green LED (+) ──► GND
ESP32 GPIO32 ──► [68Ω] ──►   Red LED (+) ──► GND

LED states:
┌─────────────┬─────────────┬──────────────────────────────┐
│ Green (33)  │ Red (32)    │ Meaning                      │
├─────────────┼─────────────┼──────────────────────────────┤
│ OFF         │ OFF         │ Idle / standby               │
│ Blink       │ OFF         │ Processing (TFLite + API)    │
│ ON          │ OFF         │ Access granted / door open   │
│ OFF         │ ON          │ Access denied                │
│ Blink       │ Blink       │ Error / camera failure       │
└─────────────┴─────────────┴──────────────────────────────┘
```

**Wiring table:**

| LED   | GPIO | Series Resistor | LED (+) to | LED (-) to |
|-------|------|----------------|-----------|-----------|
| Green | 33   | 68 Ω           | Resistor  | GND       |
| Red   | 32   | 68 Ω           | Resistor  | GND       |

---

## 10. Full System Wiring Diagram

```
                        ┌──────────────┐
                        │  12V 5A PSU  │
                        └──┬───────┬──┘
                    12V(+) │       │ 12V(-)
                     ┌─────┘       └─────┐
                     │                   │
          ┌──────────┴──────────┐        │
          │    LM2596 Buck      │        │
          │    12V → 3.3V       │        │
          └─────────┬───────────┘        │
                3.3V│        GND─────────┤
                    │                    │
          ┌─────────┴────────────────────┴──────────────────────────────┐
          │                  ESP32-CAM (AI-Thinker)                     │
          │  3V3  GND  GPIO12  GPIO13  GPIO14  GPIO15  GPIO2  GPIO4     │
          │   │    │     │       │       │       │      │      │        │
          │   │    │     │       │       │       │      │      │        │
          │   │    │  RCWL │  HC-SR04 │  IR Beam│   L298N Motor Ctrl   │
          │   │    │  0516 │  Ultrason│  Interlock│                     │
          │   │    │       │         │           │                      │
          │  GPIO16  GPIO33  GPIO32                                     │
          │    │       │       │                                        │
          │  Reed   Green    Red                                        │
          │  Switch  LED     LED                                        │
          └──────────────────────────────────────────────────────────  ┘

          ┌─────────────────────────────────┐
          │        L298N Motor Driver       │
          │ 12V─┐  IN1◄GPIO2  OUT1─┐        │
          │ GND─┘  IN2◄GPIO4  OUT2─┘        │
          │        5V OUT ──► HC-SR04 VCC   │
          └─────────────────────────────────┘
                 OUT1 ──────► Actuator Red (+)
                 OUT2 ──────► Actuator Black (-)

┌─────────────────────────────────────────────────────────────────────────┐
│                       Component Summary                                  │
│                                                                          │
│  RCWL-0516:    VCC→3.3V, GND→GND, OUT→GPIO12                           │
│  HC-SR04:      VCC→5V(L298N), GND→GND, TRIG→GPIO13, ECHO→÷2→GPIO14    │
│  IR Beam TX:   VCC→3.3V, GND→GND                                       │
│  IR Beam RX:   VCC→3.3V, GND→GND, OUT→GPIO15                          │
│  L298N:        12V→12V PSU, GND→GND, IN1→GPIO2, IN2→GPIO4              │
│  Actuator:     (+)→L298N OUT1, (-)→L298N OUT2                          │
│  Reed Switch:  A→GPIO16 (+ 10kΩ pullup), B→GND                        │
│  Green LED:    (+)→68Ω→GPIO33, (-)→GND                                 │
│  Red LED:      (+)→68Ω→GPIO32, (-)→GND                                 │
│  LM2596:       IN+→12V PSU, IN-→GND, OUT+→3.3V rail, OUT-→GND         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 11. Power Distribution Diagram

```
   12V 5A PSU
       │
       ├──[12V]──► L298N IN+ ──► Linear Actuator (12V, 2A peak)
       │
       └──[12V]──► LM2596 IN+
                       │
                   [3.3V OUT]
                       │
                       ├──► ESP32-CAM 3V3 (310 mA)
                       ├──► RCWL-0516 VCC (3 mA)
                       ├──► IR Beam TX VCC (20 mA)
                       ├──► IR Beam RX VCC (20 mA)
                       ├──► Green LED via 68Ω (19 mA)
                       └──► Red LED via 68Ω (19 mA)

   L298N 5V OUT ──► HC-SR04 VCC (15 mA)

   Total 3.3V load: ~391 mA   (LM2596 rated 3A — adequate headroom)
   Total 12V load:  ~2.5A     (PSU rated 5A — adequate headroom)
```

---

## 12. Dual-Camera Wiring (Second Board — Outside)

The system uses two identical ESP32-CAM boards — one mounted inside and one outside. Each board is wired identically (see sections above). The only difference is a compile-time flag:

```cpp
// Inside board:  -DTHIS_SIDE=SIDE_INSIDE
// Outside board: -DTHIS_SIDE=SIDE_OUTSIDE
```

Both boards connect to the same 12V power supply through separate LM2596 buck converters. Both communicate with the same API server over WiFi. Each board independently detects approach events and sends its own approach photo to the API, which logs them separately with a `side` field (`Inside` / `Outside`).

```
   12V PSU
     │
     ├── LM2596 #1 → 3.3V → ESP32-CAM #1 (Inside)  ─► API
     │
     └── LM2596 #2 → 3.3V → ESP32-CAM #2 (Outside) ─► API

   Shared: L298N, Linear Actuator
   (One actuator; control signals come from whichever board initiated the event)
```

> **Actuator arbitration:** In practice, both boards should not command the actuator simultaneously. Configure the actuator wiring on only one board (e.g., the inside board) and have the outside board send access requests; the API response is handled by the inside board. Or use a dedicated actuator controller with the API as the single source of truth.

---

## Notes & Safety

- **Never connect 12V directly to the ESP32-CAM** — it will be destroyed. Always route through the LM2596.
- **Verify buck converter output** with a multimeter before connecting to the ESP32-CAM.
- **Waterproofing:** Mount all electronics in the waterproof enclosure. Run sensor cables through waterproof cable glands.
- **Strain relief:** Secure cables near connectors to prevent vibration damage.
- **Fuse the 12V line** with a 5A automotive fuse inline with the PSU positive lead.
