using System.Security.Claims;
using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogDoor.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class GeofencesController : ControllerBase
{
    private readonly IGeofenceService _geofenceService;

    public GeofencesController(IGeofenceService geofenceService)
    {
        _geofenceService = geofenceService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Create a new geofence.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<GeofenceDto>> Create([FromBody] CreateGeofenceDto dto)
    {
        var fence = await _geofenceService.CreateGeofenceAsync(GetUserId(), dto);
        return CreatedAtAction(nameof(GetById), new { id = fence.Id }, fence);
    }

    /// <summary>
    /// List all geofences for the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GeofenceDto>>> GetAll()
    {
        var fences = await _geofenceService.GetGeofencesAsync(GetUserId());
        return Ok(fences);
    }

    /// <summary>
    /// Get a specific geofence.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<GeofenceDto>> GetById(int id)
    {
        var fence = await _geofenceService.GetGeofenceAsync(GetUserId(), id);
        if (fence == null) return NotFound();
        return Ok(fence);
    }

    /// <summary>
    /// Update a geofence.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<GeofenceDto>> Update(int id, [FromBody] UpdateGeofenceDto dto)
    {
        var fence = await _geofenceService.UpdateGeofenceAsync(GetUserId(), id, dto);
        if (fence == null) return NotFound();
        return Ok(fence);
    }

    /// <summary>
    /// Delete a geofence.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _geofenceService.DeleteGeofenceAsync(GetUserId(), id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Get updated fences since a version (used by collar sync protocol).
    /// </summary>
    [HttpGet("sync")]
    [AllowAnonymous]
    public async Task<ActionResult<GeofenceSyncDto>> Sync(
        [FromQuery] int userId,
        [FromQuery] int sinceVersion = 0)
    {
        var sync = await _geofenceService.GetGeofenceSyncAsync(userId, sinceVersion);
        if (!sync.Fences.Any() && sinceVersion > 0)
            return StatusCode(304);
        return Ok(sync);
    }

    /// <summary>
    /// Record geofence events from a collar.
    /// </summary>
    [HttpPost("events")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordEvents([FromBody] GeofenceEventBatchDto dto)
    {
        var count = await _geofenceService.RecordGeofenceEventsAsync(dto.CollarId, dto.Events);
        if (count == 0) return NotFound();
        return StatusCode(201, new { recorded = count });
    }

    /// <summary>
    /// Get geofence events for the current user.
    /// </summary>
    [HttpGet("events")]
    public async Task<ActionResult<IEnumerable<GeofenceEventDto>>> GetEvents(
        [FromQuery] int? geofenceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var events = await _geofenceService.GetGeofenceEventsAsync(GetUserId(), geofenceId, from, to);
        return Ok(events);
    }
}
