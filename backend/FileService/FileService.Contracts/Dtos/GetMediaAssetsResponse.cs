namespace FileService.Contracts.Dtos;

public record GetMediaAssetsResponse(IReadOnlyList<GetMediaAssetsDto> MediaAssets);