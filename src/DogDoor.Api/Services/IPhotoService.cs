using DogDoor.Api.DTOs;

namespace DogDoor.Api.Services;

public interface IPhotoService
{
    Task<IEnumerable<PhotoDto>> GetByAnimalIdAsync(int animalId, int userId);
    Task<PhotoDto?> UploadAsync(int animalId, Stream fileStream, string fileName, int userId);
    Task<(Stream? Stream, string? ContentType)?> GetFileAsync(int photoId, int userId);
    Task<bool> DeleteAsync(int photoId, int userId);
}
