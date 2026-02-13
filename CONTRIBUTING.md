# Contributing to Smart Dog Door

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/)
- [PlatformIO](https://platformio.org/) (for firmware development)
- [Helm 3](https://helm.sh/) (for Kubernetes deployment)

## Local Development Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/toddaheath/claude-arduino-dog-door.git
   cd claude-arduino-dog-door
   ```

2. **Start the database:**
   ```bash
   docker-compose up -d postgres
   ```

3. **Run the API:**
   ```bash
   cd src/DogDoor.Api
   dotnet restore
   dotnet run
   # API available at https://localhost:5001
   ```

4. **Run the web SPA:**
   ```bash
   cd web
   npm install
   npm run dev
   # Dev server at http://localhost:5173
   ```

5. **Or start everything with Docker Compose:**
   ```bash
   docker-compose up --build
   ```

## Running Tests

```bash
# API tests (59 tests)
dotnet test src/DogDoor.Api.Tests

# With coverage
dotnet test src/DogDoor.Api.Tests --collect:"XPlat Code Coverage"

# Web linting
cd web && npm run lint

# Firmware tests
cd firmware && pio test -e native
```

## Branch & PR Process

1. Create a feature branch from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   ```
2. Make your changes and ensure all tests pass.
3. Push your branch and open a Pull Request targeting `main`.
4. CI will run automatically on your PR (API tests, web build, firmware compile).
5. Address any review feedback, then merge.

## Coding Standards

- **C# (.NET API):** Follow standard C# conventions. EF Core for data access, AutoMapper for DTOs.
- **TypeScript (React SPA):** ESLint rules are enforced via `npm run lint`.
- **C++ (Firmware):** PlatformIO project structure. Arduino framework with TFLite Micro.
- All timestamps should be in UTC.
- Photos are stored on the filesystem under `uploads/`, with paths tracked in the database.

## Project Structure

See [docs/architecture.md](docs/architecture.md) for a full architecture overview.

| Directory | Description |
|-----------|-------------|
| `src/DogDoor.Api/` | ASP.NET Core Web API |
| `src/DogDoor.Api.Tests/` | xUnit test project |
| `web/` | React + TypeScript + Vite SPA |
| `firmware/` | ESP32-CAM PlatformIO project |
| `helm/dog-door/` | Kubernetes Helm charts |
| `docs/` | Architecture and hardware docs |
