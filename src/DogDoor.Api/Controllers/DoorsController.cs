using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DogDoor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoorsController : ControllerBase
{
    private readonly IDoorService _doorService;

    public DoorsController(IDoorService doorService)
    {
        _doorService = doorService;
    }

    [HttpPost("access-request")]
    public async Task<ActionResult<AccessResponseDto>> AccessRequest(
        IFormFile image,
        [FromForm] string? apiKey,
        [FromForm] string? side)
    {
        if (image.Length == 0)
            return BadRequest("Image is required");

        using var stream = image.OpenReadStream();
        var result = await _doorService.ProcessAccessRequestAsync(stream, apiKey, side);
        return Ok(result);
    }

    [HttpGet("status")]
    public async Task<ActionResult<DoorConfigurationDto>> GetStatus()
    {
        var config = await _doorService.GetConfigurationAsync();
        return Ok(config);
    }

    [HttpPut("configuration")]
    public async Task<ActionResult<DoorConfigurationDto>> UpdateConfiguration(
        [FromBody] UpdateDoorConfigurationDto dto)
    {
        var config = await _doorService.UpdateConfigurationAsync(dto);
        return Ok(config);
    }
}
