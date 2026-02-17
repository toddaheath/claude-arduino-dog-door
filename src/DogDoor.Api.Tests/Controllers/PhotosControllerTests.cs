using System.Security.Claims;
using DogDoor.Api.Controllers;
using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DogDoor.Api.Tests.Controllers;

public class PhotosControllerTests
{
    private readonly Mock<IPhotoService> _mockService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly PhotosController _controller;
    private const int UserId = 1;

    public PhotosControllerTests()
    {
        _mockService = new Mock<IPhotoService>();
        _mockUserService = new Mock<IUserService>();

        _mockUserService.Setup(s => s.ResolveEffectiveUserIdAsync(UserId, null))
            .ReturnsAsync(UserId);

        _controller = new PhotosController(_mockService.Object, _mockUserService.Object);
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
    public async Task GetByAnimal_ReturnsOkWithPhotos()
    {
        var photos = new List<PhotoDto>
        {
            new(1, 1, "buddy1.jpg", 12345, DateTime.UtcNow),
            new(2, 1, "buddy2.jpg", 23456, DateTime.UtcNow)
        };
        _mockService.Setup(s => s.GetByAnimalIdAsync(1, UserId)).ReturnsAsync(photos);

        var result = await _controller.GetByAnimal(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<PhotoDto>>(okResult.Value);
        Assert.Equal(2, returned.Count());
    }

    [Fact]
    public async Task Upload_EmptyFile_ReturnsBadRequest()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);
        mockFile.Setup(f => f.FileName).Returns("test.jpg");

        var result = await _controller.Upload(1, mockFile.Object);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Upload_InvalidExtension_ReturnsBadRequest()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.FileName).Returns("test.bmp");

        var result = await _controller.Upload(1, mockFile.Object);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("allowed", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task Upload_FileTooLarge_ReturnsBadRequest()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(11 * 1024 * 1024); // 11MB
        mockFile.Setup(f => f.FileName).Returns("test.jpg");

        var result = await _controller.Upload(1, mockFile.Object);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("10MB", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task Upload_AnimalNotFound_ReturnsNotFound()
    {
        var content = new byte[] { 0xFF, 0xD8 };
        var stream = new MemoryStream(content);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(content.Length);
        mockFile.Setup(f => f.FileName).Returns("test.jpg");
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        _mockService.Setup(s => s.UploadAsync(99, It.IsAny<Stream>(), "test.jpg", UserId))
            .ReturnsAsync((PhotoDto?)null);

        var result = await _controller.Upload(99, mockFile.Object);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Upload_ValidFile_ReturnsCreated()
    {
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var stream = new MemoryStream(content);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(content.Length);
        mockFile.Setup(f => f.FileName).Returns("buddy.jpg");
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        var photo = new PhotoDto(1, 1, "buddy.jpg", content.Length, DateTime.UtcNow);
        _mockService.Setup(s => s.UploadAsync(1, It.IsAny<Stream>(), "buddy.jpg", UserId))
            .ReturnsAsync(photo);

        var result = await _controller.Upload(1, mockFile.Object);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<PhotoDto>(createdResult.Value);
        Assert.Equal("buddy.jpg", returned.FileName);
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        _mockService.Setup(s => s.DeleteAsync(1, UserId)).ReturnsAsync(true);

        var result = await _controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteAsync(99, UserId)).ReturnsAsync(false);

        var result = await _controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }
}
