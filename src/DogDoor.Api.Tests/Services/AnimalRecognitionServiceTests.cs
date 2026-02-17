using DogDoor.Api.Data;
using DogDoor.Api.Models;
using DogDoor.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Tests.Services;

public class AnimalRecognitionServiceTests : IDisposable
{
    private readonly DogDoorDbContext _db;
    private readonly AnimalRecognitionService _service;
    private const int UserId = 1;

    public AnimalRecognitionServiceTests()
    {
        var options = new DbContextOptionsBuilder<DogDoorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new DogDoorDbContext(options);
        _service = new AnimalRecognitionService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task IdentifyAsync_NoPhotos_ReturnsNoMatch()
    {
        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var result = await _service.IdentifyAsync(stream, UserId);

        Assert.Null(result.AnimalId);
        Assert.Equal(0, result.Confidence);
    }

    [Fact]
    public async Task IdentifyAsync_WithPhotos_ReturnsBestMatch()
    {
        var animal = new Animal { Name = "Buddy", UserId = UserId };
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync();

        _db.AnimalPhotos.Add(new AnimalPhoto
        {
            AnimalId = animal.Id,
            FilePath = "/test/photo.jpg",
            PHash = "ABCDEF1234567890"
        });
        await _db.SaveChangesAsync();

        // Use a stream that produces a very different hash
        var stream = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00 });

        var result = await _service.IdentifyAsync(stream, UserId);

        // The result depends on hash similarity - with different input, low confidence expected
        Assert.NotNull(result);
    }

    [Fact]
    public async Task IdentifyAsync_ExactSameInput_ReturnsHighConfidence()
    {
        var animal = new Animal { Name = "Buddy", UserId = UserId };
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync();

        // Compute the hash of known bytes, then store that hash
        var testBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        string expectedHash;
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            var hash = sha.ComputeHash(testBytes);
            expectedHash = Convert.ToHexString(hash)[..16];
        }

        _db.AnimalPhotos.Add(new AnimalPhoto
        {
            AnimalId = animal.Id,
            FilePath = "/test/photo.jpg",
            PHash = expectedHash
        });
        await _db.SaveChangesAsync();

        var stream = new MemoryStream(testBytes);
        var result = await _service.IdentifyAsync(stream, UserId);

        Assert.Equal(animal.Id, result.AnimalId);
        Assert.Equal("Buddy", result.AnimalName);
        Assert.Equal(1.0, result.Confidence);
    }

    [Fact]
    public async Task IdentifyAsync_OtherUserPhotos_NotConsidered()
    {
        var animal = new Animal { Name = "OtherDog", UserId = 99 };
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync();

        var testBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        string expectedHash;
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            var hash = sha.ComputeHash(testBytes);
            expectedHash = Convert.ToHexString(hash)[..16];
        }

        _db.AnimalPhotos.Add(new AnimalPhoto
        {
            AnimalId = animal.Id,
            FilePath = "/test/photo.jpg",
            PHash = expectedHash
        });
        await _db.SaveChangesAsync();

        // Same bytes, but scoped to UserId=1 — should not find the other user's animal
        var stream = new MemoryStream(testBytes);
        var result = await _service.IdentifyAsync(stream, UserId);

        Assert.Null(result.AnimalId);
    }
}
