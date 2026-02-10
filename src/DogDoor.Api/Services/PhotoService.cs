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

    public async Task<IEnumerable<PhotoDto>> GetByAnimalIdAsync(int animalId)
    {
        var photos = await _db.AnimalPhotos
            .Where(p => p.AnimalId == animalId)
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<PhotoDto>>(photos);
    }

    public async Task<PhotoDto?> UploadAsync(int animalId, Stream fileStream, string fileName)
    {
        var animal = await _db.Animals.FindAsync(animalId);
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

        var photo = new AnimalPhoto
        {
            AnimalId = animalId,
            FilePath = filePath,
            FileName = fileName,
            PHash = pHash,
            FileSize = fileInfo.Length
        };

        _db.AnimalPhotos.Add(photo);
        await _db.SaveChangesAsync();

        return _mapper.Map<PhotoDto>(photo);
    }

    public async Task<(Stream? Stream, string? ContentType)?> GetFileAsync(int photoId)
    {
        var photo = await _db.AnimalPhotos.FindAsync(photoId);
        if (photo is null || !File.Exists(photo.FilePath)) return null;

        var ext = Path.GetExtension(photo.FilePath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        var stream = new FileStream(photo.FilePath, FileMode.Open, FileAccess.Read);
        return (stream, contentType);
    }

    public async Task<bool> DeleteAsync(int photoId)
    {
        var photo = await _db.AnimalPhotos.FindAsync(photoId);
        if (photo is null) return false;

        if (File.Exists(photo.FilePath))
        {
            File.Delete(photo.FilePath);
        }

        _db.AnimalPhotos.Remove(photo);
        await _db.SaveChangesAsync();
        return true;
    }

    private static string ComputePHash(string filePath)
    {
        // Simplified perceptual hash: uses file bytes to generate a hash.
        // In production, use a proper pHash library that resizes to 8x8,
        // converts to grayscale, applies DCT, and computes median-based hash.
        using var stream = File.OpenRead(filePath);
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash)[..16];
    }
}
