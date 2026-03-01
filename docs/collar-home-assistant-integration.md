# Collar System — Home Assistant & MQTT Integration

## Overview

Integration with Home Assistant via MQTT, enabling smart home automations triggered by collar events (geofence breach, door access, battery low, dog activity).

---

## 1. Architecture

```
Collar (ESP32-S3)                 .NET API                    MQTT Broker
    │                                │                            │
    │── WiFi upload ────────────────▶│                            │
    │   (GPS, events)                │── Publish ────────────────▶│
    │                                │   collar/luna/location     │
    │                                │   collar/luna/battery      │
    │                                │   collar/luna/geofence     │
    │                                │   door/access              │
    │                                │                            │
    │                                │                            │
    │                                │                     Home Assistant
    │                                │                            │
    │                                │◀── Subscribe ──────────────│
    │                                │   collar/+/command         │
    │                                │                            │
    │◀── BLE command ────────────────│                            │
    │   (buzz, locate)               │                            │
```

### Why MQTT (not direct Home Assistant API)?

- MQTT is the standard IoT protocol; Home Assistant has native MQTT discovery
- Decouples the SDD API from Home Assistant — works with any MQTT consumer
- Low latency (< 100ms for local broker)
- Supports retain flags for last-known state
- Broker can be Mosquitto (already common in HA setups)

---

## 2. MQTT Topic Hierarchy

```
smart-dog-door/
├── collar/
│   ├── {collar_name}/
│   │   ├── location        # GPS position (retained)
│   │   ├── battery          # Battery status (retained)
│   │   ├── activity         # Activity state (retained)
│   │   ├── geofence         # Geofence status (retained)
│   │   ├── status           # Online/offline (retained, LWT)
│   │   └── command          # Commands (subscribe)
│   └── discovery/           # HA MQTT auto-discovery
│       ├── device_tracker/
│       ├── sensor/
│       └── binary_sensor/
├── door/
│   ├── {door_name}/
│   │   ├── access           # Access events
│   │   ├── status           # Door open/closed (retained)
│   │   └── command          # Manual open/close
│   └── discovery/
└── system/
    └── status               # API online/offline (LWT)
```

---

## 3. Message Payloads

### 3.1 Location Update

**Topic:** `smart-dog-door/collar/luna/location`
**Retain:** Yes
**QoS:** 0 (best effort — high frequency, loss acceptable)

```json
{
    "latitude": 33.44842,
    "longitude": -112.07395,
    "altitude": 337.2,
    "accuracy": 2.5,
    "speed": 1.2,
    "heading": 45.0,
    "satellites": 12,
    "source": "gps",
    "timestamp": "2026-02-28T15:45:00Z"
}
```

### 3.2 Battery Status

**Topic:** `smart-dog-door/collar/luna/battery`
**Retain:** Yes
**QoS:** 1 (at least once)

```json
{
    "percentage": 78.5,
    "voltage": 3.92,
    "charging": false,
    "estimatedHoursRemaining": 96,
    "state": "normal",
    "timestamp": "2026-02-28T15:45:00Z"
}
```

`state`: "normal", "low" (< 15%), "critical" (< 5%), "charging"

### 3.3 Activity State

**Topic:** `smart-dog-door/collar/luna/activity`
**Retain:** Yes
**QoS:** 0

```json
{
    "state": "walking",
    "stepsToday": 3204,
    "distanceTodayM": 1523.4,
    "activeMinutesToday": 47,
    "timestamp": "2026-02-28T15:45:00Z"
}
```

`state`: "idle", "walking", "running", "sleeping"

### 3.4 Geofence Status

**Topic:** `smart-dog-door/collar/luna/geofence`
**Retain:** Yes
**QoS:** 1

```json
{
    "inBounds": true,
    "currentZone": "Backyard",
    "nearestFence": "Pool",
    "distanceToFenceM": 15.2,
    "breachActive": false,
    "breachesToday": 0,
    "timestamp": "2026-02-28T15:45:00Z"
}
```

