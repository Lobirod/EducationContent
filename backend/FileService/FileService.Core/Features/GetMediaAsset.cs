using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using FileService.Core.FileStorage;
using FileService.Domain.Assets;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel;

namespace FileService.Core.Features;

public sealed class GetMediaAsset : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/files/{mediaAssetId:guid}", async Task<EndpointResult<GetMediaAssetDto?>> (
            Guid mediaAssetId,
            [FromServices] GetMediaAssetHandler handler,
            CancellationToken token) => await handler.Handle(mediaAssetId, token));
    }

    public sealed class GetMediaAssetHandler
    {
        private readonly IReadDbContext _readDbContext;
        private readonly IFileStorageProvider _fileStorageProvider;

        public GetMediaAssetHandler(
            IReadDbContext readDbContext,
            IFileStorageProvider fileStorageProvider)
        {
            _readDbContext = readDbContext;
            _fileStorageProvider = fileStorageProvider;
        }

        public async Task<Result<GetMediaAssetDto?, Error>> Handle(Guid mediaAssetId, CancellationToken cancellationToken)
        {
            MediaAsset? mediaAsset = await _readDbContext.MediaAssetsQuery
                .FirstOrDefaultAsync(m => m.Id == mediaAssetId, cancellationToken);

            if (mediaAsset == null)
                return Result.Success<GetMediaAssetDto?, Error>(null);

            string? url = null;

            if (mediaAsset.Status == MediaStatus.READY)
            {
                (_, bool isFailure, string presignedUrl, Error? error) = await _fileStorageProvider
                    .GenerateDownloadUrlAsync(mediaAsset.UploadKey);

                if (isFailure)
                    return error;

                url = presignedUrl;
            }

            var mediaAssetDto = new GetMediaAssetDto(
                mediaAsset.Id,
                mediaAsset.Status.ToString().ToLowerInvariant(),
                mediaAsset.AssetType.ToString().ToLowerInvariant(),
                url,
                mediaAsset.MediaData.Size,
                mediaAsset.MediaData.FileName.Value,
                mediaAsset.MediaData.ContentType.Value);

            return mediaAssetDto;
        }
    }
}