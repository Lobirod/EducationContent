namespace FileService.Contracts.Dtos;

public record GetMediaAssetDto(
    Guid Id,
    string Status,
    string AssetType,
    string? Url,
    long? Size,
    string? FileName,
    string? ContentType);
