using AutoMapper;
using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Services;

public class PhotoService : IPhotoService
{
    private readonly DogDoorDbContext _db;
    private readonly IMapper _mapper;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public PhotoService(DogDoorDbContext db, IMapper mapper, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _mapper = mapper;
        _config = config;
        _env = env;
    }

    public async Task<IEnumerable<PhotoDto>> GetByAnimalIdAsync(int animalId, int userId)
    {
        // Verify the animal belongs to the user
        var animalExists = await _db.Animals.AnyAsync(a => a.Id == animalId && a.UserId == userId);
        if (!animalExists) return Enumerable.Empty<PhotoDto>();

        var photos = await _db.AnimalPhotos
            .Where(p => p.AnimalId == animalId)
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<PhotoDto>>(photos);
    }

    public async Task<PhotoDto?> UploadAsync(int animalId, Stream fileStream, string fileName, int userId)
    {
        var animal = await _db.Animals.FirstOrDefaultAsync(a => a.Id == animalId && a.UserId == userId);
        if (animal is null) return null;

        var basePath = _config.GetValue<string>("PhotoStorage:BasePath") ?? "uploads";
        var uploadsDir = Path.Combine(_env.ContentRootPath, basePath, animalId.ToString());
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, storedFileName);

        using (var fs = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fs);
        }

        var fileInfo = new FileInfo(filePath);
        var pHash = ComputePHash(filePath);

        // Store relative path from uploads root so paths survive base path changes
        var relativePath = Path.Combine(animalId.ToString(), storedFileName);

        var photo = new AnimalPhoto
        {
            AnimalId = animalId,
            FilePath = relativePath,
            FileName = fileName,
            PHash = pHash,
            FileSize = fileInfo.Length
        };

        _db.AnimalPhotos.Add(photo);
        await _db.SaveChangesAsync();

        return _mapper.Map<PhotoDto>(photo);
    }

    public async Task<(Stream? Stream, string? ContentType)?> GetFileAsync(int photoId, int userId)
    {
        var photo = await _db.AnimalPhotos
            .Include(p => p.Animal)
            .FirstOrDefaultAsync(p => p.Id == photoId && p.Animal.UserId == userId);

        if (photo is null) return null;

        var fullPath = ResolveFullPath(photo.FilePath);
        if (!File.Exists(fullPath)) return null;

        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return (stream, contentType);
    }

    public async Task<bool> DeleteAsync(int photoId, int userId)
    {
        var photo = await _db.AnimalPhotos
            .Include(p => p.Animal)
            .FirstOrDefaultAsync(p => p.Id == photoId && p.Animal.UserId == userId);

        if (photo is null) return false;

        var fullPath = ResolveFullPath(photo.FilePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        _db.AnimalPhotos.Remove(photo);
        await _db.SaveChangesAsync();
        return true;
    }

    private string ResolveFullPath(string storedPath)
    {
        // If the stored path is already absolute (legacy data), return as-is
        if (Path.IsPathRooted(storedPath)) return storedPath;

        var basePath = _config.GetValue<string>("PhotoStorage:BasePath") ?? "uploads";
        return Path.Combine(_env.ContentRootPath, basePath, storedPath);
    }

    private static string ComputePHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return AnimalRecognitionService.ComputeDHash(stream);
    }
}