On breach:
```json
{
    "inBounds": false,
    "currentZone": null,
    "breachedFence": "Pool",
    "distanceToFenceM": 0,
    "breachActive": true,
    "breachStartedAt": "2026-02-28T15:44:30Z",
    "breachDurationSeconds": 30,
    "breachesToday": 1,
    "latitude": 33.44842,
    "longitude": -112.07395,
    "timestamp": "2026-02-28T15:45:00Z"
}
```

### 3.5 Collar Status (Online/Offline)

**Topic:** `smart-dog-door/collar/luna/status`
**Retain:** Yes
**QoS:** 1
**Last Will and Testament (LWT):** `"offline"` (set when MQTT client connects)

```json
"online"
```

Or on disconnect (LWT):
```json
"offline"
```

### 3.6 Door Access Event

**Topic:** `smart-dog-door/door/front/access`
**Retain:** No
**QoS:** 1

```json
{
    "event": "access_granted",
    "animalName": "Luna",
    "animalId": 5,
    "direction": "entering",
    "confidenceScore": 0.92,
    "identificationMethod": "both",
    "cameraConfidence": 0.77,
    "collarConfidence": 0.95,
    "timestamp": "2026-02-28T15:45:00Z"
}
```

### 3.7 Command (Home Assistant → API → Collar)

**Topic:** `smart-dog-door/collar/luna/command`
**QoS:** 1

```json
{
    "command": "buzz",
    "duration": 10
}
```

Commands: `buzz`, `locate` (30s buzz), `sleep`, `wake`

---

## 4. Home Assistant MQTT Auto-Discovery

The API publishes MQTT discovery messages so Home Assistant automatically creates entities.

### 4.1 Device Tracker (GPS Location)

**Topic:** `homeassistant/device_tracker/sdd_collar_luna/config`

```json
{
    "name": "Luna's Collar",
    "unique_id": "sdd_collar_luna_tracker",
    "state_topic": "smart-dog-door/collar/luna/status",
    "json_attributes_topic": "smart-dog-door/collar/luna/location",
    "payload_home": "online",
    "payload_not_home": "offline",
    "source_type": "gps",
    "device": {
        "identifiers": ["sdd_collar_luna"],
        "name": "Luna's Smart Collar",
        "manufacturer": "Smart Dog Door",
        "model": "SDD Collar v1",
        "sw_version": "1.0.0"
    }
}
```

### 4.2 Battery Sensor

**Topic:** `homeassistant/sensor/sdd_collar_luna_battery/config`

```json
{
    "name": "Luna Collar Battery",
    "unique_id": "sdd_collar_luna_battery",
    "state_topic": "smart-dog-door/collar/luna/battery",
    "value_template": "{{ value_json.percentage }}",
    "unit_of_measurement": "%",
    "device_class": "battery",
    "json_attributes_topic": "smart-dog-door/collar/luna/battery",
    "device": {
        "identifiers": ["sdd_collar_luna"]
    }
}
```

### 4.3 Geofence Binary Sensor

**Topic:** `homeassistant/binary_sensor/sdd_collar_luna_geofence/config`

```json
{
    "name": "Luna In Bounds",
    "unique_id": "sdd_collar_luna_geofence",
    "state_topic": "smart-dog-door/collar/luna/geofence",
    "value_template": "{{ value_json.inBounds }}",
    "payload_on": true,
    "payload_off": false,
    "device_class": "safety",
    "json_attributes_topic": "smart-dog-door/collar/luna/geofence",
    "device": {
        "identifiers": ["sdd_collar_luna"]
    }
}
```

### 4.4 Activity Sensor

**Topic:** `homeassistant/sensor/sdd_collar_luna_activity/config`

```json
{
    "name": "Luna Activity",
    "unique_id": "sdd_collar_luna_activity",
    "state_topic": "smart-dog-door/collar/luna/activity",
    "value_template": "{{ value_json.state }}",
    "json_attributes_topic": "smart-dog-door/collar/luna/activity",
    "icon": "mdi:dog-side",
    "device": {
        "identifiers": ["sdd_collar_luna"]
    }
}
```

