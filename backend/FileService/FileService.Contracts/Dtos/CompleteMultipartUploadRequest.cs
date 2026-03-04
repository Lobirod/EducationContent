namespace FileService.Contracts.Dtos;

public record CompleteMultipartUploadRequest(Guid MediaAssetId, string UploadId, IReadOnlyList<PartETagDto> PartETags);