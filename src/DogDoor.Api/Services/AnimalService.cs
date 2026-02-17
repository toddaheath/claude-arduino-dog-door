using AutoMapper;
using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Services;

public class AnimalService : IAnimalService
{
    private readonly DogDoorDbContext _db;
    private readonly IMapper _mapper;

    public AnimalService(DogDoorDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<IEnumerable<AnimalDto>> GetAllAsync(int userId)
    {
        var animals = await _db.Animals
            .Include(a => a.Photos)
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return _mapper.Map<IEnumerable<AnimalDto>>(animals);
    }

    public async Task<AnimalDto?> GetByIdAsync(int id, int userId)
    {
        var animal = await _db.Animals
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        return animal is null ? null : _mapper.Map<AnimalDto>(animal);
    }

    public async Task<AnimalDto> CreateAsync(CreateAnimalDto dto, int userId)
    {
        var animal = _mapper.Map<Animal>(dto);
        animal.UserId = userId;
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync();

        return _mapper.Map<AnimalDto>(animal);
    }

    public async Task<AnimalDto?> UpdateAsync(int id, UpdateAnimalDto dto, int userId)
    {
        var animal = await _db.Animals
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (animal is null) return null;

        if (dto.Name is not null) animal.Name = dto.Name;
        if (dto.Breed is not null) animal.Breed = dto.Breed;
        if (dto.IsAllowed.HasValue) animal.IsAllowed = dto.IsAllowed.Value;
        animal.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return _mapper.Map<AnimalDto>(animal);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var animal = await _db.Animals.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (animal is null) return false;

        _db.Animals.Remove(animal);
        await _db.SaveChangesAsync();
        return true;
    }
}
