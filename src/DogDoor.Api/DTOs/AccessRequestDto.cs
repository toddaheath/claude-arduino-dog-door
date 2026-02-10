namespace DogDoor.Api.DTOs;

public record AccessRequestDto(
    string? ApiKey
);

public record AccessResponseDto(
    bool Allowed,
    int? AnimalId,
    string? AnimalName,
    double? ConfidenceScore,
    string? Reason
);
