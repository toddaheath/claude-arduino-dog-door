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
    private const int UserId = 1;

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
        var result = await _service.GetAllAsync(UserId);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithAnimals_ReturnsAll()
    {
        _db.Animals.Add(new Animal { Name = "Buddy", Breed = "Golden Retriever", UserId = UserId });
        _db.Animals.Add(new Animal { Name = "Max", Breed = "German Shepherd", UserId = UserId });
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync(UserId);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_OnlyReturnsOwnAnimals()
    {
        _db.Animals.Add(new Animal { Name = "Buddy", UserId = UserId });
        _db.Animals.Add(new Animal { Name = "OtherDog", UserId = 99 });
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync(UserId);

        Assert.Single(result);
        Assert.Equal("Buddy", result.First().Name);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingAnimal_ReturnsAnimal()
    {
        var animal = new Animal { Name = "Buddy", Breed = "Golden Retriever", UserId = UserId };
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(animal.Id, UserId);

        Assert.NotNull(result);
        Assert.Equal("Buddy", result.Name);
        Assert.Equal("Golden Retriever", result.Breed);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999, UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WrongUser_ReturnsNull()
    {
        var animal = new Animal { Name = "Buddy", UserId = 99 };
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(animal.Id, UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesAnimal()
    {
        var dto = new CreateAnimalDto("Buddy", "Golden Retriever", true);

        var result = await _service.CreateAsync(dto, UserId);

        Assert.Equal("Buddy", result.Name);
        Assert.Equal("Golden Retriever", result.Breed);
        Assert.True(result.IsAllowed);
        Assert.Equal(1, await _db.Animals.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_ExistingAnimal_UpdatesFields()
    {
        var animal = new Animal { Name = "Buddy", Breed = "Golden Retriever", IsAllowed = true, UserId = UserId };
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync();

        var dto = new UpdateAnimalDto("Buddy Updated", null, false);
        var result = await _service.UpdateAsync(animal.Id, dto, UserId);

        Assert.NotNull(result);
        Assert.Equal("Buddy Updated", result.Name);
        Assert.False(result.IsAllowed);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_NonExisting_ReturnsNull()
    {
        var dto = new UpdateAnimalDto("Test", null, null);
        var result = await _service.UpdateAsync(999, dto, UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingAnimal_ReturnsTrue()
    {
        var animal = new Animal { Name = "Buddy", UserId = UserId };
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(animal.Id, UserId);

        Assert.True(result);
        Assert.Equal(0, await _db.Animals.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_NonExisting_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(999, UserId);
        Assert.False(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        _db.Animals.Add(new Animal { Name = "Zeus", UserId = UserId });
        _db.Animals.Add(new Animal { Name = "Alpha", UserId = UserId });
        _db.Animals.Add(new Animal { Name = "Max", UserId = UserId });
        await _db.SaveChangesAsync();

        var result = (await _service.GetAllAsync(UserId)).ToList();

        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Max", result[1].Name);
        Assert.Equal("Zeus", result[2].Name);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesPhotoCount()
    {
        var animal = new Animal { Name = "Buddy", UserId = UserId };
        animal.Photos.Add(new AnimalPhoto { FilePath = "/test/1.jpg", FileName = "1.jpg" });
        animal.Photos.Add(new AnimalPhoto { FilePath = "/test/2.jpg", FileName = "2.jpg" });
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(animal.Id, UserId);

        Assert.NotNull(result);
        Assert.Equal(2, result.PhotoCount);
    }
}
