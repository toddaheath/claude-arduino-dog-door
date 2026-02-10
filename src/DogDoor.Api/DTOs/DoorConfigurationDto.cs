namespace DogDoor.Api.DTOs;

public record DoorConfigurationDto(
    bool IsEnabled,
    bool AutoCloseEnabled,
    int AutoCloseDelaySeconds,
    double MinConfidenceThreshold,
    bool NightModeEnabled,
    TimeOnly? NightModeStart,
    TimeOnly? NightModeEnd
);

public record UpdateDoorConfigurationDto(
    bool? IsEnabled,
    bool? AutoCloseEnabled,
    int? AutoCloseDelaySeconds,
    double? MinConfidenceThreshold,
    bool? NightModeEnabled,
    TimeOnly? NightModeStart,
    TimeOnly? NightModeEnd
);
