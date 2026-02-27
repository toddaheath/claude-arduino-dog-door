using DogDoor.Api.Data;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

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
        var imageHash = ComputeDHash(imageStream);

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
                Similarity = ComputeHammingSimilarity(imageHash, p.PHash!)
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

    /// <summary>
    /// Compute a difference hash (dHash): resize to 9x8 grayscale, compare adjacent
    /// horizontal pixels to produce a 64-bit hash. Robust to scaling, compression,
    /// and minor color/brightness changes.
    /// </summary>
    public static string ComputeDHash(Stream stream)
    {
        using var bitmap = SKBitmap.Decode(stream);
        if (bitmap is null) return new string('0', 16);

        // Resize to 9x8 (9 wide so we get 8 horizontal differences per row)
        using var resized = bitmap.Resize(new SKImageInfo(9, 8, SKColorType.Gray8), SKFilterQuality.Low);
        if (resized is null) return new string('0', 16);

        ulong hash = 0;
        int bit = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                var left = resized.GetPixel(x, y).Red;
                var right = resized.GetPixel(x + 1, y).Red;
                if (left > right)
                    hash |= 1UL << bit;
                bit++;
            }
        }

        return hash.ToString("X16");
    }

    /// <summary>
    /// Compute similarity as 1 - (hamming distance / total bits).
    /// Two identical images produce 1.0; completely different images produce ~0.5.
    /// </summary>
    public static double ComputeHammingSimilarity(string hash1, string hash2)
    {
        if (hash1.Length != hash2.Length || hash1.Length == 0) return 0;

        if (!ulong.TryParse(hash1, System.Globalization.NumberStyles.HexNumber, null, out var h1) ||
            !ulong.TryParse(hash2, System.Globalization.NumberStyles.HexNumber, null, out var h2))
            return 0;

        var xor = h1 ^ h2;
        int distance = System.Numerics.BitOperations.PopCount(xor);
        return 1.0 - (double)distance / 64;
    }
}
