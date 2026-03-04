using Amazon.S3;
using Amazon.S3.Model;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Core;
using FileService.Core.FileStorage;
using FileService.Core.Models;
using FileService.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.SharedKernel;
using CompleteMultipartUploadRequest = Amazon.S3.Model.CompleteMultipartUploadRequest;

namespace FileService.Infrastructure.S3;

public class S3Provider : IFileStorageProvider
{
    private readonly ILogger<S3Provider> _logger;
    private readonly IAmazonS3 _s3Client;
    private readonly FileStorageOptions _fileStorageOptions;

    private readonly SemaphoreSlim _requestsSemaphore;

    public S3Provider(IAmazonS3 s3Client, IOptions<FileStorageOptions> s3Options, ILogger<S3Provider> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
        _fileStorageOptions = s3Options.Value;
        _requestsSemaphore = new SemaphoreSlim(_fileStorageOptions.MaxConcurrentRequests);
    }

    public async Task<Result<string, Error>> StartMultipartUploadAsync(
        string bucketName,
        string key,
        string contentType,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new InitiateMultipartUploadRequest
            {
                BucketName = bucketName, Key = key, ContentType = contentType,
            };

            InitiateMultipartUploadResponse result =
                await _s3Client.InitiateMultipartUploadAsync(request, cancellationToken);

            return result.UploadId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting multipart upload");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<ChunkUploadUrl>, Error>> GenerateAllChunksUploadUrlsAsync(
        StorageKey storageKey,
        string uploadId,
        int totalChunks,
        CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Task<ChunkUploadUrl>> tasks = Enumerable.Range(1, totalChunks)
                .Select(async partNumber =>
                {
                    await _requestsSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var request = new GetPreSignedUrlRequest
                        {
                            BucketName = storageKey.Location,
                            Key = storageKey.Value,
                            Verb = HttpVerb.PUT,
                            UploadId = uploadId,
                            PartNumber = partNumber,
                            Expires = DateTime.UtcNow.AddHours(_fileStorageOptions.UploadUrlExpirationHours),
                            Protocol = _fileStorageOptions.WithSsl ? Protocol.HTTPS : Protocol.HTTP,
                        };

                        string? url = await _s3Client.GetPreSignedURLAsync(request);

                        return new ChunkUploadUrl(partNumber, url);
                    }
                    finally
                    {
                        _requestsSemaphore.Release();
                    }
                });

            ChunkUploadUrl[] results = await Task.WhenAll(tasks);

            return results;
        }
        catch (Exception ex)
        {
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> GenerateDownloadUrlAsync(
        string bucketName,
        string key)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(_fileStorageOptions.DownloadUrlExpirationDays),
                Protocol = _fileStorageOptions.WithSsl ? Protocol.HTTPS : Protocol.HTTP,
            };

            string? response = await _s3Client.GetPreSignedURLAsync(request);

            return response;
        }
        catch (Exception ex)
        {
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> GenerateUploadUrlAsync(
        string bucketName,
        string key)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddHours(6),
                Protocol = _fileStorageOptions.WithSsl ? Protocol.HTTPS : Protocol.HTTP,
            };

            string? response = await _s3Client.GetPreSignedURLAsync(request);

            return response;
        }
        catch (Exception ex)
        {
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> CompleteMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        IReadOnlyList<PartETagDto> partETags,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new CompleteMultipartUploadRequest
            {
                BucketName = storageKey.Location,
                Key = storageKey.Value,
                UploadId = uploadId,
                PartETags = partETags.Select(p => new PartETag { ETag = p.ETag, PartNumber = p.PartNumber, }).ToList(),
            };

            CompleteMultipartUploadResponse response =
                await _s3Client.CompleteMultipartUploadAsync(request, cancellationToken);

            return response.Key;
        }
        catch (Exception ex)
        {
            return S3ErrorMapper.ToError(ex);
        }
    }

    public Task<Result<string, Error>> StartMultipartUploadAsync(StorageKey storageKey, MediaData mediaData,
        CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<Result<IReadOnlyList<ChunkUploadUrl>, Error>> GenerateAllChunksUploadUrlsAsync(StorageKey storageKey,
        string uploadId, int totalChunks,
        CancellationToken cancellationToken, bool useExternalEndpoint = false) =>
        throw new NotImplementedException();

    public Task<Result<string, Error>>
        GenerateDownloadUrlAsync(StorageKey storageKey, bool useExternalEndpoint = false) =>
        throw new NotImplementedException();

    public async Task<Result<IReadOnlyList<MediaUrl>, Error>> GenerateDownloadUrlsAsync(
        IEnumerable<StorageKey> storageKeys,
        CancellationToken cancellationToken,
        bool useExternalEndpoint = false)
    {
        try
        {
            IEnumerable<Task<MediaUrl>> tasks = storageKeys.Select(async storageKey =>
            {
                await _requestsSemaphore.WaitAsync(cancellationToken);

                try
                {
                    var request = new GetPreSignedUrlRequest
                    {
                        BucketName = storageKey.Location,
                        Key = storageKey.Value,
                        Verb = HttpVerb.GET,
                        Expires = DateTime.UtcNow.AddDays(_fileStorageOptions.DownloadUrlExpirationDays),
                        Protocol = _fileStorageOptions.WithSsl ? Protocol.HTTPS : Protocol.HTTP
                    };

                    string? response = await _s3Client.GetPreSignedURLAsync(request);

                    //if (useExternalEndpoint)
                        //response = ReplaceEnpoint(response);

                    return new MediaUrl(storageKey, response);
                }
                finally
                {
                    _requestsSemaphore.Release();
                }
            });

            MediaUrl[] results = await Task.WhenAll(tasks);

            return results;
        }
        catch (Exception ex)
        {
            return S3ErrorMapper.ToError(ex);
        }
    }

    public Task<UnitResult<Error>> AbortMultipartUploadAsync(StorageKey storageKey, string uploadId,
        CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task<UnitResult<Error>> UploadFileAsync(StorageKey storageKey, FileStream fileStream, string contentType,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<UnitResult<Error>> DeleteFileAsync(StorageKey storageKey, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}