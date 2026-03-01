# Collar PCB & Enclosure Design Guide

## Overview

Detailed PCB layout, schematic guidance, and 3D-printed enclosure design for the collar companion device.

---

## 1. Schematic Overview

```
                                USB-C Connector
                                     │
                                     ▼
                              ┌──────────────┐
                              │   TP4056     │
                              │ Charge IC    │
                    ┌────────►│ + DW01 prot  │◄──── LiPo 500mAh
                    │         └──────┬───────┘      3.7V
                    │                │
                    │                ▼
                    │         ┌──────────────┐
                    │         │  TPS63001    │
                    │         │  Buck-Boost   │──── 3.3V Rail
                    │         │  3.3V output  │     │
                    │         └──────────────┘     │
                    │                               │
      ┌─────────────┼───────────────────────────────┤
      │             │                               │
      ▼             ▼                               ▼
┌───────────┐ ┌───────────┐                  ┌───────────┐
│  ESP32-S3 │ │  MAX-M10S │                  │  PN532    │
│  MINI-1   │ │  GPS      │                  │  NFC      │
│           │ │           │                  │           │
│  GPIO1 ───┼─┤── SDA (I2C shared) ─────────┤── SDA     │
│  GPIO2 ───┼─┤── SCL (I2C shared) ─────────┤── SCL     │
│  GPIO3 ───┼─┤── TX (UART1)                │           │
│  GPIO4 ──◄┼─┤── RX (UART1)                │  GPIO5 ──►│── IRQ
│  GPIO10──►┼─┤── EN (power gate)            │  GPIO11─►│── RST
│           │ └───────────┘                  └───────────┘
│           │
│  GPIO6 ──►┤── LSM6DSO INT1 (wakeup)
│  GPIO1 ───┤── LSM6DSO SDA (shared I2C)
│  GPIO2 ───┤── LSM6DSO SCL (shared I2C)
│           │                    ┌───────────┐
│  GPIO7 ──►┼───────────────────►│  Piezo    │
│           │                    │  Buzzer   │
│  GPIO8 ──►┼───────────────────►│  WS2812B  │
│           │                    │  RGB LED  │
│  GPIO9 ──◄┼──── Voltage divider (100k/100k)
│           │     ← Battery tap
│           │
│  USB-C ───┼──── Native USB (prog/debug)
└───────────┘
```

### 1.1 I2C Bus Configuration

Shared I2C bus at 400kHz (Fast Mode):
- **Address 0x24**: PN532 NFC module
- **Address 0x6A**: LSM6DSO IMU
- **Pull-ups**: 4.7kΩ to 3.3V on SDA and SCL lines
- **Bus capacitance**: ~100pF (well within 400pF limit for Fast Mode)

### 1.2 UART Configuration

GPS UART (UART1):
- **Baud rate**: 9600 (default for u-blox MAX-M10S)
- **GPIO3 (TX)**: ESP32 → GPS RX
- **GPIO4 (RX)**: GPS TX → ESP32
- **Logic level**: 3.3V (native for both ESP32-S3 and MAX-M10S)

### 1.3 Power Gate Circuit

GPS module power controlled by N-channel MOSFET:

```
3.3V Rail ──┬──── MAX-M10S VCC
             │
             │    ┌────────┐
             └────┤  DRAIN │
                  │ Si2302  │ (N-ch MOSFET, SOT-23)
GPIO10 ──[10kΩ]──┤  GATE  │
                  │ SOURCE │──── GND
                  └────────┘

HIGH on GPIO10 = MOSFET conducts = GPS powered
LOW on GPIO10  = MOSFET off = GPS unpowered (~0 current)
```

Note: Using a P-channel MOSFET on the high side is more conventional, but N-channel on the low side is simpler and the MAX-M10S tolerates low-side switching.

### 1.4 Battery Voltage Divider

```
LiPo+ ──[100kΩ]──┬──[100kΩ]── GND
                   │
                   └──── GPIO9 (ADC input)

V_adc = V_battery × (100k / (100k + 100k)) = V_battery / 2
At 4.2V: V_adc = 2.1V (within ESP32-S3 ADC range of 0-3.3V with 11dB attenuation)
At 3.2V: V_adc = 1.6V
```

### 1.5 Buzzer Driver

```
GPIO7 ──[220Ω]──┬──── 2N2222 Base
                 │
          3.3V ──┤──── Collector ──── Piezo+ ────┐
                 │                                 │
                 └──── Emitter ──── GND ──── Piezo- ┘
                       │
                  [Flyback diode across piezo]

PWM frequency: 2000-4000 Hz (optimal for piezo resonance)
Max volume: controlled by PWM duty cycle (85 dB max at 1m)
```

---

## 2. PCB Layout

### 2.1 Board Dimensions

