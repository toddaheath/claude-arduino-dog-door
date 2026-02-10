namespace DogDoor.Api.DTOs;

public record PhotoDto(
    int Id,
    int AnimalId,
    string? FileName,
    long FileSize,
    DateTime UploadedAt
);
