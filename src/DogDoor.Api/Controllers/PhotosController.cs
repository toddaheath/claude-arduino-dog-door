using System.Security.Claims;
using Asp.Versioning;
using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogDoor.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/photos")]
[Authorize]
public class PhotosController : ControllerBase
{
    private readonly IPhotoService _photoService;
    private readonly IUserService _userService;

    public PhotosController(IPhotoService photoService, IUserService userService)
    {
        _photoService = photoService;
        _userService = userService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("animal/{animalId}")]
    public async Task<ActionResult<IEnumerable<PhotoDto>>> GetByAnimal(int animalId, [FromQuery] int? asOwner = null)
    {
        var userId = await _userService.ResolveEffectiveUserIdAsync(CurrentUserId, asOwner);
        var photos = await _photoService.GetByAnimalIdAsync(animalId, userId);
        return Ok(photos);
    }

    [HttpPost("upload/{animalId}")]
    public async Task<ActionResult<PhotoDto>> Upload(int animalId, IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest("File is empty");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest("Only .jpg, .jpeg, and .png files are allowed");

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest("File size must be less than 10MB");

        using var stream = file.OpenReadStream();
        var photo = await _photoService.UploadAsync(animalId, stream, file.FileName, CurrentUserId);

        if (photo is null)
            return NotFound($"Animal with ID {animalId} not found");

        return CreatedAtAction(nameof(GetFile), new { id = photo.Id }, photo);
    }

    [HttpGet("{id}/file")]
    public async Task<IActionResult> GetFile(int id)
    {
        var result = await _photoService.GetFileAsync(id, CurrentUserId);
        if (result is null) return NotFound();

        var (stream, contentType) = result.Value;
        if (stream is null) return NotFound();

        return File(stream, contentType ?? "application/octet-stream");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _photoService.DeleteAsync(id, CurrentUserId);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
