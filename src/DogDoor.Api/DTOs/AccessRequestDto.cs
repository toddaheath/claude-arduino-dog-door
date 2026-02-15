namespace DogDoor.Api.DTOs;

public record AccessRequestDto(
    string? ApiKey,
    string? Side
);

public record AccessResponseDto(
    bool Allowed,
    int? AnimalId,
    string? AnimalName,
    double? ConfidenceScore,
    string? Reason,
    string? Direction
);
