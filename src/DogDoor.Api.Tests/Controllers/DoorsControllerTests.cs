using System.Security.Claims;
using DogDoor.Api.Controllers;
using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DogDoor.Api.Tests.Controllers;

public class DoorsControllerTests
{
    private readonly Mock<IDoorService> _mockService;
    private readonly DoorsController _controller;
    private const int UserId = 1;

    public DoorsControllerTests()
    {
        _mockService = new Mock<IDoorService>();
        _controller = new DoorsController(_mockService.Object);
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
    public async Task GetStatus_ReturnsOkWithConfiguration()
    {
        var config = new DoorConfigurationDto(true, true, 10, 0.7, false, null, null);
        _mockService.Setup(s => s.GetConfigurationAsync(UserId)).ReturnsAsync(config);

        var result = await _controller.GetStatus();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<DoorConfigurationDto>(okResult.Value);
        Assert.True(returned.IsEnabled);
    }

    [Fact]
    public async Task UpdateConfiguration_ReturnsOkWithUpdated()
    {
        var updateDto = new UpdateDoorConfigurationDto(false, null, null, null, null, null, null);
        var updated = new DoorConfigurationDto(false, true, 10, 0.7, false, null, null);
        _mockService.Setup(s => s.UpdateConfigurationAsync(updateDto, UserId)).ReturnsAsync(updated);

        var result = await _controller.UpdateConfiguration(updateDto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<DoorConfigurationDto>(okResult.Value);
        Assert.False(returned.IsEnabled);
    }

    [Fact]
    public async Task AccessRequest_EmptyImage_ReturnsBadRequest()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);

        var result = await _controller.AccessRequest(mockFile.Object, null, null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AccessRequest_ValidImage_ReturnsOk()
    {
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header
        var stream = new MemoryStream(content);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(content.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        var response = new AccessResponseDto(true, 1, "Buddy", 0.85, null, null);
        _mockService.Setup(s => s.ProcessAccessRequestAsync(It.IsAny<Stream>(), null, null))
            .ReturnsAsync(response);

        var result = await _controller.AccessRequest(mockFile.Object, null, null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<AccessResponseDto>(okResult.Value);
        Assert.True(returned.Allowed);
        Assert.Equal("Buddy", returned.AnimalName);
    }

    [Fact]
    public async Task AccessRequest_WithSide_PassesSideToService()
    {
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var stream = new MemoryStream(content);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(content.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        var response = new AccessResponseDto(true, 1, "Buddy", 0.85, null, "Exiting");
        _mockService.Setup(s => s.ProcessAccessRequestAsync(It.IsAny<Stream>(), null, "inside"))
            .ReturnsAsync(response);

        var result = await _controller.AccessRequest(mockFile.Object, null, "inside");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<AccessResponseDto>(okResult.Value);
        Assert.Equal("Exiting", returned.Direction);
    }

    [Fact]
    public async Task FirmwareEvent_ValidEventType_ReturnsNoContent()
    {
        var dto = new FirmwareEventDto("test-api-key", "DoorOpened", null, null);
        _mockService.Setup(s => s.RecordFirmwareEventAsync("test-api-key", DogDoor.Api.Models.DoorEventType.DoorOpened, null, null))
            .Returns(Task.CompletedTask);

        var result = await _controller.FirmwareEvent(dto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task FirmwareEvent_InvalidEventType_ReturnsBadRequest()
    {
        var dto = new FirmwareEventDto(null, "NotARealEvent", null, null);

        var result = await _controller.FirmwareEvent(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
