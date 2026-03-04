using System.Data;
using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using FileService.Core.FileStorage;
using FileService.Domain.Assets;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace FileService.Core.Features;

public sealed class CompleteMultipartUpload : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/files/complete-upload", async Task<EndpointResult> (
            [FromBody] CompleteMultipartUploadRequest request,
            [FromServices] CompleteMultipartUploadHandler handler,
            CancellationToken token) => await handler.Handle(request, token));
    }
}

public sealed class CompleteMultipartUploadHandler
{
    private readonly ILogger<CompleteMultipartUploadHandler> _logger;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly IMediaAssetsRepository _mediaAssetsRepository;
    private readonly ITransactionManager _transactionManager;
   // private readonly ISchedulerFactory _schedulerFactory;
   // private readonly IEnumerable<IProcessingJobFactory> _processingJobFactories;

    public CompleteMultipartUploadHandler(
        ILogger<CompleteMultipartUploadHandler> logger,
        IFileStorageProvider fileStorageProvider,
        IMediaAssetsRepository mediaAssetsRepository,
       // ISchedulerFactory schedulerFactory,
       // IEnumerable<IProcessingJobFactory> processingJobFactories,
        ITransactionManager transactionManager)
    {
        _logger = logger;
        _fileStorageProvider = fileStorageProvider;
        _mediaAssetsRepository = mediaAssetsRepository;
        _transactionManager = transactionManager;
        // _schedulerFactory = schedulerFactory;
        // _processingJobFactories = processingJobFactories;
    }

    public async Task<UnitResult<Error>> Handle(CompleteMultipartUploadRequest request, CancellationToken cancellationToken)
    {
        (_, bool isFailure, MediaAsset? mediaAsset, Error? error) = await _mediaAssetsRepository
            .GetBy(m => m.Id == request.MediaAssetId, cancellationToken);

        if (isFailure)
            return error;

        if (mediaAsset.MediaData.ExpectedChunksCount != request.PartETags.Count)
            return GeneralErrors.Failure("Количество etags не соответствует количеству чанков");

        Result<string, Error> completeResult = await _fileStorageProvider.CompleteMultipartUploadAsync(
            mediaAsset.UploadKey,
            request.UploadId,
            request.PartETags,
            cancellationToken);

        try
        {
            //IDbTransaction transaction = await _transactionManager.BeginTransactionAsync(cancellationToken);

            if (completeResult.IsFailure)
            {
                mediaAsset.MarkFailed();
                await _transactionManager.SaveChangesAsync(cancellationToken);

                return completeResult.Error;
            }

            mediaAsset.MarkUploaded();

            await _transactionManager.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("File uploaded successfully. MediaAssetId: {MediaAssetId}", mediaAsset.Id);

            /*
            if (mediaAsset.RequiresProcessing())
            {
                IProcessingJobFactory? factory = _processingJobFactories.FirstOrDefault(f => f.CanProcess(mediaAsset));
                if (factory == null)
                {
                    _logger.LogError("No processing job factory found for MediaAssetId: {MediaAssetId}", mediaAsset.Id);
                    return GeneralErrors.Failure("No processing job factory found");
                }

                IScheduler scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

                IJobDetail job = factory.CreateJob(mediaAsset);
                ITrigger trigger = factory.CreateTrigger(mediaAsset);

                await scheduler.ScheduleJob(job, trigger, cancellationToken);

                _logger.LogInformation("Scheduled processing job for MediaAssetId: {MediaAssetId}", mediaAsset.Id);
            }
            else
            {
                UnitResult<Error> markReadyResult = mediaAsset.MarkReady();
                if (markReadyResult.IsFailure)
                    return markReadyResult.Error;

                _logger.LogInformation("MediaAssetId: {MediaAssetId} does not require processing. Marked as READY.", mediaAsset.Id);
            }
            */

            Result<int, Error> saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
            if (saveResult.IsFailure)
                return saveResult.Error;

            //transaction.Commit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing multipart upload for MediaAssetId: {MediaAssetId}", mediaAsset.Id);
            return GeneralErrors.Failure("Error completing multipart upload");
        }

        return Result.Success<Error>();
    }
}