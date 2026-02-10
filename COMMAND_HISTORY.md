# Command History

Commands used to set up and develop the Smart Dog Door project.

## Project Initialization
```bash
git init
dotnet new sln -n DogDoor
dotnet new webapi -n DogDoor.Api -o src/DogDoor.Api
dotnet new xunit -n DogDoor.Api.Tests -o src/DogDoor.Api.Tests
dotnet sln add src/DogDoor.Api/DogDoor.Api.csproj
dotnet sln add src/DogDoor.Api.Tests/DogDoor.Api.Tests.csproj
dotnet add src/DogDoor.Api.Tests reference src/DogDoor.Api
```

## API NuGet Packages
```bash
cd src/DogDoor.Api
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
```

## Test NuGet Packages
```bash
cd src/DogDoor.Api.Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Moq
dotnet add package coverlet.collector
```

## React SPA
```bash
cd web
npm create vite@latest . -- --template react-ts
npm install
npm install axios react-router-dom
npm install -D @types/react-router-dom
```

## PlatformIO Firmware
```bash
cd firmware
pio init --board esp32cam
```

## Docker
```bash
docker-compose up --build
docker-compose down
```

## Helm
```bash
helm template helm/dog-door
```
