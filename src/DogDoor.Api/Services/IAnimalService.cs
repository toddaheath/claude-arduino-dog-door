using DogDoor.Api.DTOs;

namespace DogDoor.Api.Services;

public interface IAnimalService
{
    Task<IEnumerable<AnimalDto>> GetAllAsync();
    Task<AnimalDto?> GetByIdAsync(int id);
    Task<AnimalDto> CreateAsync(CreateAnimalDto dto);
    Task<AnimalDto?> UpdateAsync(int id, UpdateAnimalDto dto);
    Task<bool> DeleteAsync(int id);
}
