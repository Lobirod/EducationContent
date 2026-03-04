using System.Net.Mime;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Core.FileStorage;
using FileService.Domain;
using FileService.Domain.Assets;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;
using ContentType = FileService.Domain.ContentType;

namespace FileService.Core.Features;

public class StartMultipartUpload : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/files/multipart-upload", async Task<EndpointResult<StartMultipartUploadResponse>>(
            [FromBody] StartMultipartUploadRequest request,
            [FromServices] StartMultipartUploadHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(request, cancellationToken));
    }
}

public sealed class StartMultipartUploadHandler
{
    private readonly ILogger<StartMultipartUploadHandler> _logger;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly IChunkSizeCalculator _chunkSizeCalculator;
    private readonly IMediaAssetsRepository _mediaAssetsRepository;
    private readonly ITransactionManager _transactionManager;

    public StartMultipartUploadHandler(
        ILogger<StartMultipartUploadHandler> logger,
        IFileStorageProvider fileStorageProvider,
        IChunkSizeCalculator chunkSizeCalculator,
        IMediaAssetsRepository mediaAssetsRepository,
        ITransactionManager transactionManager)
    {
        _logger = logger;
        _fileStorageProvider = fileStorageProvider;
        _chunkSizeCalculator = chunkSizeCalculator;
        _mediaAssetsRepository = mediaAssetsRepository;
        _transactionManager = transactionManager;
    }

    public async Task<Result<StartMultipartUploadResponse, Error>> Handle(
        StartMultipartUploadRequest request,
        CancellationToken cancellationToken)
    {
        Result<FileName, Error> fileNameResult = FileName.Create(request.FileName);
        if (fileNameResult.IsFailure)
            return fileNameResult.Error;

        Result<ContentType, Error> contentTypeResult = ContentType.Create(request.ContentType);
        if (contentTypeResult.IsFailure)
            return contentTypeResult.Error;

        Result<(int ChunkSize, int TotalChunks), Error> chunkCalculationResult = _chunkSizeCalculator
            .CalculateChunkSize(request.Size);

        Result<MediaData, Error> mediaDataResult = MediaData.Create(
            fileNameResult.Value,
            contentTypeResult.Value,
            request.Size,
            chunkCalculationResult.Value.TotalChunks);

        Result<MediaAsset, Error> mediaAssetResult = MediaAsset.CreateForUpload(
            mediaDataResult.Value,
            request.AssetType.ToAssetType());

        _mediaAssetsRepository.Add(mediaAssetResult.Value, cancellationToken);

        await _transactionManager.SaveChangesAsync(cancellationToken);

        Result<string, Error> startUploadResult = await _fileStorageProvider.StartMultipartUploadAsync(
            mediaAssetResult.Value.UploadKey,
            mediaAssetResult.Value.MediaData,
            cancellationToken);

        if (startUploadResult.IsFailure)
            return startUploadResult.Error;

        Result<IReadOnlyList<ChunkUploadUrl>, Error> chunkUploadUrlsResult = await _fileStorageProvider
            .GenerateAllChunksUploadUrlsAsync(
                mediaAssetResult.Value.UploadKey,
                startUploadResult.Value,
                chunkCalculationResult.Value.TotalChunks,
                cancellationToken,
                useExternalEndpoint: true);

        if (chunkUploadUrlsResult.IsFailure)
            return chunkUploadUrlsResult.Error;

        _logger.LogInformation(
            "Media Asset started uploading: {MediaAssetId} with key: {StorageKey}",
            mediaAssetResult.Value.Id,
            mediaAssetResult.Value.Key);

        return new StartMultipartUploadResponse(
            mediaAssetResult.Value.Id,
            startUploadResult.Value,
            chunkUploadUrlsResult.Value,
            chunkCalculationResult.Value.ChunkSize);
    }
}