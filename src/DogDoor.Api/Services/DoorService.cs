using AutoMapper;
using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Services;

public class DoorService : IDoorService
{
    private readonly DogDoorDbContext _db;
    private readonly IMapper _mapper;
    private readonly IAnimalRecognitionService _recognition;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public DoorService(
        DogDoorDbContext db,
        IMapper mapper,
        IAnimalRecognitionService recognition,
        IWebHostEnvironment env,
        IConfiguration config)
    {
        _db = db;
        _mapper = mapper;
        _recognition = recognition;
        _env = env;
        _config = config;
    }

    public async Task<AccessResponseDto> ProcessAccessRequestAsync(Stream imageStream, string? apiKey, string? side = null)
    {
        // Determine side and direction from the requesting camera
        DoorSide? doorSide = side?.ToLowerInvariant() switch
        {
            "inside" => DoorSide.Inside,
            "outside" => DoorSide.Outside,
            _ => null
        };

        TransitDirection? direction = doorSide switch
        {
            DoorSide.Inside => TransitDirection.Exiting,
            DoorSide.Outside => TransitDirection.Entering,
            _ => null
        };

        string? directionString = direction?.ToString();

        // Resolve DoorConfiguration by API key
        DoorConfiguration? doorConfig;
        if (apiKey != null)
        {
            doorConfig = await _db.DoorConfigurations.FirstOrDefaultAsync(c => c.ApiKey == apiKey);
            if (doorConfig == null)
                return new AccessResponseDto(false, null, null, null, "Invalid API key", directionString);
        }
        else
        {
            // No API key provided — find config without API key requirement (backward compat)
            doorConfig = await _db.DoorConfigurations.FirstOrDefaultAsync(c => c.ApiKey == null);
            if (doorConfig == null)
                return new AccessResponseDto(false, null, null, null, "API key required", directionString);
        }

        int userId = doorConfig.UserId;

        // Check if door is enabled
        if (!doorConfig.IsEnabled)
        {
            await LogEventAsync(userId, null, DoorEventType.AccessDenied, null, 0, "Door is disabled", doorSide, direction);
            return new AccessResponseDto(false, null, null, null, "Door is disabled", directionString);
        }

        // Check night mode
        if (doorConfig is { NightModeEnabled: true, NightModeStart: not null, NightModeEnd: not null })
        {
            var now = TimeOnly.FromDateTime(DateTime.UtcNow);
            bool isNightTime = doorConfig.NightModeStart <= doorConfig.NightModeEnd
                ? now >= doorConfig.NightModeStart && now <= doorConfig.NightModeEnd
                : now >= doorConfig.NightModeStart || now <= doorConfig.NightModeEnd;

            if (isNightTime)
            {
                await LogEventAsync(userId, null, DoorEventType.AccessDenied, null, 0, "Night mode active", doorSide, direction);
                return new AccessResponseDto(false, null, null, null, "Night mode active", directionString);
            }
        }

        // Save incoming image
        var basePath = _config.GetValue<string>("PhotoStorage:BasePath") ?? "uploads";
        var eventImagesDir = Path.Combine(_env.ContentRootPath, basePath, "events");
        Directory.CreateDirectory(eventImagesDir);
        var imagePath = Path.Combine(eventImagesDir, $"{Guid.NewGuid()}.jpg");

        using (var fs = new FileStream(imagePath, FileMode.Create))
        {
            await imageStream.CopyToAsync(fs);
        }

        // Reset stream for recognition
        using var recognitionStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
        var result = await _recognition.IdentifyAsync(recognitionStream, userId);

        var threshold = doorConfig.MinConfidenceThreshold;

        if (result.AnimalId.HasValue && result.Confidence >= threshold)
        {
            var animal = await _db.Animals.FirstOrDefaultAsync(a => a.Id == result.AnimalId.Value && a.UserId == userId);
            if (animal is not null && animal.IsAllowed)
            {
                var grantedType = direction switch
                {
                    TransitDirection.Exiting => DoorEventType.ExitGranted,
                    TransitDirection.Entering => DoorEventType.EntryGranted,
                    _ => DoorEventType.AccessGranted
                };
                await LogEventAsync(userId, animal.Id, grantedType, imagePath, result.Confidence, null, doorSide, direction);
                return new AccessResponseDto(true, animal.Id, animal.Name, result.Confidence, null, directionString);
            }

            var deniedType = direction switch
            {
                TransitDirection.Exiting => DoorEventType.ExitDenied,
                TransitDirection.Entering => DoorEventType.EntryDenied,
                _ => DoorEventType.AccessDenied
            };
            await LogEventAsync(userId, result.AnimalId, deniedType, imagePath, result.Confidence, "Animal not allowed", doorSide, direction);
            return new AccessResponseDto(false, result.AnimalId, result.AnimalName, result.Confidence, "Animal not allowed", directionString);
        }

        await LogEventAsync(userId, null, DoorEventType.UnknownAnimal, imagePath, result.Confidence, "Animal not recognized", doorSide, direction);
        return new AccessResponseDto(false, null, null, result.Confidence, "Animal not recognized", directionString);
    }

