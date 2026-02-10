using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DogDoor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessLogsController : ControllerBase
{
    private readonly IDoorService _doorService;

    public AccessLogsController(IDoorService doorService)
    {
        _doorService = doorService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DoorEventDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? eventType = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var logs = await _doorService.GetAccessLogsAsync(page, pageSize, eventType);
        return Ok(logs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DoorEventDto>> GetById(int id)
    {
        var log = await _doorService.GetAccessLogAsync(id);
        if (log is null) return NotFound();
        return Ok(log);
    }
}
