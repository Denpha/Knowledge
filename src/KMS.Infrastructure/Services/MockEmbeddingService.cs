using KMS.Application.Interfaces;

namespace KMS.Infrastructure.Services;

public class MockEmbeddingService : IEmbeddingService, IAiEmbeddingService
{
    private readonly Random _random = new Random();
    private const int EmbeddingDimension = 384; // Common dimension for smaller models

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        // Simulate processing delay
        await Task.Delay(50, cancellationToken);

        // Generate random embedding for testing
        var embedding = new float[EmbeddingDimension];
        for (int i = 0; i < EmbeddingDimension; i++)
        {
            embedding[i] = (float)(_random.NextDouble() * 2 - 1); // Values between -1 and 1
        }

        // Normalize the vector
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < EmbeddingDimension; i++)
            {
                embedding[i] = (float)(embedding[i] / magnitude);
            }
        }

        return embedding;
    }

    public async Task<float[][]> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = new List<float[]>();
        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text, cancellationToken);
            embeddings.Add(embedding);
        }
        return embeddings.ToArray();
    }

    public double CalculateSimilarity(float[] embedding1, float[] embedding2)
    {
        if (embedding1.Length != embedding2.Length)
            throw new ArgumentException("Embeddings must have the same dimension");

        // Calculate cosine similarity
        double dotProduct = 0;
        double magnitude1 = 0;
        double magnitude2 = 0;

        for (int i = 0; i < embedding1.Length; i++)
        {
            dotProduct += embedding1[i] * embedding2[i];
            magnitude1 += embedding1[i] * embedding1[i];
            magnitude2 += embedding2[i] * embedding2[i];
        }

        magnitude1 = Math.Sqrt(magnitude1);
        magnitude2 = Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }

    public string GetModelName()
    {
        return "mock-embedding-model";
    }

    public int GetDimension()
    {
        return EmbeddingDimension;
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    // IAiEmbeddingService implementation
    async Task<List<float[]>> IAiEmbeddingService.GenerateEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken)
    {
        var embeddings = await GenerateEmbeddingsAsync(texts, cancellationToken);
        return embeddings.ToList();
    }
}