    public async Task<DoorConfigurationDto> GetConfigurationAsync(int userId)
    {
        var config = await _db.DoorConfigurations.FirstOrDefaultAsync(c => c.UserId == userId);
        if (config is null)
        {
            config = new DoorConfiguration { UserId = userId, UpdatedAt = DateTime.UtcNow };
            _db.DoorConfigurations.Add(config);
            await _db.SaveChangesAsync();
        }

        return _mapper.Map<DoorConfigurationDto>(config);
    }

    public async Task<DoorConfigurationDto> UpdateConfigurationAsync(UpdateDoorConfigurationDto dto, int userId)
    {
        var config = await _db.DoorConfigurations.FirstOrDefaultAsync(c => c.UserId == userId);
        if (config is null)
        {
            config = new DoorConfiguration { UserId = userId };
            _db.DoorConfigurations.Add(config);
        }

        if (dto.IsEnabled.HasValue) config.IsEnabled = dto.IsEnabled.Value;
        if (dto.AutoCloseEnabled.HasValue) config.AutoCloseEnabled = dto.AutoCloseEnabled.Value;
        if (dto.AutoCloseDelaySeconds.HasValue) config.AutoCloseDelaySeconds = dto.AutoCloseDelaySeconds.Value;
        if (dto.MinConfidenceThreshold.HasValue) config.MinConfidenceThreshold = dto.MinConfidenceThreshold.Value;
        if (dto.NightModeEnabled.HasValue) config.NightModeEnabled = dto.NightModeEnabled.Value;
        if (dto.NightModeStart.HasValue) config.NightModeStart = dto.NightModeStart.Value;
        if (dto.NightModeEnd.HasValue) config.NightModeEnd = dto.NightModeEnd.Value;
        config.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return _mapper.Map<DoorConfigurationDto>(config);
    }

    public async Task<IEnumerable<DoorEventDto>> GetAccessLogsAsync(int page, int pageSize, string? eventType, string? direction, int userId)
    {
        var query = _db.DoorEvents
            .Include(e => e.Animal)
            .Where(e => e.UserId == userId)
            .AsQueryable();

        if (eventType is not null && Enum.TryParse<DoorEventType>(eventType, true, out var parsedType))
        {
            query = query.Where(e => e.EventType == parsedType);
        }

        if (direction is not null && Enum.TryParse<TransitDirection>(direction, true, out var parsedDirection))
        {
            query = query.Where(e => e.Direction == parsedDirection);
        }

        var events = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return _mapper.Map<IEnumerable<DoorEventDto>>(events);
    }

    public async Task<DoorEventDto?> GetAccessLogAsync(int id, int userId)
    {
        var doorEvent = await _db.DoorEvents
            .Include(e => e.Animal)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        return doorEvent is null ? null : _mapper.Map<DoorEventDto>(doorEvent);
    }

    private async Task LogEventAsync(int userId, int? animalId, DoorEventType eventType, string? imagePath, double? confidence, string? notes, DoorSide? side = null, TransitDirection? direction = null)
    {
        var doorEvent = new DoorEvent
        {
            UserId = userId,
            AnimalId = animalId,
            EventType = eventType,
            ImagePath = imagePath,
            ConfidenceScore = confidence,
            Notes = notes,
            Side = side,
            Direction = direction
        };

        _db.DoorEvents.Add(doorEvent);
        await _db.SaveChangesAsync();
    }
}
