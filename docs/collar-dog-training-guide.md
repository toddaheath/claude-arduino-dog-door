# Geofence Buzzer — Dog Training & Conditioning Guide

## Overview

The collar's buzzer serves as an auditory boundary signal for the dog. Proper conditioning ensures the dog associates the beep with "turn back" rather than experiencing fear or anxiety. This guide covers positive reinforcement training techniques.

**Important:** The buzzer is a training aid, not a punishment device. It works best when paired with positive reinforcement. Without proper training, the buzzer alone will not reliably keep a dog within boundaries.

---

## 1. Training Philosophy

### 1.1 How It Works

The collar buzzer provides three progressive audio signals:

| Signal | Sound | Meaning | When |
|--------|-------|---------|------|
| **Warning beep** | Short double-beep (100ms × 2) | "You're getting close to the boundary" | Within 5m of fence edge |
| **Breach alert** | Longer triple-beep (500ms × 3) | "You've crossed the boundary, come back" | Crossed fence boundary |
| **Continuous tone** | 2s on / 1s off repeating | "You're still out of bounds" | > 30s in breach |

**The buzzer is NOT an electric shock.** It produces only sound (max 85 dB at 1m, about as loud as a city bus). There is no physical correction. The goal is for the dog to learn through association that the beep means "good things are inside the boundary, boring/nothing out here."

### 1.2 Positive Reinforcement Principle

```
Dog approaches boundary → Warning beep → Dog turns around → TREAT + PRAISE
                                                            ^^^^^^^^^^^^^^^^
                                                            This is the key part
```

The dog learns: "When I hear the beep and turn back, I get rewarded." Over time, the behavior becomes automatic.

---

## 2. Training Protocol

### Phase 1: Introduction (Days 1-3)

**Goal:** Dog associates the buzzer sound with treats (no boundary context yet).

1. Hold the collar (not on the dog)
2. Play the warning beep (via SPA or BLE command)
3. Immediately give a high-value treat
4. Repeat 10-15 times per session, 2-3 sessions per day

**What to use:** Cheese, hot dog, chicken — whatever your dog values most.

```
[BEEP] → [TREAT] → [BEEP] → [TREAT] → [BEEP] → [TREAT]
```

After 3 days, the dog should perk up and look at you expectantly when hearing the beep. This is classical conditioning (Pavlovian association).

### Phase 2: Leashed Boundary Walk (Days 4-7)

**Goal:** Dog learns that the beep means "turn around."

1. Put the collar on the dog (powered on, geofencing enabled)
2. Walk the dog on a long leash (15-20 feet) near the boundary
3. As the dog approaches the boundary:
   - Warning beep sounds automatically (5m from edge)
   - Say "come back" or your chosen recall cue
   - Gently guide the dog back with the leash
   - Reward with treat + praise when they turn back
4. Repeat along the entire boundary perimeter
5. Do 2-3 sessions per day, 10-15 minutes each

**Key:** Never drag the dog or use the leash as punishment. The leash prevents the dog from crossing while they learn. You're showing them where the boundary is.

### Phase 3: Long Line Freedom (Days 8-14)

**Goal:** Dog begins self-correcting on hearing the beep.

1. Use a 30-50 foot long line (lightweight)
2. Let the dog explore naturally
3. When the warning beep sounds:
   - Wait 3 seconds for the dog to self-correct
   - If they turn back on their own: BIG reward (jackpot treat + happy voice)
   - If they continue: gently guide back, reward when they comply
4. Gradually increase the pause before intervening
5. Most dogs begin self-correcting by day 10-12

### Phase 4: Supervised Off-Leash (Days 15-21)

**Goal:** Dog reliably turns back at the warning beep without leash guidance.

1. Remove the long line
2. Stay outside with the dog, watching
3. If the dog approaches the boundary:
   - Warning beep → dog turns back → praise + treat
   - If dog ignores warning and crosses → breach alert sounds → call the dog back → reward for returning (do NOT punish)
4. If the dog frequently ignores the breach alert, go back to Phase 3

**Success criteria:** Dog turns back at the warning beep 90%+ of the time without your intervention.

### Phase 5: Unsupervised (Day 22+)

**Goal:** Dog respects boundaries independently.

1. Let the dog outside unsupervised for short periods (15-30 minutes)
2. Monitor via the SPA live map
3. Check geofence event log for any breaches
4. Gradually increase unsupervised time
5. Continue occasional reinforcement (treat when they come in, praise for staying in bounds)

**Maintenance:** Even after training is complete, periodically reinforce the behavior. Give the dog a treat for coming inside. Acknowledge good behavior. The training is never truly "done" — it's a habit that needs occasional reinforcement.

---

## 3. Buzzer Sound Configuration

The SPA allows customizing buzzer patterns per geofence:

| Fence | Recommended Buzzer | Why |
|-------|-------------------|-----|
| Yard boundary (allow) | Short double-beep | Gentle reminder, frequently heard |
| Pool (deny) | Long triple-beep | More urgent, less frequent |
| Road/street (deny) | Continuous | Most dangerous zone, strongest signal |
| Garden bed (deny) | Short double-beep | Minor restriction, gentle tone |

**Customization in SPA:**
```
/map/geofences → Select fence → Buzzer Pattern dropdown:
  - Silent (monitoring only, no buzzer)
  - Short (100ms double-beep) — recommended for most fences
  - Long (500ms triple-beep) — for important restrictions
  - Continuous (2s on/1s off) — for dangerous areas only
```

**Volume control:** The firmware limits the buzzer to 85 dB maximum. For sensitive dogs, the owner can reduce volume via the collar BLE config characteristic. Minimum effective volume: ~60 dB (about normal conversation level).

---

