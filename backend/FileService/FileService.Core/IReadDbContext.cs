using FileService.Domain.Assets;

namespace FileService.Core;

public interface IReadDbContext
{
    IQueryable<MediaAsset> MediaAssetsQuery { get; }
}