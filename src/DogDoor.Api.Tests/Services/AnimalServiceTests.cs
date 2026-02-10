using AutoMapper;
using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using DogDoor.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Tests.Services;

public class AnimalServiceTests : IDisposable
{
    private readonly DogDoorDbContext _db;
    private readonly IMapper _mapper;
    private readonly AnimalService _service;

    public AnimalServiceTests()
    {
        var options = new DbContextOptionsBuilder<DogDoorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new DogDoorDbContext(options);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _service = new AnimalService(_db, _mapper);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmpty()
    {
        var result = await _service.GetAllAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithAnimals_ReturnsAll()
    {
        _db.Animals.Add(new Animal { Name = "Buddy", Breed = "Golden Retriever" });
        _db.Animals.Add(new Animal { Name = "Max", Breed = "German Shepherd" });
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ExistingAnimal_ReturnsAnimal()
    {
        var animal = new Animal { Name = "Buddy", Breed = "Golden Retriever" };
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(animal.Id);

        Assert.NotNull(result);
        Assert.Equal("Buddy", result.Name);
        Assert.Equal("Golden Retriever", result.Breed);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesAnimal()
    {
        var dto = new CreateAnimalDto("Buddy", "Golden Retriever", true);

        var result = await _service.CreateAsync(dto);

        Assert.Equal("Buddy", result.Name);
        Assert.Equal("Golden Retriever", result.Breed);
        Assert.True(result.IsAllowed);
        Assert.Equal(1, await _db.Animals.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_ExistingAnimal_UpdatesFields()
    {
        var animal = new Animal { Name = "Buddy", Breed = "Golden Retriever", IsAllowed = true };
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync();

        var dto = new UpdateAnimalDto("Buddy Updated", null, false);
        var result = await _service.UpdateAsync(animal.Id, dto);

        Assert.NotNull(result);
        Assert.Equal("Buddy Updated", result.Name);
        Assert.False(result.IsAllowed);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_NonExisting_ReturnsNull()
    {
        var dto = new UpdateAnimalDto("Test", null, null);
        var result = await _service.UpdateAsync(999, dto);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingAnimal_ReturnsTrue()
    {
        var animal = new Animal { Name = "Buddy" };
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(animal.Id);

        Assert.True(result);
        Assert.Equal(0, await _db.Animals.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_NonExisting_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(999);
        Assert.False(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        _db.Animals.Add(new Animal { Name = "Zeus" });
        _db.Animals.Add(new Animal { Name = "Alpha" });
        _db.Animals.Add(new Animal { Name = "Max" });
        await _db.SaveChangesAsync();

        var result = (await _service.GetAllAsync()).ToList();

        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Max", result[1].Name);
        Assert.Equal("Zeus", result[2].Name);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesPhotoCount()
    {
        var animal = new Animal { Name = "Buddy" };
        animal.Photos.Add(new AnimalPhoto { FilePath = "/test/1.jpg", FileName = "1.jpg" });
        animal.Photos.Add(new AnimalPhoto { FilePath = "/test/2.jpg", FileName = "2.jpg" });
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(animal.Id);

        Assert.NotNull(result);
        Assert.Equal(2, result.PhotoCount);
    }
}
