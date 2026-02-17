using System.Security.Claims;
using Asp.Versioning;
using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogDoor.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/animals")]
[Authorize]
public class AnimalsController : ControllerBase
{
    private readonly IAnimalService _animalService;
    private readonly IUserService _userService;

    public AnimalsController(IAnimalService animalService, IUserService userService)
    {
        _animalService = animalService;
        _userService = userService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnimalDto>>> GetAll([FromQuery] int? asOwner = null)
    {
        var userId = await _userService.ResolveEffectiveUserIdAsync(CurrentUserId, asOwner);
        var animals = await _animalService.GetAllAsync(userId);
        return Ok(animals);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AnimalDto>> GetById(int id, [FromQuery] int? asOwner = null)
    {
        var userId = await _userService.ResolveEffectiveUserIdAsync(CurrentUserId, asOwner);
        var animal = await _animalService.GetByIdAsync(id, userId);
        if (animal is null) return NotFound();
        return Ok(animal);
    }

    [HttpPost]
    public async Task<ActionResult<AnimalDto>> Create([FromBody] CreateAnimalDto dto)
    {
        var animal = await _animalService.CreateAsync(dto, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = animal.Id }, animal);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AnimalDto>> Update(int id, [FromBody] UpdateAnimalDto dto)
    {
        var animal = await _animalService.UpdateAsync(id, dto, CurrentUserId);
        if (animal is null) return NotFound();
        return Ok(animal);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _animalService.DeleteAsync(id, CurrentUserId);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
