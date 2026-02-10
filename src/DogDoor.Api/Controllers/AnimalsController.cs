using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DogDoor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnimalsController : ControllerBase
{
    private readonly IAnimalService _animalService;

    public AnimalsController(IAnimalService animalService)
    {
        _animalService = animalService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnimalDto>>> GetAll()
    {
        var animals = await _animalService.GetAllAsync();
        return Ok(animals);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AnimalDto>> GetById(int id)
    {
        var animal = await _animalService.GetByIdAsync(id);
        if (animal is null) return NotFound();
        return Ok(animal);
    }

    [HttpPost]
    public async Task<ActionResult<AnimalDto>> Create([FromBody] CreateAnimalDto dto)
    {
        var animal = await _animalService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = animal.Id }, animal);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AnimalDto>> Update(int id, [FromBody] UpdateAnimalDto dto)
    {
        var animal = await _animalService.UpdateAsync(id, dto);
        if (animal is null) return NotFound();
        return Ok(animal);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _animalService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
