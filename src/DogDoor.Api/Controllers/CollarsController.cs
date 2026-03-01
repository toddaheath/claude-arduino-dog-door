using System.Security.Claims;
using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogDoor.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CollarsController : ControllerBase
{
    private readonly ICollarService _collarService;

    public CollarsController(ICollarService collarService)
    {
        _collarService = collarService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Register a new collar device and receive pairing credentials.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CollarPairingResultDto>> Register([FromBody] CreateCollarDeviceDto dto)
    {
        var result = await _collarService.RegisterCollarAsync(GetUserId(), dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// List all collar devices for the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CollarDeviceDto>>> GetAll()
    {
        var collars = await _collarService.GetCollarsAsync(GetUserId());
        return Ok(collars);
    }

    /// <summary>
    /// Get a specific collar device.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CollarDeviceDto>> GetById(int id)
    {
        var collar = await _collarService.GetCollarAsync(GetUserId(), id);
        if (collar == null) return NotFound();
        return Ok(collar);
    }

    /// <summary>
    /// Update collar device settings.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CollarDeviceDto>> Update(int id, [FromBody] UpdateCollarDeviceDto dto)
    {
        var collar = await _collarService.UpdateCollarAsync(GetUserId(), id, dto);
        if (collar == null) return NotFound();
        return Ok(collar);
    }

    /// <summary>
    /// Delete a collar device.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _collarService.DeleteCollarAsync(GetUserId(), id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Verify NFC challenge-response from a collar at the door.
    /// Called by the door firmware, not the user.
    /// </summary>
    [HttpPost("{collarId}/verify")]
    [AllowAnonymous]
    public async Task<ActionResult<NfcVerifyResponseDto>> VerifyNfc(
        string collarId, [FromBody] NfcVerifyRequestDto dto)
    {
        var result = await _collarService.VerifyNfcAsync(collarId, dto);
        return Ok(result);
    }

    /// <summary>
    /// Upload a batch of GPS location points from the collar.
    /// Called by the collar firmware.
    /// </summary>
    [HttpPost("{collarId}/locations")]
    [AllowAnonymous]
    public async Task<IActionResult> UploadLocations(
        string collarId, [FromBody] LocationBatchDto dto)
    {
        var count = await _collarService.UploadLocationsAsync(collarId, dto.Points);
        if (count == 0) return NotFound();
        return StatusCode(201, new { uploaded = count });
    }

    /// <summary>
    /// Get location history for a collar device.
    /// </summary>
    [HttpGet("{id:int}/locations")]
    public async Task<ActionResult<IEnumerable<LocationQueryDto>>> GetLocations(
        int id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var collar = await _collarService.GetCollarAsync(GetUserId(), id);
        if (collar == null) return NotFound();

        var fromDate = from ?? DateTime.UtcNow.AddHours(-24);
        var toDate = to ?? DateTime.UtcNow;

        var locations = await _collarService.GetLocationHistoryAsync(id, fromDate, toDate);
        return Ok(locations);
    }

    /// <summary>
    /// Get current location for a collar device.
    /// </summary>
    [HttpGet("{id:int}/location/current")]
    public async Task<ActionResult<CurrentLocationDto>> GetCurrentLocation(int id)
    {
        var collar = await _collarService.GetCollarAsync(GetUserId(), id);
        if (collar == null) return NotFound();

        var location = await _collarService.GetCurrentLocationAsync(id);
        if (location == null) return NotFound();
        return Ok(location);
    }
}
