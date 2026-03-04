using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Core.Models;
using FileService.Domain;
using Shared.SharedKernel;

namespace FileService.Core.FileStorage;

public interface IFileStorageProvider
{
    Task<Result<string, Error>> StartMultipartUploadAsync(
        StorageKey storageKey,
        MediaData mediaData,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ChunkUploadUrl>, Error>> GenerateAllChunksUploadUrlsAsync(
        StorageKey storageKey,
        string uploadId,
        int totalChunks,
        CancellationToken cancellationToken,
        bool useExternalEndpoint = false);

    Task<Result<string, Error>> GenerateDownloadUrlAsync(
        StorageKey storageKey,
        bool useExternalEndpoint = false);

    Task<Result<IReadOnlyList<MediaUrl>, Error>> GenerateDownloadUrlsAsync(
        IEnumerable<StorageKey> storageKeys,
        CancellationToken cancellationToken,
        bool useExternalEndpoint = false);

    Task<Result<string, Error>> CompleteMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        IReadOnlyList<PartETagDto> partETags,
        CancellationToken cancellationToken);

    Task<UnitResult<Error>> AbortMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        CancellationToken cancellationToken = default);

    Task<UnitResult<Error>> UploadFileAsync(
        StorageKey storageKey,
        FileStream fileStream,
        string contentType,
        CancellationToken cancellationToken);

    Task<UnitResult<Error>> DeleteFileAsync(StorageKey storageKey, CancellationToken cancellationToken);
}
