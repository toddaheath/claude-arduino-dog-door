# System Architecture

## Overview

The Smart Dog Door is a multi-tier IoT system that uses edge AI to detect and identify dogs, automatically controlling a physical door mechanism.

> **Interactive version**: Open [architecture-animation.html](architecture-animation.html) in a browser to see an animated walkthrough of the full data flow.

## System Diagram

```mermaid
graph TB
    subgraph "Edge Device (ESP32-CAM)"
        RADAR[RCWL-0516 Radar]
        US[HC-SR04 Ultrasonic]
        IR[IR Break Beam]
        CAM[OV2640 Camera]
        TFLITE[TFLite Micro Model]
        ACT[Linear Actuator]
        REED[Reed Switch]

        RADAR --> |motion detected| CAM
        US --> |proximity confirmed| CAM
        CAM --> TFLITE
        TFLITE --> |dog detected| API_CLIENT
        API_CLIENT --> |allow/deny| ACT
        IR --> |safety interlock| ACT
        REED --> |door position| ACT
    end

    subgraph "Backend Server"
        API[.NET Web API]
        DB[(PostgreSQL)]
        FS[File Storage]

        API --> DB
        API --> FS
    end

    subgraph "Admin Interface"
        SPA[React SPA]
    end

    API_CLIENT[API Client] --> |HTTPS| API
    SPA --> |HTTPS| API
```

## Data Flow

```mermaid
sequenceDiagram
    participant S as Sensors
    participant ESP as ESP32-CAM
    participant API as .NET API
    participant DB as PostgreSQL
    participant FS as File Storage
    participant SPA as React SPA

    S->>ESP: Motion + proximity detected
    ESP->>ESP: Capture camera image

    ESP->>API: POST /api/v1/doors/approach-photo (image, side, apiKey)
    API->>FS: Save image to uploads/approach/
    API->>DB: Log AnimalApproach event (with image path + side)

    ESP->>ESP: TFLite: dog vs not-dog

    alt Dog Detected
        ESP->>API: POST /api/v1/doors/access-request (image, side, apiKey)
        API->>FS: Save image to uploads/events/
        API->>API: Determine direction from side (inside→exiting, outside→entering)
        API->>API: pHash compare with stored animal photos
        API->>DB: Log access event (Granted/Denied/Unknown, with image path)

        alt Known Dog (Allowed)
            API-->>ESP: 200 OK {allow: true, animalId: 1, direction: "entering"}
            ESP->>ESP: Open door actuator
            ESP->>ESP: Wait for IR beam clear
            ESP->>ESP: Close door actuator
        else Unknown / Denied
            API-->>ESP: 200 OK {allow: false, direction: "entering"}
            ESP->>ESP: Flash deny LED
        end
    else Not a Dog
        ESP->>ESP: Return to sensing (approach photo already logged)
    end

    SPA->>API: GET /api/v1/accesslogs
    API->>DB: Query events (includes imageUrl)
    API-->>SPA: Return event list with imageUrl for each entry
    SPA->>API: GET /uploads/approach/{guid}.jpg
    API-->>SPA: Serve JPEG from filesystem
```

## Component Details

### ESP32-CAM (Edge Device)
- **Sensors**: RCWL-0516 radar detects motion at range, HC-SR04 confirms proximity, IR break beam provides safety interlock
- **Camera**: OV2640 captures 320x240 JPEG frames
- **ML Inference**: TensorFlow Lite Micro runs a quantized MobileNet-based dog detector
- **Actuator**: 12V linear actuator controlled via L298N motor driver
- **Safety**: Reed switch monitors door position, IR beam prevents closing on animal

### .NET Web API
- **Framework**: ASP.NET Core 8.0 with Entity Framework Core 9.x
- **Database**: PostgreSQL via Npgsql EF Core provider 9.x
- **Recognition**: Perceptual hashing (pHash) compares camera images to stored animal profile photos
- **Direction Detection**: Dual-sided cameras report which side (inside/outside) triggered the request; the API infers transit direction (entering/exiting)
- **Storage**: Photos stored on filesystem (`uploads/`), paths tracked in database

### React Admin SPA
- **Stack**: React 18, TypeScript, Vite
- **Features**: Animal management, photo upload, access log viewer, door configuration
- **Demo Mode**: Supports `VITE_DEMO_MODE=true` for GitHub Pages deployment (uses HashRouter)
- **Deployment**: Static build served by nginx

### Infrastructure
- **Docker Compose**: Local development with API + SPA + PostgreSQL
- **Helm/Kubernetes**: Production deployment with configurable replicas, ingress, secrets

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/animals | List all animals |
| POST | /api/animals | Create animal |
| GET | /api/animals/{id} | Get animal details |
| PUT | /api/animals/{id} | Update animal |
| DELETE | /api/animals/{id} | Delete animal |
| GET | /api/animals/{id}/photos | List animal photos |
| POST | /api/photos/upload/{animalId} | Upload photo |
| DELETE | /api/photos/{id} | Delete photo |
| POST | /api/v1/doors/approach-photo | Upload approach image for any motion detection (no-auth, apiKey in form) |
| POST | /api/v1/doors/access-request | Request door access — image + optional side + apiKey |
| POST | /api/v1/doors/firmware-event | Post firmware event (door opened/closed, power events) |
| GET | /api/v1/doors/status | Get door status |
| PUT | /api/v1/doors/configuration | Update door config |
| GET | /api/v1/accesslogs | Query access logs (includes imageUrl per entry) |
| GET | /api/v1/accesslogs/{id} | Get specific log entry |
| GET | /uploads/approach/{file} | Serve approach photo (static file) |
| GET | /uploads/events/{file} | Serve access-request photo (static file) |

## Security Considerations

- API key authentication for ESP32-to-API communication
- CORS configured for SPA origin only
- Photo uploads validated for file type and size
- SQL injection prevented by EF Core parameterized queries
- No sensitive data in Helm values (secrets managed separately)
