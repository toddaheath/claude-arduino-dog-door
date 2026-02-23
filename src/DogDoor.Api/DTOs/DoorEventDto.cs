using DogDoor.Api.Models;

namespace DogDoor.Api.DTOs;

public record DoorEventDto(
    int Id,
    int? AnimalId,
    string? AnimalName,
    DoorEventType EventType,
    double? ConfidenceScore,
    string? Notes,
    DateTime Timestamp,
    string? Side,
    string? Direction,
    string? ImageUrl
);
