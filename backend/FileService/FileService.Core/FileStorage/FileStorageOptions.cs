namespace FileService.Core.FileStorage;

public record FileStorageOptions
{
    public string Endpoint { get; init; } = string.Empty;
    
    public string ExternalEndpoint { get; init; } = string.Empty;
    
    public string AccessKey { get; init; } = string.Empty;
    
    public string SecretKey { get; init; } = string.Empty;

    public bool WithSsl { get; init; }

    public int DownloadUrlExpirationDays { get; init; } = 6;
    
    public IReadOnlyList<string> RequiredBuckets { get; init; } = [];
    
    public int UploadUrlExpirationHours { get; init; } = 1;
    
    public int MaxConcurrentRequests { get; init; } = 20;
    
    public long RecommendedChunkSizeBytes { get; init; } = 100 * 1024 * 1024; // 100 MB
    
    public int MaxChunks { get; init; } = 100;
}