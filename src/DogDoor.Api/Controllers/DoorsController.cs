using System.Security.Claims;
using Asp.Versioning;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogDoor.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/doors")]
public class DoorsController : ControllerBase
{
    private readonly IDoorService _doorService;

    public DoorsController(IDoorService doorService)
    {
        _doorService = doorService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // No auth — ESP32 identifies via API key
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
    [Authorize]
    public async Task<ActionResult<DoorConfigurationDto>> GetStatus()
    {
        var config = await _doorService.GetConfigurationAsync(CurrentUserId);
        return Ok(config);
    }

    [HttpPut("configuration")]
    [Authorize]
    public async Task<ActionResult<DoorConfigurationDto>> UpdateConfiguration(
        [FromBody] UpdateDoorConfigurationDto dto)
    {
        var config = await _doorService.UpdateConfigurationAsync(dto, CurrentUserId);
        return Ok(config);
    }

    // No auth — ESP32 identifies via API key in form body
    [HttpPost("approach-photo")]
    public async Task<IActionResult> ApproachPhoto(
        IFormFile image,
        [FromForm] string? apiKey,
        [FromForm] string? side)
    {
        if (image.Length == 0)
            return BadRequest("Image is required");

        using var stream = image.OpenReadStream();
        await _doorService.RecordApproachPhotoAsync(stream, apiKey, side);
        return NoContent();
    }

    // No auth — ESP32 identifies via API key
    [HttpPost("firmware-event")]
    public async Task<IActionResult> FirmwareEvent([FromBody] FirmwareEventDto dto)
    {
        if (!Enum.TryParse<DoorEventType>(dto.EventType, true, out var eventType))
            return BadRequest($"Unknown event type: {dto.EventType}");

        await _doorService.RecordFirmwareEventAsync(dto.ApiKey, eventType, dto.Notes, dto.BatteryVoltage);
        return NoContent();
    }
}
