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

    // ── Firmware Management ──────────────────────────────────

    /// <summary>
    /// Check if a firmware update is available for a collar.
    /// Called by the collar firmware.
    /// </summary>
    [HttpGet("{collarId}/firmware")]
    [AllowAnonymous]
    public async Task<ActionResult<FirmwareCheckDto>> CheckFirmware(
        string collarId, [FromQuery] string current)
    {
        var check = await _collarService.CheckFirmwareAsync(collarId, current);
        if (!check.UpdateAvailable) return NotFound();
        return Ok(check);
    }

    /// <summary>
    /// Download the latest firmware binary.
    /// Called by the collar firmware during OTA.
    /// </summary>
    [HttpGet("{collarId}/firmware/download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadFirmware(string collarId)
    {
        var result = await _collarService.DownloadFirmwareAsync(collarId);
        if (result == null) return NotFound();

        var (stream, contentType, length) = result.Value;
        if (stream == null) return NotFound();

        Response.ContentLength = length;
        return File(stream, contentType ?? "application/octet-stream", "firmware.bin");
    }

    /// <summary>
    /// Upload a new firmware release (admin action).
    /// </summary>
    [HttpPost("firmware")]
    public async Task<ActionResult<FirmwareReleaseDto>> UploadFirmware(
        [FromForm] string version,
        [FromForm] string? releaseNotes,
        IFormFile file)
    {
        if (file.Length == 0) return BadRequest("No firmware file provided");

        using var stream = file.OpenReadStream();
        var release = await _collarService.UploadFirmwareAsync(
            GetUserId(), version, releaseNotes, stream, file.FileName);

        return CreatedAtAction(nameof(GetFirmwareReleases), release);
    }

    /// <summary>
    /// List all firmware releases.
    /// </summary>
    [HttpGet("firmware")]
    public async Task<ActionResult<IEnumerable<FirmwareReleaseDto>>> GetFirmwareReleases()
    {
        var releases = await _collarService.GetFirmwareReleasesAsync();
        return Ok(releases);
    }
}
