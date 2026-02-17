using DogDoor.Api.DTOs;

namespace DogDoor.Api.Services;

public interface IAnimalService
{
    Task<IEnumerable<AnimalDto>> GetAllAsync(int userId);
    Task<AnimalDto?> GetByIdAsync(int id, int userId);
    Task<AnimalDto> CreateAsync(CreateAnimalDto dto, int userId);
    Task<AnimalDto?> UpdateAsync(int id, UpdateAnimalDto dto, int userId);
    Task<bool> DeleteAsync(int id, int userId);
}
