namespace DogDoor.Api.Services;

public record RecognitionResult(int? AnimalId, string? AnimalName, double Confidence);

public interface IAnimalRecognitionService
{
    Task<RecognitionResult> IdentifyAsync(Stream imageStream);
}