### 4.5 Door Access Event Sensor

**Topic:** `homeassistant/sensor/sdd_door_front_access/config`

```json
{
    "name": "Front Door Last Access",
    "unique_id": "sdd_door_front_access",
    "state_topic": "smart-dog-door/door/front/access",
    "value_template": "{{ value_json.animalName }} {{ value_json.direction }}",
    "json_attributes_topic": "smart-dog-door/door/front/access",
    "icon": "mdi:door",
    "device": {
        "identifiers": ["sdd_door_front"],
        "name": "Smart Dog Door (Front)",
        "manufacturer": "Smart Dog Door",
        "model": "SDD Door Unit v1"
    }
}
```

### 4.6 Find My Dog Button

**Topic:** `homeassistant/button/sdd_collar_luna_buzz/config`

```json
{
    "name": "Find Luna",
    "unique_id": "sdd_collar_luna_buzz",
    "command_topic": "smart-dog-door/collar/luna/command",
    "payload_press": "{\"command\":\"locate\",\"duration\":30}",
    "icon": "mdi:bell-ring",
    "device": {
        "identifiers": ["sdd_collar_luna"]
    }
}
```

---

## 5. Home Assistant Automations

### 5.1 Flash Lights on Geofence Breach

```yaml
automation:
  - alias: "Dog Left Yard - Flash Lights"
    trigger:
      - platform: state
        entity_id: binary_sensor.luna_in_bounds
        from: "on"
        to: "off"
    action:
      - service: light.turn_on
        target:
          entity_id: light.backyard_floodlight
        data:
          flash: long
      - service: notify.mobile_app
        data:
          title: "Luna left the yard!"
          message: >
            Luna breached the {{ state_attr('binary_sensor.luna_in_bounds', 'breachedFence') }}
            fence at {{ now().strftime('%H:%M') }}.
          data:
            priority: high
            channel: dog_alert
```

### 5.2 Announce Door Access on Smart Speaker

```yaml
automation:
  - alias: "Announce Dog Door Access"
    trigger:
      - platform: mqtt
        topic: "smart-dog-door/door/front/access"
    condition:
      - condition: template
        value_template: "{{ trigger.payload_json.event in ['access_granted', 'exit_granted'] }}"
    action:
      - service: tts.google_translate_say
        target:
          entity_id: media_player.kitchen_speaker
        data:
          message: >
            {{ trigger.payload_json.animalName }} is
            {{ 'coming inside' if trigger.payload_json.direction == 'entering' else 'going outside' }}.
```

### 5.3 Low Battery Reminder

```yaml
automation:
  - alias: "Collar Battery Low Reminder"
    trigger:
      - platform: numeric_state
        entity_id: sensor.luna_collar_battery
        below: 15
    action:
      - service: persistent_notification.create
        data:
          title: "Collar Battery Low"
          message: "Luna's collar is at {{ states('sensor.luna_collar_battery') }}%. Please charge it."
```

### 5.4 Track Dog in Home Assistant Map

```yaml
# configuration.yaml
zone:
  - name: Backyard
    latitude: 33.44842
    longitude: -112.07395
    radius: 25
    icon: mdi:grass

  - name: Pool Area
    latitude: 33.44842
    longitude: -112.07395
    radius: 5
    icon: mdi:pool
```

The `device_tracker.sdd_collar_luna` entity automatically appears on the Home Assistant map card with real-time position updates.

### 5.5 Dog Activity Dashboard (Lovelace)

```yaml
# Lovelace card configuration
type: vertical-stack
cards:
  - type: map
    entities:
      - device_tracker.sdd_collar_luna
      - device_tracker.sdd_collar_max
    default_zoom: 19

  - type: horizontal-stack
    cards:
      - type: entity
        entity: sensor.luna_collar_battery
        name: Luna Battery
        icon: mdi:battery
      - type: entity
        entity: sensor.luna_activity
        name: Luna Activity
        icon: mdi:dog-side
      - type: entity
        entity: binary_sensor.luna_in_bounds
        name: In Bounds

  - type: history-graph
    entities:
      - entity: sensor.luna_activity
    hours_to_show: 24

  - type: logbook
    entities:
      - sensor.sdd_door_front_access
    hours_to_show: 24
```

