using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace FileService.Infrastructure.Postgres.Repositories;

public class MediaAssetsRepository : IMediaAssetsRepository
{
    private readonly FileServiceDbContext _dbContext;
    private readonly ILogger<MediaAssetsRepository> _logger;

    public MediaAssetsRepository(FileServiceDbContext dbContext, ILogger<MediaAssetsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Result<Guid, Error> Add(MediaAsset mediaAsset, CancellationToken cancellationToken = default)
    {
        _dbContext.MediaAssets.Add(mediaAsset);
        return mediaAsset.Id;
    }

    public Task DeleteAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
    {
        _dbContext.MediaAssets.Remove(mediaAsset);
        return Task.CompletedTask;
    }

    public async Task<Result<MediaAsset, Error>> GetBy(
        Expression<Func<MediaAsset, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        MediaAsset? lesson = await _dbContext.MediaAssets.FirstOrDefaultAsync(predicate, cancellationToken);
        if (lesson is null)
            return GeneralErrors.NotFound(null, "медиа файл");

        return lesson;
    }

    public async Task<Result<VideoAsset, Error>> GetVideoBy(
        Expression<Func<VideoAsset, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        VideoAsset? lesson = await _dbContext.MediaAssets
            .OfType<VideoAsset>()
            .FirstOrDefaultAsync(predicate, cancellationToken);

        if (lesson is null)
            return GeneralErrors.NotFound(null, "медиа файл");

        return lesson;
    }
}
