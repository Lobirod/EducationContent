using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using FileService.Core.FileStorage;
using FileService.Core.Models;
using FileService.Domain;
using FileService.Domain.Assets;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Shared.SharedKernel;

namespace FileService.Core.Features;

public sealed class GetMediaAssets : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/files/batch", async Task<EndpointResult<GetMediaAssetsResponse>> (
            [FromBody] GetMediaAssetsRequest request,
            [FromServices] GetMediaAssetsHandler handler,
            CancellationToken token) => await handler.Handle(request, token));
    }

    public sealed class GetMediaAssetsHandler
    {
        private readonly IReadDbContext _readDbContext;
        private readonly IFileStorageProvider _fileStorageProvider;
        private readonly HybridCache _cache;
        private readonly FileStorageOptions _fileStorageOptions;

        public GetMediaAssetsHandler(
            IReadDbContext readDbContext,
            IFileStorageProvider fileStorageProvider,
            HybridCache cache,
            IOptions<FileStorageOptions> fileStorageOptions)
        {
            _readDbContext = readDbContext;
            _fileStorageProvider = fileStorageProvider;
            _cache = cache;
            _fileStorageOptions = fileStorageOptions.Value;
        }

        public async Task<Result<GetMediaAssetsResponse, Error>> Handle(
            GetMediaAssetsRequest request,
            CancellationToken cancellationToken)
        {
            if (!request.MediaAssetIds.Any())
                return new GetMediaAssetsResponse([]);

            List<MediaAsset> mediaAssets = await _readDbContext.MediaAssetsQuery
                .Where(m => request.MediaAssetIds.Contains(m.Id) && m.Status != MediaStatus.DELETED)
                .ToListAsync(cancellationToken);

            List<MediaAsset> readyMediaAssets = mediaAssets
                .Where(m => m.Status == MediaStatus.READY && m.Key != null)
                .ToList();

            List<StorageKey> keys = readyMediaAssets.Select(m => m.Key!).ToList();

            Dictionary<StorageKey, string> urls = await GetPresignedUrlsFromCache(keys, cancellationToken);
            
            var results = new List<GetMediaAssetsDto>();
            
            foreach (MediaAsset mediaAsset in mediaAssets)  
            {
                string? downloadUrl = null;

                if (mediaAsset.Key != null && urls.TryGetValue(mediaAsset.Key, out string? url))
                {
                    downloadUrl = url;
                }

                var mediaAssetDto = new GetMediaAssetsDto(
                    mediaAsset.Id,
                    mediaAsset.Status.ToString().ToLowerInvariant(),
                    mediaAsset.AssetType.ToString().ToLowerInvariant(),
                    downloadUrl);

                results.Add(mediaAssetDto);
            }

            return new GetMediaAssetsResponse(results);
        }

        private async Task<Dictionary<StorageKey, string>> GetPresignedUrlsFromCache(
            IEnumerable<StorageKey> storageKeys,
            CancellationToken cancellationToken)
        {
            var keys = storageKeys.ToList();

            if (!keys.Any())
                return [];

            IEnumerable<Task<(StorageKey key, string? url)>> cachedUrlsTasks = keys.Select(async key =>
            {
                string? url = await _cache.GetOrCreateAsync(
                    key: key.Value,
                    factory: _ => ValueTask.FromResult<string?>(null),
                    options: new HybridCacheEntryOptions()
                    {
                        Expiration = TimeSpan.FromDays(_fileStorageOptions.DownloadUrlExpirationDays)
                            .Subtract(TimeSpan.FromHours(1)),
                        LocalCacheExpiration = TimeSpan.FromHours(1),
                    },
                    cancellationToken: cancellationToken);

                return (key, url);
            });

            (StorageKey key, string? url)[] cachedUrls = await Task.WhenAll(cachedUrlsTasks);

            var result = new Dictionary<StorageKey, string>();
            var keysToGenerate = new List<StorageKey>();

            foreach ((StorageKey key, string? url) in cachedUrls)
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    result[key] = url;
                }
                else
                {
                    keysToGenerate.Add(key);
                }
            }

            if (!keysToGenerate.Any())
                return result;

            Result<IReadOnlyList<MediaUrl>, Error> mediaUrls = await _fileStorageProvider
                .GenerateDownloadUrlsAsync(keysToGenerate, cancellationToken, true);

            if (mediaUrls.IsFailure)
                return result;

            IEnumerable<Task> setTasks = mediaUrls.Value.Select(async mediaUrl =>
            {
                result[mediaUrl.StorageKey] = mediaUrl.PresignedUrl;

                await _cache.SetAsync(
                    key: mediaUrl.StorageKey.Value,
                    value: mediaUrl.PresignedUrl,
                    options: new HybridCacheEntryOptions
                    {
                        Expiration = TimeSpan.FromDays(_fileStorageOptions.DownloadUrlExpirationDays)
                            .Subtract(TimeSpan.FromHours(1)),
                    },
                    cancellationToken: cancellationToken);
            });

            await Task.WhenAll(setTasks);

            return result;
        }
    }
}