---

## 6. API MQTT Publisher Implementation

### 6.1 NuGet Package

```xml
<PackageReference Include="MQTTnet" Version="4.3.1" />
<PackageReference Include="MQTTnet.Extensions.ManagedClient" Version="4.3.1" />
```

### 6.2 MQTT Service

```csharp
// Services/MqttPublisherService.cs

public interface IMqttPublisherService
{
    Task PublishCollarLocation(CollarDevice collar, LocationPoint point);
    Task PublishCollarBattery(CollarDevice collar);
    Task PublishCollarActivity(CollarDevice collar, ActivityData activity);
    Task PublishGeofenceStatus(CollarDevice collar, GeofenceStatus status);
    Task PublishDoorAccess(DoorEvent doorEvent);
    Task PublishCollarStatus(CollarDevice collar, bool online);
}

public class MqttPublisherService : IMqttPublisherService, IHostedService
{
    private readonly IManagedMqttClient _client;
    private readonly ILogger<MqttPublisherService> _logger;
    private readonly MqttConfig _config;

    public MqttPublisherService(IOptions<MqttConfig> config, ILogger<MqttPublisherService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _client = new MqttFactory().CreateManagedMqttClient();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_config.Enabled) return;

        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer(_config.Host, _config.Port)
                .WithCredentials(_config.Username, _config.Password)
                .WithWillTopic("smart-dog-door/system/status")
                .WithWillPayload("offline")
                .WithWillRetain(true)
                .Build())
            .Build();

        await _client.StartAsync(options);

        // Publish online status
        await PublishRetained("smart-dog-door/system/status", "\"online\"");

        // Publish MQTT discovery messages for all active collars
        await PublishDiscoveryMessages();

        _logger.LogInformation("MQTT publisher connected to {Host}:{Port}", _config.Host, _config.Port);
    }

    public async Task PublishCollarLocation(CollarDevice collar, LocationPoint point)
    {
        var topic = $"smart-dog-door/collar/{Slugify(collar.Name)}/location";
        var payload = JsonSerializer.Serialize(new
        {
            latitude = point.Latitude,
            longitude = point.Longitude,
            altitude = point.Altitude,
            accuracy = point.Accuracy,
            speed = point.Speed,
            heading = point.Heading,
            satellites = point.Satellites,
            source = "gps",
            timestamp = point.Timestamp
        });

        await PublishRetained(topic, payload);
    }

    public async Task PublishGeofenceStatus(CollarDevice collar, GeofenceStatus status)
    {
        var topic = $"smart-dog-door/collar/{Slugify(collar.Name)}/geofence";
        var payload = JsonSerializer.Serialize(status);
        await Publish(topic, payload, MqttQualityOfServiceLevel.AtLeastOnce, retain: true);
    }

    public async Task PublishDoorAccess(DoorEvent doorEvent)
    {
        var topic = "smart-dog-door/door/main/access";
        var payload = JsonSerializer.Serialize(new
        {
            @event = doorEvent.EventType.ToString(),
            animalName = doorEvent.Animal?.Name,
            animalId = doorEvent.AnimalId,
            direction = doorEvent.TransitDirection.ToString(),
            confidenceScore = doorEvent.ConfidenceScore,
            identificationMethod = doorEvent.IdentificationMethod,
            cameraConfidence = doorEvent.CameraConfidence,
            collarConfidence = doorEvent.CollarConfidence,
            timestamp = doorEvent.Timestamp
        });

        await Publish(topic, payload, MqttQualityOfServiceLevel.AtLeastOnce, retain: false);
    }

    private async Task PublishRetained(string topic, string payload)
    {
        await Publish(topic, payload, MqttQualityOfServiceLevel.AtMostOnce, retain: true);
    }

    private async Task Publish(string topic, string payload, MqttQualityOfServiceLevel qos, bool retain)
    {
        if (!_config.Enabled || !_client.IsConnected) return;

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .WithRetainFlag(retain)
            .Build();

        await _client.EnqueueAsync(message);
    }

    private string Slugify(string name) =>
        name?.ToLower().Replace(" ", "_").Replace("'", "") ?? "unknown";
}
```

