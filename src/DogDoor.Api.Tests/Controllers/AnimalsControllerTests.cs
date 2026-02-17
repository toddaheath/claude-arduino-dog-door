using System.Security.Claims;
using DogDoor.Api.Controllers;
using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DogDoor.Api.Tests.Controllers;

public class AnimalsControllerTests
{
    private readonly Mock<IAnimalService> _mockService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly AnimalsController _controller;
    private const int UserId = 1;

    public AnimalsControllerTests()
    {
        _mockService = new Mock<IAnimalService>();
        _mockUserService = new Mock<IUserService>();

        // Default: no asOwner — return current userId
        _mockUserService.Setup(s => s.ResolveEffectiveUserIdAsync(UserId, null))
            .ReturnsAsync(UserId);

        _controller = new AnimalsController(_mockService.Object, _mockUserService.Object);
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
    public async Task GetAll_ReturnsOkWithAnimals()
    {
        var animals = new List<AnimalDto>
        {
            new(1, "Buddy", "Golden Retriever", true, DateTime.UtcNow, null, 2),
            new(2, "Max", "German Shepherd", true, DateTime.UtcNow, null, 0)
        };
        _mockService.Setup(s => s.GetAllAsync(UserId)).ReturnsAsync(animals);

        var result = await _controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<AnimalDto>>(okResult.Value);
        Assert.Equal(2, returned.Count());
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var animal = new AnimalDto(1, "Buddy", "Golden Retriever", true, DateTime.UtcNow, null, 2);
        _mockService.Setup(s => s.GetByIdAsync(1, UserId)).ReturnsAsync(animal);

        var result = await _controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<AnimalDto>(okResult.Value);
        Assert.Equal("Buddy", returned.Name);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetByIdAsync(99, UserId)).ReturnsAsync((AnimalDto?)null);

        var result = await _controller.GetById(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        var createDto = new CreateAnimalDto("Buddy", "Golden Retriever", true);
        var createdAnimal = new AnimalDto(1, "Buddy", "Golden Retriever", true, DateTime.UtcNow, null, 0);
        _mockService.Setup(s => s.CreateAsync(createDto, UserId)).ReturnsAsync(createdAnimal);

        var result = await _controller.Create(createDto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<AnimalDto>(createdResult.Value);
        Assert.Equal("Buddy", returned.Name);
        Assert.Equal(1, returned.Id);
    }

    [Fact]
    public async Task Update_ExistingId_ReturnsOk()
    {
        var updateDto = new UpdateAnimalDto("Buddy Updated", null, null);
        var updated = new AnimalDto(1, "Buddy Updated", "Golden Retriever", true, DateTime.UtcNow, DateTime.UtcNow, 0);
        _mockService.Setup(s => s.UpdateAsync(1, updateDto, UserId)).ReturnsAsync(updated);

        var result = await _controller.Update(1, updateDto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<AnimalDto>(okResult.Value);
        Assert.Equal("Buddy Updated", returned.Name);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var updateDto = new UpdateAnimalDto("Buddy Updated", null, null);
        _mockService.Setup(s => s.UpdateAsync(99, updateDto, UserId)).ReturnsAsync((AnimalDto?)null);

        var result = await _controller.Update(99, updateDto);

        Assert.IsType<NotFoundResult>(result.Result);
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
