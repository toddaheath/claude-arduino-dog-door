namespace DogDoor.Api.DTOs;

// ── Collar Device ───────────────────────────────────────────

public record CollarDeviceDto(
    int Id,
    string CollarId,
    string Name,
    int? AnimalId,
    string? AnimalName,
    string? FirmwareVersion,
    float? BatteryPercent,
    float? BatteryVoltage,
    DateTime? LastSeenAt,
    double? LastLatitude,
    double? LastLongitude,
    float? LastAccuracy,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateCollarDeviceDto(
    string Name,
    int? AnimalId
);

public record UpdateCollarDeviceDto(
    string? Name,
    int? AnimalId,
    bool? IsActive
);

public record CollarPairingResultDto(
    int Id,
    string CollarId,
    string SharedSecret,
    string Name
);

// ── NFC Verification ────────────────────────────────────────

public record NfcVerifyRequestDto(
    string CollarId,
    string Challenge,
    string Response,
    long Timestamp
);

public record NfcVerifyResponseDto(
    bool Verified,
    int? AnimalId,
    string? AnimalName,
    bool? IsAllowed,
    string? Reason
);

// ── Location ────────────────────────────────────────────────

public record LocationBatchDto(
    string? ApiKey,
    LocationPointDto[] Points
);

public record LocationPointDto(
    double Lat,
    double Lng,
    float? Alt,
    float? Acc,
    float? Spd,
    float? Hdg,
    int? Sat,
    float? Bat,
    long Ts
);

public record LocationQueryDto(
    double Latitude,
    double Longitude,
    float? Altitude,
    float? Accuracy,
    float? Speed,
    float? Heading,
    int? Satellites,
    DateTime Timestamp
);

public record CurrentLocationDto(
    double Latitude,
    double Longitude,
    float? Accuracy,
    float? Speed,
    DateTime Timestamp,
    float? BatteryPercent,
    string? ActivityState
);

// ── Geofence ────────────────────────────────────────────────

public record GeofenceDto(
    int Id,
    string Name,
    string FenceType,
    string Rule,
    string BoundaryJson,
    int BuzzerPattern,
    bool IsActive,
    int Version,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateGeofenceDto(
    string Name,
    string FenceType,
    string Rule,
    string BoundaryJson,
    int BuzzerPattern = 1
);

public record UpdateGeofenceDto(
    string? Name,
    string? Rule,
    string? BoundaryJson,
    int? BuzzerPattern,
    bool? IsActive
);

public record GeofenceSyncDto(
    int Version,
    GeofenceDto[] Fences
);

// ── Geofence Events ─────────────────────────────────────────

public record GeofenceEventDto(
    long Id,
    int GeofenceId,
    string GeofenceName,
    int CollarDeviceId,
    string CollarName,
    string EventType,
    double Latitude,
    double Longitude,
    DateTime Timestamp
);

public record GeofenceEventBatchDto(
    string? ApiKey,
    string CollarId,
    GeofenceEventInputDto[] Events
);

public record GeofenceEventInputDto(
    int FenceId,
    string Type,
    double Lat,
    double Lng,
    long Ts
);
