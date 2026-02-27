# Smart Dog Door - CLAUDE.md

## Project Overview
Smart dog door system: ESP32-CAM firmware for edge AI dog detection, .NET/C# API with PostgreSQL, React admin SPA, containerized with Docker/Kubernetes/Helm.

## Build & Run Commands

### API (.NET)
```bash
cd src/DogDoor.Api
dotnet restore
dotnet build
dotnet run
# API runs at https://localhost:5001
```

### Tests (79 tests, all passing)
```bash
dotnet test src/DogDoor.Api.Tests
# With coverage:
dotnet test src/DogDoor.Api.Tests --collect:"XPlat Code Coverage"
```

### React SPA
```bash
cd web
npm install
npm run dev    # Dev server at http://localhost:5173
npm run build  # Production build to dist/
npm run lint   # ESLint
```

### Firmware (PlatformIO)
```bash
cd firmware
pio run              # Compile
pio run -t upload    # Flash to ESP32-CAM
pio test             # Run unit tests
```

### Docker
```bash
docker-compose up --build        # Start all services
docker-compose down              # Stop all services
docker-compose up -d postgres    # Start just the database
```

### Helm
```bash
helm template helm/dog-door                    # Render templates
helm install dog-door helm/dog-door            # Install to cluster
helm upgrade dog-door helm/dog-door            # Upgrade
helm uninstall dog-door                        # Remove
```

## Architecture
- **firmware/**: ESP32-CAM PlatformIO project (C++)
- **src/DogDoor.Api/**: ASP.NET Core Web API
- **src/DogDoor.Api.Tests/**: xUnit test project
- **web/**: React + TypeScript + Vite admin SPA
- **helm/dog-door/**: Kubernetes Helm charts
- **docs/**: Architecture and hardware documentation

## Key Conventions
- API uses EF Core with PostgreSQL
- Photos stored on filesystem under `uploads/`, relative paths in DB (resolved at read time)
- ESP32 does on-device dog detection via TFLite Micro
- API does dog identification via perceptual hashing (pHash)
- All timestamps in UTC
- Auth endpoints rate-limited (configurable via `RateLimiting:Auth:PermitLimit`, default 10/min)
- Docker containers run as non-root (`appuser` for API, `nginx` for web)
- Web container listens on port 8080 (non-root can't bind <1024)
- Invitation tokens SHA-256 hashed in DB; password reset tokens BCrypt hashed with TokenPrefix for lookup
- Firmware uses NVS encrypted storage for WiFi credentials (not LittleFS)
- Firmware has 30s hardware watchdog (`esp_task_wdt`) — auto-reboots on hang

## Testing Gotchas
- Test project uses `Microsoft.NET.Sdk` (not Web SDK) — `AddRateLimiter()` not available; use `builder.UseSetting("RateLimiting:Auth:PermitLimit", "10000")` in test factory
- Two test factories: `CustomWebAppFactory` (TestAuthHandler, most tests) and `AuthWebAppFactory` (real JWT auth, auth integration tests)
- SkiaSharp requires `SkiaSharp.NativeAssets.Linux` package for CI runners
