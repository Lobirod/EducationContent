using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Domain.MediaProcessing;
using Shared.SharedKernel;

namespace FileService.Core;

public interface IVideoProcessingRepository
{
    Task<Result<VideoProcess, Error>> GetBy(
        Expression<Func<VideoProcess, bool>> predicate,
        CancellationToken cancellationToken = default);

    Result<Guid, Error> Add(VideoProcess videoProcessing);
}