### 6.3 Configuration

```json
// appsettings.json
{
    "Mqtt": {
        "Enabled": false,
        "Host": "localhost",
        "Port": 1883,
        "Username": "",
        "Password": "",
        "TopicPrefix": "smart-dog-door",
        "PublishDiscovery": true
    }
}
```

```yaml
# helm/dog-door/values.yaml
mqtt:
  enabled: false
  host: mosquitto.default.svc.cluster.local
  port: 1883
  # username and password in secrets
```

### 6.4 Integration Points

The MQTT publisher hooks into existing services:

```csharp
// In LocationService.cs, after storing GPS points:
await _mqttPublisher.PublishCollarLocation(collar, latestPoint);

// In DoorService.cs, after logging access event:
await _mqttPublisher.PublishDoorAccess(doorEvent);

// In GeofenceService.cs, after processing breach event:
await _mqttPublisher.PublishGeofenceStatus(collar, geofenceStatus);

// In CollarHealthMonitorService.cs, on status change:
await _mqttPublisher.PublishCollarStatus(collar, isOnline);
```

---

## 7. Command Flow (Home Assistant → Collar)

```
Home Assistant                 MQTT Broker              .NET API                  Collar
    │                              │                       │                        │
    │── Press "Find Luna" ────────▶│                       │                        │
    │   button entity               │                       │                        │
    │                              │ smart-dog-door/        │                        │
    │                              │ collar/luna/command     │                        │
    │                              │───────────────────────▶│                        │
    │                              │                       │── Lookup collar         │
    │                              │                       │   by name               │
    │                              │                       │                        │
    │                              │                       │── Queue BLE command ───▶│
    │                              │                       │   (via next WiFi burst  │
    │                              │                       │    or direct BLE)       │
    │                              │                       │                        │
    │                              │                       │                   Buzzer sounds!
    │                              │                       │                        │
```

The API subscribes to `smart-dog-door/collar/+/command` and translates MQTT commands to the appropriate delivery mechanism (WiFi response during next upload, or BLE if door unit is in range).

---

## 8. Advanced Automations

### 8.1 Dog Is Outside Too Long

```yaml
automation:
  - alias: "Dog Outside Too Long"
    trigger:
      - platform: state
        entity_id: device_tracker.sdd_collar_luna
        to: "not_home"
        for:
          minutes: 30
    condition:
      - condition: state
        entity_id: weather.home
        state: "rainy"
    action:
      - service: notify.mobile_app
        data:
          title: "Luna is still outside"
          message: "Luna has been outside for 30 minutes and it's raining."
```

### 8.2 Auto-Lock Dog Door at Night

```yaml
automation:
  - alias: "Lock Dog Door at Night"
    trigger:
      - platform: time
        at: "22:00:00"
    condition:
      - condition: state
        entity_id: device_tracker.sdd_collar_luna
        state: "home"
    action:
      - service: mqtt.publish
        data:
          topic: "smart-dog-door/door/main/command"
          payload: '{"command":"lock"}'
      - service: notify.mobile_app
        data:
          message: "Dog door locked for the night. Luna is inside."
```

### 8.3 Welcome Home Routine

```yaml
automation:
  - alias: "Dog Came Inside - Welcome"
    trigger:
      - platform: mqtt
        topic: "smart-dog-door/door/front/access"
    condition:
      - condition: template
        value_template: "{{ trigger.payload_json.direction == 'entering' }}"
    action:
      - service: light.turn_on
        target:
          entity_id: light.mudroom
      - delay:
          minutes: 5
      - service: light.turn_off
        target:
          entity_id: light.mudroom
```
