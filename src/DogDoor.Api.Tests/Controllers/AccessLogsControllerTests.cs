using System.Security.Claims;
using DogDoor.Api.Controllers;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DogDoor.Api.Tests.Controllers;

public class AccessLogsControllerTests
{
    private readonly Mock<IDoorService> _mockService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly AccessLogsController _controller;
    private const int UserId = 1;

    public AccessLogsControllerTests()
    {
        _mockService = new Mock<IDoorService>();
        _mockUserService = new Mock<IUserService>();

        _mockUserService.Setup(s => s.ResolveEffectiveUserIdAsync(UserId, null))
            .ReturnsAsync(UserId);

        _controller = new AccessLogsController(_mockService.Object, _mockUserService.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, UserId.ToString()) },
                    "Test"))
            }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithLogs()
    {
        var logs = new List<DoorEventDto>
        {
            new(1, 1, "Buddy", DoorEventType.AccessGranted, 0.9, null, DateTime.UtcNow, null, null, null),
            new(2, null, null, DoorEventType.UnknownAnimal, 0.3, "Not recognized", DateTime.UtcNow, null, null, null)
        };
        _mockService.Setup(s => s.GetAccessLogsAsync(1, 20, null, null, UserId)).ReturnsAsync(logs);

        var result = await _controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<DoorEventDto>>(okResult.Value);
        Assert.Equal(2, returned.Count());
    }

    [Fact]
    public async Task GetAll_WithFilter_PassesEventType()
    {
        var logs = new List<DoorEventDto>
        {
            new(1, 1, "Buddy", DoorEventType.AccessGranted, 0.9, null, DateTime.UtcNow, null, null, null)
        };
        _mockService.Setup(s => s.GetAccessLogsAsync(1, 20, "AccessGranted", null, UserId)).ReturnsAsync(logs);

        var result = await _controller.GetAll(eventType: "AccessGranted");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<DoorEventDto>>(okResult.Value);
        Assert.Single(returned);
    }

    [Fact]
    public async Task GetAll_WithDirectionFilter_PassesDirection()
    {
        var logs = new List<DoorEventDto>
        {
            new(1, 1, "Buddy", DoorEventType.EntryGranted, 0.9, null, DateTime.UtcNow, "Outside", "Entering", null)
        };
        _mockService.Setup(s => s.GetAccessLogsAsync(1, 20, null, "Entering", UserId)).ReturnsAsync(logs);

        var result = await _controller.GetAll(direction: "Entering");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<DoorEventDto>>(okResult.Value);
        Assert.Single(returned);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var log = new DoorEventDto(1, 1, "Buddy", DoorEventType.AccessGranted, 0.9, null, DateTime.UtcNow, null, null, null);
        _mockService.Setup(s => s.GetAccessLogAsync(1, UserId)).ReturnsAsync(log);

        var result = await _controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<DoorEventDto>(okResult.Value);
        Assert.Equal("Buddy", returned.AnimalName);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetAccessLogAsync(99, UserId)).ReturnsAsync((DoorEventDto?)null);

        var result = await _controller.GetById(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_NegativePage_DefaultsToOne()
    {
        _mockService.Setup(s => s.GetAccessLogsAsync(1, 20, null, null, UserId))
            .ReturnsAsync(new List<DoorEventDto>());

        var result = await _controller.GetAll(page: -1);

        _mockService.Verify(s => s.GetAccessLogsAsync(1, 20, null, null, UserId), Times.Once);
    }
}
