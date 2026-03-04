namespace FileService.Contracts.Dtos;

public record GetMediaAssetsDto(
    Guid Id,
    string Status,
    string AssetType,
    string? Url);