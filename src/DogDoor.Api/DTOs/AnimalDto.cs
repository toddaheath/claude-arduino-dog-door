namespace DogDoor.Api.DTOs;

public record AnimalDto(
    int Id,
    string Name,
    string? Breed,
    bool IsAllowed,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int PhotoCount
);

public record CreateAnimalDto(
    string Name,
    string? Breed,
    bool IsAllowed = true
);

public record UpdateAnimalDto(
    string? Name,
    string? Breed,
    bool? IsAllowed
);
