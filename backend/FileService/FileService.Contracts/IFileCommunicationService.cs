using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using Shared.SharedKernel;

namespace FileService.Contracts;

public interface IFileCommunicationService
{
    Task<Result<GetMediaAssetsResponse, Error>> GetMediaAssets(
        GetMediaAssetsRequest request,
        CancellationToken cancellationToken);
}