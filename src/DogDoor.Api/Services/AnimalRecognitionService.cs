using DogDoor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Services;

public class AnimalRecognitionService : IAnimalRecognitionService
{
    private readonly DogDoorDbContext _db;

    public AnimalRecognitionService(DogDoorDbContext db)
    {
        _db = db;
    }

    public async Task<RecognitionResult> IdentifyAsync(Stream imageStream, int userId)
    {
        // Compute hash of the incoming camera image
        var imageHash = ComputeHash(imageStream);

        // Compare against all stored animal photo hashes scoped to this user
        var photos = await _db.AnimalPhotos
            .Include(p => p.Animal)
            .Where(p => p.PHash != null && p.Animal.UserId == userId)
            .ToListAsync();

        if (photos.Count == 0)
        {
            return new RecognitionResult(null, null, 0);
        }

        var bestMatch = photos
            .Select(p => new
            {
                Photo = p,
                Similarity = ComputeHashSimilarity(imageHash, p.PHash!)
            })
            .OrderByDescending(x => x.Similarity)
            .First();

        if (bestMatch.Similarity >= 0.6)
        {
            return new RecognitionResult(
                bestMatch.Photo.AnimalId,
                bestMatch.Photo.Animal.Name,
                bestMatch.Similarity);
        }

        return new RecognitionResult(null, null, bestMatch.Similarity);
    }

    private static string ComputeHash(Stream stream)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash)[..16];
    }

    private static double ComputeHashSimilarity(string hash1, string hash2)
    {
        // Compare hex string hashes character by character.
        // In production, use Hamming distance on actual perceptual hash bits.
        if (hash1.Length != hash2.Length) return 0;

        int matching = 0;
        for (int i = 0; i < hash1.Length; i++)
        {
            if (hash1[i] == hash2[i]) matching++;
        }

        return (double)matching / hash1.Length;
    }
}