```
┌─────────────────────────────────────────┐
│                                          │  45mm
│  2-layer PCB, 1.0mm thickness            │
│  30mm × 20mm active area                 │
│  + 15mm antenna keepout                  │
│                                          │
│  Total: 45mm × 30mm                      │  30mm
│                                          │
└─────────────────────────────────────────┘
```

### 2.2 Component Placement (Top View)

```
                30mm
    ┌──────────────────────────────┐
    │  [GPS ANT]  ceramic patch    │
    │  18mm × 18mm × 4mm          │
    │  (keep area clear above)     │   Top of board
    ├──────────────────────────────┤   (faces up toward sky)
    │                              │
    │  ┌──────┐  ┌──────┐ ┌────┐  │
    │  │ESP32 │  │PN532 │ │GPS │  │   45mm
    │  │S3    │  │      │ │M10S│  │
    │  │MINI  │  │      │ │    │  │
    │  └──────┘  └──────┘ └────┘  │
    │                              │
    │  ┌──────┐  ┌────┐  ○  ○    │
    │  │TP4056│  │IMU │  LED BZR  │
    │  │+ prot│  │6DSO│           │
    │  └──────┘  └────┘           │
    │                              │
    │  [USB-C connector]           │
    ├──────────────────────────────┤
    │  [NFC ANT]  coil antenna     │
    │  (bottom layer copper trace) │   Bottom of board
    └──────────────────────────────┘   (faces toward door NFC reader)
```

### 2.3 Layer Stackup

```
Layer 1 (Top):    Components + signal traces + GPS antenna pad
                  Ground pour (around components, NOT under GPS antenna)
                  I2C bus, UART, GPIO traces

Layer 2 (Bottom): Ground plane (continuous, under GPS antenna for reference)
                  NFC antenna coil (etched copper spiral)
                  Battery connector pads
                  USB-C footprint
```

### 2.4 Critical Layout Rules

1. **GPS antenna keepout**: No copper fill, traces, or components within 5mm above the ceramic patch antenna. Ground plane on Layer 2 directly below provides the required ground reference.

2. **NFC antenna**: Etched spiral on Layer 2, 4 turns, outer dimension ~25mm × 25mm. Tuned to 13.56 MHz with matching capacitors (47pF + 22pF typical). PN532 datasheet has reference designs.

3. **Decoupling capacitors**: 100nF ceramic on each VCC pin, placed within 3mm. 10uF bulk cap near TPS63001 output.

4. **I2C routing**: Keep SDA and SCL traces short (< 20mm total bus length). Route together, not under GPS antenna.

5. **Crystal**: ESP32-S3-MINI-1 has integrated crystal — no external crystal needed.

6. **USB-C**: D+ and D- differential pair, 90Ω impedance, length-matched within 0.5mm.

---

## 3. Enclosure Design

### 3.1 Material: TPU (Thermoplastic Polyurethane)

| Property | Value | Why It Matters |
|----------|-------|----------------|
| Shore hardness | 95A | Flexible enough for collar bending, rigid enough to protect PCB |
| Water resistance | Excellent | IP67 when properly designed |
| Impact resistance | High | Survives drops, dog play, running into objects |
| Temperature range | -40°C to +80°C | Handles all weather |
| UV resistance | Good | Outdoor use without degradation |
| Print method | FDM at 230°C | Standard 3D printer compatible |

### 3.2 Enclosure Dimensions

```
External: 48mm × 33mm × 18mm (includes walls)
Internal: 44mm × 29mm × 14mm (component cavity)
Wall thickness: 2mm (sides), 2mm (top/bottom)
Weight (enclosure only): ~8g
Weight (complete assembly): ~25g
```

### 3.3 Cross-Section View

```
        ── Collar strap ──
       /                   \
      /     Clip mount      \
     │  ┌──────────────────┐ │
     │  │  TPU flex clip    │ │
     │  ├──────────────────┤ │
     │  │                  │ │
     │  │  GPS antenna     │ │  ← Top face (sky-facing)
     │  │  (window in top) │ │
     │  │                  │ │
     │  │  ┌────────────┐  │ │
     │  │  │   PCB      │  │ │  ← Component cavity
     │  │  │ (vertical) │  │ │
     │  │  └────────────┘  │ │
     │  │                  │ │
     │  │  ┌────────────┐  │ │
     │  │  │   LiPo     │  │ │  ← Battery below PCB
     │  │  │  battery   │  │ │
     │  │  └────────────┘  │ │
     │  │                  │ │
     │  │  NFC antenna     │ │  ← Bottom face (dog/door-facing)
     │  │  (window in base)│ │
     │  │                  │ │
     │  ├──────────────────┤ │
     │  │  USB-C port      │ │  ← Side, with rubber flap cover
     │  │  (recessed)      │ │
     │  └──────────────────┘ │
     └────────────────────────┘
```

### 3.4 Sealing Strategy (IP67)

