using AutoMapper;
using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Services;

public class NotificationPreferencesService : INotificationPreferencesService
{
    private readonly DogDoorDbContext _db;
    private readonly IMapper _mapper;

    public NotificationPreferencesService(DogDoorDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<NotificationPreferencesDto> GetAsync(int userId)
    {
        var prefs = await _db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prefs is null)
        {
            prefs = new NotificationPreferences { UserId = userId, UpdatedAt = DateTime.UtcNow };
            _db.NotificationPreferences.Add(prefs);
            await _db.SaveChangesAsync();
        }

        return _mapper.Map<NotificationPreferencesDto>(prefs);
    }

    public async Task<NotificationPreferencesDto> UpdateAsync(int userId, UpdateNotificationPreferencesDto dto)
    {
        var prefs = await _db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prefs is null)
        {
            prefs = new NotificationPreferences { UserId = userId };
            _db.NotificationPreferences.Add(prefs);
        }

        if (dto.EmailEnabled.HasValue) prefs.EmailEnabled = dto.EmailEnabled.Value;
        if (dto.SmsEnabled.HasValue) prefs.SmsEnabled = dto.SmsEnabled.Value;
        if (dto.AnimalApproachInside.HasValue) prefs.AnimalApproachInside = dto.AnimalApproachInside.Value;
        if (dto.AnimalApproachOutside.HasValue) prefs.AnimalApproachOutside = dto.AnimalApproachOutside.Value;
        if (dto.UnknownAnimalInside.HasValue) prefs.UnknownAnimalInside = dto.UnknownAnimalInside.Value;
        if (dto.UnknownAnimalOutside.HasValue) prefs.UnknownAnimalOutside = dto.UnknownAnimalOutside.Value;
        if (dto.DoorOpened.HasValue) prefs.DoorOpened = dto.DoorOpened.Value;
        if (dto.DoorClosed.HasValue) prefs.DoorClosed = dto.DoorClosed.Value;
        if (dto.DoorFailedOpen.HasValue) prefs.DoorFailedOpen = dto.DoorFailedOpen.Value;
        if (dto.DoorFailedClose.HasValue) prefs.DoorFailedClose = dto.DoorFailedClose.Value;
        if (dto.PowerDisconnected.HasValue) prefs.PowerDisconnected = dto.PowerDisconnected.Value;
        if (dto.PowerRestored.HasValue) prefs.PowerRestored = dto.PowerRestored.Value;
        if (dto.BatteryLow.HasValue) prefs.BatteryLow = dto.BatteryLow.Value;
        if (dto.BatteryCharged.HasValue) prefs.BatteryCharged = dto.BatteryCharged.Value;
        prefs.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return _mapper.Map<NotificationPreferencesDto>(prefs);
    }
}