## 4. Breed Considerations

Different breeds respond differently to auditory signals:

| Breed Group | Typical Response | Training Notes |
|------------|-----------------|----------------|
| Herding (Border Collie, Aussie) | Very responsive, learn quickly | May need only 7-10 days |
| Sporting (Lab, Golden) | Moderate, food-motivated | Standard 21-day protocol works well |
| Hound (Beagle, Basset) | Distracted by scent, may ignore | Use stronger buzzer pattern, higher-value treats |
| Terrier (Jack Russell, Fox) | Strong prey drive, may test limits | Expect longer training (28+ days), extra Phase 3 time |
| Guardian (Akita, Great Pyrenees) | Independent, may dismiss buzzer | May need professional trainer assistance |
| Toy (Chihuahua, Pomeranian) | Sensitive to sound, may over-react | Use lowest volume, gentle short beeps only |
| Brachycephalic (Bulldog, Pug) | Generally compliant | Standard protocol, watch for overheating outdoors |

**Anxiety-prone dogs:** If the dog shows signs of stress (cowering, tail between legs, panting, avoiding the collar), reduce the buzzer volume, go slower through the phases, and consider consulting a certified dog behaviorist (CAAB or CPDT-KA).

---

## 5. Troubleshooting

### 5.1 Dog Ignores the Buzzer

**Cause:** Insufficient Phase 1 conditioning, or buzzer not audible over environmental noise.

**Fix:**
- Return to Phase 1: re-establish buzzer → treat association
- Check buzzer volume (may be too quiet for outdoor use)
- Use higher-value treats (real meat, not kibble)
- Ensure the buzzer frequency (2000-4000 Hz) is within the dog's hearing sensitivity (dogs hear best at 2000-4000 Hz — this is already optimized)

### 5.2 Dog Is Afraid of the Buzzer

**Cause:** Volume too high, or negative association formed.

**Fix:**
- Reduce buzzer volume to minimum (60 dB)
- Switch to "silent" mode temporarily
- Re-do Phase 1 at very low volume, gradually increasing over 2 weeks
- Pair buzzer with play sessions (not just treats)
- If anxiety persists, disable the buzzer and rely on owner monitoring + push notifications only

### 5.3 Dog Blasts Through the Boundary (High Prey Drive)

**Cause:** Chase instinct overrides learned behavior (squirrel, cat, another dog).

**Fix:**
- This is a limitation — no auditory signal can override a strong chase instinct
- Use the buzzer as an alert system (notifies owner) rather than relying on it as a barrier
- Train a strong emergency recall ("come NOW") separately
- Consider physical fencing for high-drive dogs (geofence is supplementary, not primary containment)

### 5.4 Dog Learns the Boundary Exactly and Dances on It

**Cause:** Smart dog figured out the exact boundary location and plays near the edge.

**Fix:**
- This is actually a success! The dog knows exactly where the boundary is.
- Adjust the warning distance (default 5m) to push the effective boundary inward
- Accept that some dogs will "test" the boundary as a game
- As long as they don't cross into breach, this behavior is harmless

---

## 6. Important Safety Notes

1. **The collar buzzer is NOT a replacement for physical fencing** in areas with traffic, aggressive animals, or other immediate dangers. Use it as a secondary layer.

2. **Never use the buzzer as punishment.** If the dog crosses the boundary, recall them and reward the return. Punishment creates fear, not learning.

3. **Supervise new dogs for at least 3 weeks** before trusting the boundary training. Even well-trained dogs can have off days.

4. **Remove the collar for swimming, bathing, and sleeping.** The IP67 rating handles rain, not submersion. Also, continuous collar wearing can cause skin irritation.

5. **Check collar fit regularly.** Two fingers should fit between the collar device and the dog's neck. Too tight = discomfort. Too loose = bouncing that causes inaccurate IMU readings.

6. **Monitor for wildlife.** If wildlife regularly triggers the door camera, consider adjusting the geofence to keep your dog away from areas where wildlife congregates (bird feeders, compost, etc.).

---

## 7. Configurable Training Parameters

These settings in `config.h` / collar BLE config can be adjusted for training:

```c
// Warning distance (how far from boundary to start beeping)
#define GEOFENCE_WARNING_DIST_M   5.0    // Default: 5m
// Increase to 8-10m for dogs in early training (more warning time)
// Decrease to 2-3m for well-trained dogs (less nuisance beeping)

// Buzzer volume (PWM duty cycle)
#define BUZZER_MAX_DUTY          128     // Default: 50% duty (moderate)
// Reduce to 64 for sensitive dogs (25% duty = quieter)
// Max 200 for outdoor environments with ambient noise

// Buzzer frequency
#define BUZZER_FREQ_WARNING      2700    // Hz (sharp, attention-getting)
#define BUZZER_FREQ_BREACH       2200    // Hz (lower, more urgent tone)
#define BUZZER_FREQ_CONTINUOUS   2500    // Hz (mid-range, sustained)
// These are tuned for peak dog hearing sensitivity (2000-4000 Hz)

// Warning repeat interval
#define WARNING_INTERVAL_MS      5000    // Default: one warning every 5s
// Increase to 10000 for less frequent warnings (less nagging)
// Decrease to 3000 for faster feedback during training

// Breach escalation timing
#define BREACH_ESCALATE_1_MS     10000   // First escalation: 10s after breach
#define BREACH_ESCALATE_2_MS     30000   // Second escalation: 30s
#define BREACH_ESCALATE_3_MS     60000   // Third escalation: 60s
// Longer intervals = gentler escalation, good for training
// Shorter intervals = more urgent, for dangerous zones
```

All these are configurable via the SPA collar settings page and pushed to the collar via BLE or WiFi.