| Area | Seal Method |
|------|-------------|
| Main enclosure join | Tongue-and-groove with silicone gasket |
| USB-C port | TPU rubber flap (hinged, snap-close) |
| GPS antenna window | Thin TPU membrane (RF transparent, waterproof) |
| NFC antenna window | Same as GPS (thin TPU membrane) |
| LED window | Clear epoxy fill (light pipe) |
| Buzzer port | Acoustic membrane (Gore-Tex vent, waterproof + sound-transparent) |

### 3.5 Collar Attachment

```
      ┌────────────────────────┐
      │     Collar strap       │  ← Standard collar (15-25mm width)
      │  ┌──────────────────┐  │
      │  │  TPU flex tab    │  │
      │  │  (loops around   │  │
      │  │   collar strap)  │  │
      │  │                  │  │
      │  │  ┌────────────┐  │  │  ← Snap-lock mechanism
      │  │  │  Enclosure │  │  │    (snap collar tab through slot)
      │  │  │            │  │  │
      │  │  └────────────┘  │  │
      │  └──────────────────┘  │
      │     Collar strap       │
      └────────────────────────┘

Two attachment options:
1. TPU loop-through: Collar strap threads through two slots in the mount
2. TPU clip-on: Snap clip grips collar from one side (easier to attach/remove)
```

---

## 4. Assembly Process

### 4.1 Bill of Assembly

| Step | Operation | Notes |
|------|-----------|-------|
| 1 | PCB fabrication | 2-layer, 1.0mm, ENIG finish, standard lead time |
| 2 | Stencil printing | Solder paste on pad openings |
| 3 | SMD placement | Pick-and-place or manual (low volume) |
| 4 | Reflow solder | Standard lead-free profile (peak 250°C) |
| 5 | PN532 module | If using breakout board: solder header or direct wire |
| 6 | GPS module | Solder MAX-M10S module to footprint |
| 7 | Battery connector | JST-PH 2-pin, hand solder |
| 8 | USB-C connector | Hand solder (12-pin mid-mount) |
| 9 | GPS antenna | Attach ceramic patch with conductive epoxy |
| 10 | Firmware flash | Via USB-C, PlatformIO upload |
| 11 | NVS provisioning | Flash encryption + collar identity via USB |
| 12 | Secure Boot | Burn eFuses (ONE-TIME, irreversible) |
| 13 | Battery install | Connect LiPo to JST connector |
| 14 | Enclosure assembly | Place PCB + battery in enclosure, seal |
| 15 | Waterproof test | Submersion test (1m, 30min) |
| 16 | Functional test | GPS fix, BLE advertising, NFC handshake, buzzer, LED |

### 4.2 Programming & Provisioning Station

```bash
# Step 1: Flash firmware
cd firmware/collar
pio run -e collar --target upload

# Step 2: Enable flash encryption (first time only)
espefuse.py burn_efuse FLASH_CRYPT_CNT 0x01

# Step 3: Enable Secure Boot v2 (first time only)
espsecure.py generate_signing_key collar_key.pem
espefuse.py burn_key BLOCK_KEY0 collar_key.pem

# Step 4: Write provisioning data to NVS
python3 provision_collar.py \
    --collar-id "$(openssl rand -hex 16)" \
    --shared-secret "$(openssl rand -hex 32)" \
    --wifi-ssid "FactoryTest" \
    --wifi-password "test1234" \
    --port /dev/ttyUSB0

# Step 5: Verify
python3 verify_collar.py --port /dev/ttyUSB0
# → Checks: BLE advertising, NFC emulation, GPS module detection, IMU response
```

---

## 5. Design Files (to be created)

| File | Format | Tool | Purpose |
|------|--------|------|---------|
| collar_schematic.kicad_sch | KiCad 7 | KiCad | Full circuit schematic |
| collar_pcb.kicad_pcb | KiCad 7 | KiCad | PCB layout |
| collar_bom.csv | CSV | KiCad BOM plugin | Component sourcing |
| collar_gerber.zip | Gerber | KiCad export | PCB fabrication files |
| collar_enclosure.step | STEP | FreeCAD/Fusion360 | 3D enclosure model |
| collar_enclosure.stl | STL | FreeCAD/Fusion360 | 3D print file |
| collar_assembly.pdf | PDF | — | Assembly instructions |

---

## 6. Cost Analysis (Per Unit, Low Volume)

| Category | Item | Qty 1 | Qty 100 |
|----------|------|-------|---------|
| PCB | 2-layer, 45x30mm | $5 | $0.50 |
| Components | BOM total | $38 | $28 |
| Assembly | Manual (1hr) | $15 | — |
| Assembly | Machine (100 units) | — | $3 |
| Enclosure | 3D printed TPU | $5 | $2 |
| Battery | 500mAh LiPo | $5 | $3 |
| Packaging | Box + cable | $2 | $1 |
| **Total** | | **~$70** | **~$37.50** |

At 100+ units, the collar device costs ~$38 to manufacture — comparable to commercial GPS pet trackers ($30-80 retail without subscription).
