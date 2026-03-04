using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using Shared.SharedKernel;

namespace EducationContentService.IntegrationTests.Mocks;

public class FileServiceCommunicationMock : IFileCommunicationService
{
    public Task<Result<CheckMediaAssetExistsResponse, Error>> CheckMediaAssetExists(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        var response = new CheckMediaAssetExistsResponse(true);
        return Task.FromResult(Result.Success<CheckMediaAssetExistsResponse, Error>(response));
    }

    public Task<Result<GetMediaAssetsResponse, Error>> GetMediaAssets(GetMediaAssetsRequest request, CancellationToken cancellationToken)
    {
        var result = new GetMediaAssetsResponse([
            new GetMediaAssetsDto(Guid.NewGuid(), "ready", "video", "url"),
            new GetMediaAssetsDto(Guid.NewGuid(), "ready", "video", "url"), 
            new GetMediaAssetsDto(Guid.NewGuid(), "ready", "video", "url"),
        ]);

        return Task.FromResult(Result.Success<GetMediaAssetsResponse, Error>(result));
    }
}
