using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Domain.MediaProcessing;
using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel;

namespace FileService.Infrastructure.Postgres.Repositories;

public class VideoProcessingRepository : IVideoProcessingRepository
{
    private readonly FileServiceDbContext _dbContext;

    public VideoProcessingRepository(FileServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<VideoProcess, Error>> GetBy(
        Expression<Func<VideoProcess, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        VideoProcess? videoProcessing = await _dbContext.VideoProcesses
            .Include(v => v.Steps)
            .FirstOrDefaultAsync(predicate, cancellationToken);

        if (videoProcessing is null)
            return GeneralErrors.NotFound();

        return videoProcessing;
    }

    public Result<Guid, Error> Add(VideoProcess videoProcessing)
    {
        _dbContext.VideoProcesses.Add(videoProcessing);
        return videoProcessing.Id;
    }
}