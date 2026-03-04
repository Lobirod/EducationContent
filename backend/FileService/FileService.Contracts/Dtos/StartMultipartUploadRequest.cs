namespace FileService.Contracts.Dtos;

public record StartMultipartUploadRequest(
    string FileName,
    string AssetType,
    string ContentType,
    long Size);
    // string Context,
    // Guid ContextId);