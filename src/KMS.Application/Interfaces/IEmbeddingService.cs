namespace KMS.Application.Interfaces;

public interface IEmbeddingService
{
    // Generate embedding for text
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    
    // Generate embeddings for multiple texts
    Task<float[][]> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
    
    // Calculate cosine similarity between two embeddings
    double CalculateSimilarity(float[] embedding1, float[] embedding2);
    
    // Get embedding model information
    string GetModelName();
    int GetDimension();
    
    // Check if service is available
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}