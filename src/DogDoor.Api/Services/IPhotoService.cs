using DogDoor.Api.DTOs;

namespace DogDoor.Api.Services;

public interface IPhotoService
{
    Task<IEnumerable<PhotoDto>> GetByAnimalIdAsync(int animalId);
    Task<PhotoDto?> UploadAsync(int animalId, Stream fileStream, string fileName);
    Task<(Stream? Stream, string? ContentType)?> GetFileAsync(int photoId);
    Task<bool> DeleteAsync(int photoId);
}
