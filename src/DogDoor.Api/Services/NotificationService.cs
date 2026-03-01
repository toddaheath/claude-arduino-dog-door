using DogDoor.Api.Data;
using DogDoor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Services;

public class NotificationService : INotificationService
{
    private readonly DogDoorDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        DogDoorDbContext db,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task NotifyAsync(int userId, DoorEventType eventType, string? animalName, DoorSide? side, string? notes = null)
    {
        try
        {
            var user = await _db.Users
                .Include(u => u.NotificationPreferences)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null) return;

            var prefs = user.NotificationPreferences;
            if (prefs is null) return;

            if (!IsEnabled(prefs, eventType, side, notes)) return;

            var subject = BuildSubject(eventType, animalName, side);
            var body = BuildBody(eventType, animalName, side, notes);

            if (prefs.EmailEnabled)
                await _emailService.SendNotificationAsync(user.Email, subject, body);

            if (prefs.SmsEnabled && !string.IsNullOrEmpty(user.MobilePhone))
                await _smsService.SendSmsAsync(user.MobilePhone, $"{subject}: {body}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification failed for user {UserId} event {EventType}", userId, eventType);
        }
    }

    private static bool IsEnabled(NotificationPreferences prefs, DoorEventType eventType, DoorSide? side, string? notes)
    {
        return eventType switch
        {
            DoorEventType.EntryGranted => prefs.AnimalApproachOutside,
            DoorEventType.ExitGranted => prefs.AnimalApproachInside,
            DoorEventType.AccessGranted => side switch
            {
                DoorSide.Outside => prefs.AnimalApproachOutside,
                DoorSide.Inside => prefs.AnimalApproachInside,
                _ => prefs.AnimalApproachOutside || prefs.AnimalApproachInside
            },
            DoorEventType.UnknownAnimal => side switch
            {
                DoorSide.Inside => prefs.UnknownAnimalInside,
                DoorSide.Outside => prefs.UnknownAnimalOutside,
                _ => prefs.UnknownAnimalInside || prefs.UnknownAnimalOutside
            },
            DoorEventType.DoorOpened => prefs.DoorOpened,
            DoorEventType.DoorClosed => prefs.DoorClosed,
            DoorEventType.DoorObstructed => notes?.Contains("open", StringComparison.OrdinalIgnoreCase) == true
                ? prefs.DoorFailedOpen
                : prefs.DoorFailedClose,
            DoorEventType.PowerLost => prefs.PowerDisconnected,
            DoorEventType.PowerRestored => prefs.PowerRestored,
            DoorEventType.BatteryLow => prefs.BatteryLow,
            DoorEventType.BatteryCharged => prefs.BatteryCharged,
            DoorEventType.GeofenceBreach => prefs.GeofenceBreach,
            DoorEventType.GeofenceEntered or DoorEventType.GeofenceExited => prefs.GeofenceEnteredExited,
            DoorEventType.CollarBatteryLow => prefs.CollarBatteryLow,
            DoorEventType.CollarDisconnected => prefs.CollarDisconnected,
            _ => false
        };
    }

    private static string BuildSubject(DoorEventType eventType, string? animalName, DoorSide? side)
    {
        return eventType switch
        {
            DoorEventType.EntryGranted or DoorEventType.ExitGranted or DoorEventType.AccessGranted =>
                $"Dog Door: {animalName ?? "Your dog"} used the door",
            DoorEventType.UnknownAnimal => "Dog Door: Unknown animal detected",
            DoorEventType.DoorOpened => "Dog Door: Door opened",
            DoorEventType.DoorClosed => "Dog Door: Door closed",
            DoorEventType.DoorObstructed => "Dog Door: Door obstruction detected",
            DoorEventType.PowerLost => "Dog Door: Power disconnected",
            DoorEventType.PowerRestored => "Dog Door: Power restored",
            DoorEventType.BatteryLow => "Dog Door: Battery low",
            DoorEventType.BatteryCharged => "Dog Door: Battery charged",
            DoorEventType.GeofenceBreach => $"Collar: {animalName ?? "Your dog"} left a geofence!",
            DoorEventType.GeofenceEntered => $"Collar: {animalName ?? "Your dog"} entered a geofence",
            DoorEventType.GeofenceExited => $"Collar: {animalName ?? "Your dog"} left a geofence",
            DoorEventType.CollarBatteryLow => $"Collar: {animalName ?? "Dog"}'s collar battery is low",
            DoorEventType.CollarDisconnected => $"Collar: {animalName ?? "Dog"}'s collar disconnected",
            _ => "Dog Door: Event notification"
        };
    }

    private static string BuildBody(DoorEventType eventType, string? animalName, DoorSide? side, string? notes)
    {
        var sideStr = side?.ToString().ToLowerInvariant();
        return eventType switch
        {
            DoorEventType.EntryGranted => $"{animalName ?? "Your dog"} entered through the dog door.",
            DoorEventType.ExitGranted => $"{animalName ?? "Your dog"} exited through the dog door.",
            DoorEventType.AccessGranted => $"{animalName ?? "Your dog"} used the dog door{(sideStr != null ? $" ({sideStr})" : "")}.",
            DoorEventType.UnknownAnimal => $"An unrecognized animal was detected at the {sideStr ?? "door"} sensor.",
            DoorEventType.DoorOpened => "The dog door has been opened.",
            DoorEventType.DoorClosed => "The dog door has been closed.",
            DoorEventType.DoorObstructed => $"The dog door failed to {(notes?.Contains("open", StringComparison.OrdinalIgnoreCase) == true ? "open" : "close")}. Please check for obstructions.",
            DoorEventType.PowerLost => "Main power has been disconnected. Running on battery.",
            DoorEventType.PowerRestored => "Main power has been restored.",
            DoorEventType.BatteryLow => "Battery level is low. Please check the power supply.",
            DoorEventType.BatteryCharged => "Battery is fully charged.",
            DoorEventType.GeofenceBreach => $"{animalName ?? "Your dog"} has breached a geofence boundary.{(notes != null ? $" Zone: {notes}" : "")}",
            DoorEventType.GeofenceEntered => $"{animalName ?? "Your dog"} entered a geofence zone.{(notes != null ? $" Zone: {notes}" : "")}",
            DoorEventType.GeofenceExited => $"{animalName ?? "Your dog"} left a geofence zone.{(notes != null ? $" Zone: {notes}" : "")}",
            DoorEventType.CollarBatteryLow => $"{animalName ?? "Dog"}'s collar battery is low. Please charge soon.",
            DoorEventType.CollarDisconnected => $"{animalName ?? "Dog"}'s collar has not been seen for an extended period.",
            _ => notes ?? "A door event occurred."
        };
    }
}
