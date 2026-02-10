using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DogDoor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PhotosController : ControllerBase
{
    private readonly IPhotoService _photoService;

    public PhotosController(IPhotoService photoService)
    {
        _photoService = photoService;
    }

    [HttpGet("animal/{animalId}")]
    public async Task<ActionResult<IEnumerable<PhotoDto>>> GetByAnimal(int animalId)
    {
        var photos = await _photoService.GetByAnimalIdAsync(animalId);
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
        var photo = await _photoService.UploadAsync(animalId, stream, file.FileName);

        if (photo is null)
            return NotFound($"Animal with ID {animalId} not found");

        return CreatedAtAction(nameof(GetFile), new { id = photo.Id }, photo);
    }

    [HttpGet("{id}/file")]
    public async Task<IActionResult> GetFile(int id)
    {
        var result = await _photoService.GetFileAsync(id);
        if (result is null) return NotFound();

        var (stream, contentType) = result.Value;
        if (stream is null) return NotFound();

        return File(stream, contentType ?? "application/octet-stream");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _photoService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
