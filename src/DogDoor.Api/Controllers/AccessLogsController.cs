using System.Security.Claims;
using Asp.Versioning;
using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogDoor.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/accesslogs")]
[Authorize]
public class AccessLogsController : ControllerBase
{
    private readonly IDoorService _doorService;
    private readonly IUserService _userService;

    public AccessLogsController(IDoorService doorService, IUserService userService)
    {
        _doorService = doorService;
        _userService = userService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DoorEventDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? eventType = null,
        [FromQuery] string? direction = null,
        [FromQuery] int? asOwner = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var userId = await _userService.ResolveEffectiveUserIdAsync(CurrentUserId, asOwner);
        var logs = await _doorService.GetAccessLogsAsync(page, pageSize, eventType, direction, userId);
        return Ok(logs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DoorEventDto>> GetById(int id, [FromQuery] int? asOwner = null)
    {
        var userId = await _userService.ResolveEffectiveUserIdAsync(CurrentUserId, asOwner);
        var log = await _doorService.GetAccessLogAsync(id, userId);
        if (log is null) return NotFound();
        return Ok(log);
    }
}
