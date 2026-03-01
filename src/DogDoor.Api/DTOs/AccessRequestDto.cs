namespace DogDoor.Api.DTOs;

public record AccessRequestDto(
    string? ApiKey,
    string? Side,
    // Collar fields (optional, sent when collar is detected at door)
    string? CollarId,
    bool? CollarNfcVerified,
    int? CollarRssi
);

public record FirmwareEventDto(
    string? ApiKey,
    string EventType,
    string? Notes,
    double? BatteryVoltage);

public record AccessResponseDto(
    bool Allowed,
    int? AnimalId,
    string? AnimalName,
    double? ConfidenceScore,
    string? Reason,
    string? Direction
);
