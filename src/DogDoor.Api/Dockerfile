# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DogDoor.Api/DogDoor.Api.csproj DogDoor.Api/
RUN dotnet restore DogDoor.Api/DogDoor.Api.csproj
COPY DogDoor.Api/ DogDoor.Api/
RUN dotnet publish DogDoor.Api/DogDoor.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Create uploads directory
RUN mkdir -p /app/uploads

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
RUN chown -R appuser:appgroup /app/uploads
USER appuser

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000

ENTRYPOINT ["dotnet", "DogDoor.Api.dll"]
