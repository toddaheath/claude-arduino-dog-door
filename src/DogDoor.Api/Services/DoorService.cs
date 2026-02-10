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

    public async Task<AccessResponseDto> ProcessAccessRequestAsync(Stream imageStream, string? apiKey)
    {
        var doorConfig = await _db.DoorConfigurations.FirstOrDefaultAsync();

        // Validate API key if configured
        if (doorConfig?.ApiKey is not null && doorConfig.ApiKey != apiKey)
        {
            return new AccessResponseDto(false, null, null, null, "Invalid API key");
        }

        // Check if door is enabled
        if (doorConfig is not null && !doorConfig.IsEnabled)
        {
            await LogEventAsync(null, DoorEventType.AccessDenied, null, 0, "Door is disabled");
            return new AccessResponseDto(false, null, null, null, "Door is disabled");
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
                await LogEventAsync(null, DoorEventType.AccessDenied, null, 0, "Night mode active");
                return new AccessResponseDto(false, null, null, null, "Night mode active");
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
        var result = await _recognition.IdentifyAsync(recognitionStream);

        var threshold = doorConfig?.MinConfidenceThreshold ?? 0.7;

        if (result.AnimalId.HasValue && result.Confidence >= threshold)
        {
            // Check if animal is allowed
            var animal = await _db.Animals.FindAsync(result.AnimalId.Value);
            if (animal is not null && animal.IsAllowed)
            {
                await LogEventAsync(animal.Id, DoorEventType.AccessGranted, imagePath, result.Confidence, null);
                return new AccessResponseDto(true, animal.Id, animal.Name, result.Confidence, null);
            }

            await LogEventAsync(result.AnimalId, DoorEventType.AccessDenied, imagePath, result.Confidence, "Animal not allowed");
            return new AccessResponseDto(false, result.AnimalId, result.AnimalName, result.Confidence, "Animal not allowed");
        }

        await LogEventAsync(null, DoorEventType.UnknownAnimal, imagePath, result.Confidence, "Animal not recognized");
        return new AccessResponseDto(false, null, null, result.Confidence, "Animal not recognized");
    }

    public async Task<DoorConfigurationDto> GetConfigurationAsync()
    {
        var config = await _db.DoorConfigurations.FirstOrDefaultAsync();
        if (config is null)
        {
            config = new DoorConfiguration();
            _db.DoorConfigurations.Add(config);
            await _db.SaveChangesAsync();
        }

        return _mapper.Map<DoorConfigurationDto>(config);
    }

    public async Task<DoorConfigurationDto> UpdateConfigurationAsync(UpdateDoorConfigurationDto dto)
    {
        var config = await _db.DoorConfigurations.FirstOrDefaultAsync();
        if (config is null)
        {
            config = new DoorConfiguration();
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

    public async Task<IEnumerable<DoorEventDto>> GetAccessLogsAsync(int page, int pageSize, string? eventType)
    {
        var query = _db.DoorEvents
            .Include(e => e.Animal)
            .AsQueryable();

        if (eventType is not null && Enum.TryParse<DoorEventType>(eventType, true, out var parsedType))
        {
            query = query.Where(e => e.EventType == parsedType);
        }

        var events = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return _mapper.Map<IEnumerable<DoorEventDto>>(events);
    }

    public async Task<DoorEventDto?> GetAccessLogAsync(int id)
    {
        var doorEvent = await _db.DoorEvents
            .Include(e => e.Animal)
            .FirstOrDefaultAsync(e => e.Id == id);

        return doorEvent is null ? null : _mapper.Map<DoorEventDto>(doorEvent);
    }

    private async Task LogEventAsync(int? animalId, DoorEventType eventType, string? imagePath, double? confidence, string? notes)
    {
        var doorEvent = new DoorEvent
        {
            AnimalId = animalId,
            EventType = eventType,
            ImagePath = imagePath,
            ConfidenceScore = confidence,
            Notes = notes
        };

        _db.DoorEvents.Add(doorEvent);
        await _db.SaveChangesAsync();
    }
}
