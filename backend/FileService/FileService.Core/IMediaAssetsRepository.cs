using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Domain.Assets;
using Shared.SharedKernel;

namespace FileService.Core;

public interface IMediaAssetsRepository
{
    Result<Guid, Error> Add(MediaAsset mediaAsset, CancellationToken cancellationToken = default);

    Task DeleteAsync(MediaAsset mediaAsset, CancellationToken cancellationToken);

    Task<Result<MediaAsset, Error>> GetBy(
        Expression<Func<MediaAsset, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<Result<VideoAsset, Error>> GetVideoBy(
        Expression<Func<VideoAsset, bool>> predicate,
        CancellationToken cancellationToken = default);
}
