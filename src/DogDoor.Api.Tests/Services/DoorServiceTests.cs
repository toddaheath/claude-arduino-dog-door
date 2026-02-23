using AutoMapper;
using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using DogDoor.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DogDoor.Api.Tests.Services;

public class DoorServiceTests : IDisposable
{
    private readonly DogDoorDbContext _db;
    private readonly IMapper _mapper;
    private readonly Mock<IAnimalRecognitionService> _mockRecognition;
    private readonly DoorService _service;
    private readonly string _tempDir;
    private const int UserId = 1;

    public DoorServiceTests()
    {
        var options = new DbContextOptionsBuilder<DogDoorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new DogDoorDbContext(options);

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = mapperConfig.CreateMapper();

        _mockRecognition = new Mock<IAnimalRecognitionService>();

        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        var mockEnv = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(_tempDir);

        var configDict = new Dictionary<string, string?>
        {
            { "PhotoStorage:BasePath", "uploads" }
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        _service = new DoorService(_db, _mapper, _mockRecognition.Object, mockEnv.Object, config, Mock.Of<INotificationService>());
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private DoorConfiguration CreateConfig(bool isEnabled = true, string? apiKey = null)
    {
        return new DoorConfiguration
        {
            UserId = UserId,
            IsEnabled = isEnabled,
            ApiKey = apiKey,
            AutoCloseEnabled = true,
            AutoCloseDelaySeconds = 10,
            MinConfidenceThreshold = 0.7,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetConfigurationAsync_NoConfig_CreatesDefault()
    {
        var result = await _service.GetConfigurationAsync(UserId);

        Assert.True(result.IsEnabled);
        Assert.True(result.AutoCloseEnabled);
        Assert.Equal(10, result.AutoCloseDelaySeconds);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_UpdatesFields()
    {
        // Ensure a config exists
        await _service.GetConfigurationAsync(UserId);

        var dto = new UpdateDoorConfigurationDto(false, null, 30, null, null, null, null);
        var result = await _service.UpdateConfigurationAsync(dto, UserId);

        Assert.False(result.IsEnabled);
        Assert.Equal(30, result.AutoCloseDelaySeconds);
    }

    [Fact]
    public async Task ProcessAccessRequestAsync_DoorDisabled_ReturnsDenied()
    {
        _db.DoorConfigurations.Add(CreateConfig(isEnabled: false));
        await _db.SaveChangesAsync();

        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8 });
        var result = await _service.ProcessAccessRequestAsync(stream, null);

        Assert.False(result.Allowed);
        Assert.Equal("Door is disabled", result.Reason);
    }

    [Fact]
    public async Task ProcessAccessRequestAsync_InvalidApiKey_ReturnsDenied()
    {
        _db.DoorConfigurations.Add(CreateConfig(apiKey: "valid-key"));
        await _db.SaveChangesAsync();

        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8 });
        var result = await _service.ProcessAccessRequestAsync(stream, "wrong-key");

        Assert.False(result.Allowed);
        Assert.Equal("Invalid API key", result.Reason);
    }

    [Fact]
    public async Task ProcessAccessRequestAsync_KnownAllowedAnimal_ReturnsAllowed()
    {
        var animal = new Animal { Name = "Buddy", IsAllowed = true, UserId = UserId };
        _db.Animals.Add(animal);
        _db.DoorConfigurations.Add(CreateConfig());
        await _db.SaveChangesAsync();

        _mockRecognition.Setup(r => r.IdentifyAsync(It.IsAny<Stream>(), UserId))
            .ReturnsAsync(new RecognitionResult(animal.Id, "Buddy", 0.85));

        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8 });
        var result = await _service.ProcessAccessRequestAsync(stream, null);

