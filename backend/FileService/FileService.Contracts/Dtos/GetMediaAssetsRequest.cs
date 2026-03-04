namespace FileService.Contracts.Dtos;

public record GetMediaAssetsRequest(IReadOnlyList<Guid> MediaAssetIds);