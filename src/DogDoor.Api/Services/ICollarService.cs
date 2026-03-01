using DogDoor.Api.DTOs;

namespace DogDoor.Api.Services;

public interface ICollarService
{
    // Collar CRUD
    Task<CollarPairingResultDto> RegisterCollarAsync(int userId, CreateCollarDeviceDto dto);
    Task<IEnumerable<CollarDeviceDto>> GetCollarsAsync(int userId);
    Task<CollarDeviceDto?> GetCollarAsync(int userId, int collarId);
    Task<CollarDeviceDto?> UpdateCollarAsync(int userId, int collarId, UpdateCollarDeviceDto dto);
    Task<bool> DeleteCollarAsync(int userId, int collarId);

    // NFC Verification
    Task<NfcVerifyResponseDto> VerifyNfcAsync(string collarId, NfcVerifyRequestDto dto);

    // Location
    Task<int> UploadLocationsAsync(string collarId, LocationPointDto[] points);
    Task<IEnumerable<LocationQueryDto>> GetLocationHistoryAsync(int collarId, DateTime from, DateTime to);
    Task<CurrentLocationDto?> GetCurrentLocationAsync(int collarId);
}