        Assert.True(result.Allowed);
        Assert.Equal("Buddy", result.AnimalName);
    }

    [Fact]
    public async Task ProcessAccessRequestAsync_KnownDeniedAnimal_ReturnsDenied()
    {
        var animal = new Animal { Name = "Stray", IsAllowed = false, UserId = UserId };
        _db.Animals.Add(animal);
        _db.DoorConfigurations.Add(CreateConfig());
        await _db.SaveChangesAsync();

        _mockRecognition.Setup(r => r.IdentifyAsync(It.IsAny<Stream>(), UserId))
            .ReturnsAsync(new RecognitionResult(animal.Id, "Stray", 0.85));

        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8 });
        var result = await _service.ProcessAccessRequestAsync(stream, null);

        Assert.False(result.Allowed);
        Assert.Equal("Animal not allowed", result.Reason);
    }

    [Fact]
    public async Task ProcessAccessRequestAsync_UnknownAnimal_ReturnsDenied()
    {
        _db.DoorConfigurations.Add(CreateConfig());
        await _db.SaveChangesAsync();

        _mockRecognition.Setup(r => r.IdentifyAsync(It.IsAny<Stream>(), UserId))
            .ReturnsAsync(new RecognitionResult(null, null, 0.2));

        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8 });
        var result = await _service.ProcessAccessRequestAsync(stream, null);

        Assert.False(result.Allowed);
        Assert.Equal("Animal not recognized", result.Reason);
    }

    [Fact]
    public async Task GetAccessLogsAsync_ReturnsPagedResults()
    {
        _db.DoorEvents.AddRange(
            Enumerable.Range(1, 25).Select(i => new DoorEvent
            {
                UserId = UserId,
                EventType = DoorEventType.AccessGranted,
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            })
        );
        await _db.SaveChangesAsync();

        var page1 = await _service.GetAccessLogsAsync(1, 10, null, null, UserId);
        var page2 = await _service.GetAccessLogsAsync(2, 10, null, null, UserId);
        var page3 = await _service.GetAccessLogsAsync(3, 10, null, null, UserId);

        Assert.Equal(10, page1.Count());
        Assert.Equal(10, page2.Count());
        Assert.Equal(5, page3.Count());
    }

    [Fact]
    public async Task GetAccessLogsAsync_WithFilter_FiltersEvents()
    {
        _db.DoorEvents.Add(new DoorEvent { UserId = UserId, EventType = DoorEventType.AccessGranted });
        _db.DoorEvents.Add(new DoorEvent { UserId = UserId, EventType = DoorEventType.AccessDenied });
        _db.DoorEvents.Add(new DoorEvent { UserId = UserId, EventType = DoorEventType.AccessGranted });
        await _db.SaveChangesAsync();

        var result = await _service.GetAccessLogsAsync(1, 20, "AccessGranted", null, UserId);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAccessLogAsync_ExistingId_ReturnsEvent()
    {
        var doorEvent = new DoorEvent { UserId = UserId, EventType = DoorEventType.AccessGranted, Notes = "Test" };
        _db.DoorEvents.Add(doorEvent);
        await _db.SaveChangesAsync();

        var result = await _service.GetAccessLogAsync(doorEvent.Id, UserId);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Notes);
    }

    [Fact]
    public async Task GetAccessLogAsync_NonExisting_ReturnsNull()
    {
        var result = await _service.GetAccessLogAsync(999, UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ProcessAccessRequestAsync_LogsEvent()
    {
        _db.DoorConfigurations.Add(CreateConfig());
        await _db.SaveChangesAsync();

        _mockRecognition.Setup(r => r.IdentifyAsync(It.IsAny<Stream>(), UserId))
            .ReturnsAsync(new RecognitionResult(null, null, 0.1));

        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8 });
        await _service.ProcessAccessRequestAsync(stream, null);

        Assert.Equal(1, await _db.DoorEvents.CountAsync());
        var logged = await _db.DoorEvents.FirstAsync();
        Assert.Equal(DoorEventType.UnknownAnimal, logged.EventType);
    }

    [Fact]
    public async Task ProcessAccessRequestAsync_InsideSide_SetsExitingDirection()
    {
        var animal = new Animal { Name = "Buddy", IsAllowed = true, UserId = UserId };
        _db.Animals.Add(animal);
        _db.DoorConfigurations.Add(CreateConfig());
        await _db.SaveChangesAsync();

        _mockRecognition.Setup(r => r.IdentifyAsync(It.IsAny<Stream>(), UserId))
            .ReturnsAsync(new RecognitionResult(animal.Id, "Buddy", 0.85));

        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8 });
        var result = await _service.ProcessAccessRequestAsync(stream, null, "inside");

        Assert.True(result.Allowed);
        Assert.Equal("Exiting", result.Direction);

        var logged = await _db.DoorEvents.FirstAsync();
        Assert.Equal(DoorEventType.ExitGranted, logged.EventType);
        Assert.Equal(DoorSide.Inside, logged.Side);
        Assert.Equal(TransitDirection.Exiting, logged.Direction);
    }

    [Fact]
    public async Task ProcessAccessRequestAsync_OutsideSide_SetsEnteringDirection()
    {
        var animal = new Animal { Name = "Buddy", IsAllowed = true, UserId = UserId };
        _db.Animals.Add(animal);
        _db.DoorConfigurations.Add(CreateConfig());
        await _db.SaveChangesAsync();

        _mockRecognition.Setup(r => r.IdentifyAsync(It.IsAny<Stream>(), UserId))
            .ReturnsAsync(new RecognitionResult(animal.Id, "Buddy", 0.85));

        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8 });
        var result = await _service.ProcessAccessRequestAsync(stream, null, "outside");

        Assert.True(result.Allowed);
        Assert.Equal("Entering", result.Direction);

        var logged = await _db.DoorEvents.FirstAsync();
        Assert.Equal(DoorEventType.EntryGranted, logged.EventType);
        Assert.Equal(DoorSide.Outside, logged.Side);
        Assert.Equal(TransitDirection.Entering, logged.Direction);
    }

    [Fact]
    public async Task ProcessAccessRequestAsync_NullSide_PreservesBackwardCompat()
    {
        var animal = new Animal { Name = "Buddy", IsAllowed = true, UserId = UserId };
        _db.Animals.Add(animal);
        _db.DoorConfigurations.Add(CreateConfig());
        await _db.SaveChangesAsync();

        _mockRecognition.Setup(r => r.IdentifyAsync(It.IsAny<Stream>(), UserId))
            .ReturnsAsync(new RecognitionResult(animal.Id, "Buddy", 0.85));

        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8 });
        var result = await _service.ProcessAccessRequestAsync(stream, null, null);

        Assert.True(result.Allowed);
        Assert.Null(result.Direction);

        var logged = await _db.DoorEvents.FirstAsync();
        Assert.Equal(DoorEventType.AccessGranted, logged.EventType);
        Assert.Null(logged.Side);
        Assert.Null(logged.Direction);
    }

    [Fact]
    public async Task GetAccessLogsAsync_WithDirectionFilter_FiltersEvents()
    {
        _db.DoorEvents.Add(new DoorEvent { UserId = UserId, EventType = DoorEventType.EntryGranted, Direction = TransitDirection.Entering });
        _db.DoorEvents.Add(new DoorEvent { UserId = UserId, EventType = DoorEventType.ExitGranted, Direction = TransitDirection.Exiting });
        _db.DoorEvents.Add(new DoorEvent { UserId = UserId, EventType = DoorEventType.EntryGranted, Direction = TransitDirection.Entering });
        _db.DoorEvents.Add(new DoorEvent { UserId = UserId, EventType = DoorEventType.AccessGranted });
        await _db.SaveChangesAsync();

        var entering = await _service.GetAccessLogsAsync(1, 20, null, "Entering", UserId);
        var exiting = await _service.GetAccessLogsAsync(1, 20, null, "Exiting", UserId);

        Assert.Equal(2, entering.Count());
        Assert.Single(exiting);
    }
}
