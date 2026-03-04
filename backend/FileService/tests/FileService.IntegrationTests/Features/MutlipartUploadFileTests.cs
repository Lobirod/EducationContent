using System.Net.Http.Json;
using Amazon.S3;
using Amazon.S3.Model;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Domain.Assets;
using FileService.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.SharedKernel;
using CompleteMultipartUploadRequest = FileService.Contracts.Dtos.CompleteMultipartUploadRequest;

namespace FileService.IntegrationTests.Features;

public class MutlipartUploadFileTests : FileServiceTestsBase
{
    private readonly IntegrationTestsWebFactory _factory;

    public MutlipartUploadFileTests(IntegrationTestsWebFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MultipartUpload_FullCycle_PersistsMediaFile()
    {
        // arrange
        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        FileInfo fileInfo = new(Path.Combine(AppContext.BaseDirectory, "Resources", TestFileName));

        // act
        StartMultipartUploadResponse startMultipartUploadResponse = await StartMultipartUpload(fileInfo, cancellationToken);

        IReadOnlyList<PartETagDto> partEtags = await UploadChunks(fileInfo, startMultipartUploadResponse, cancellationToken);

        UnitResult<Error> result = await CompleteMultipartUpload(startMultipartUploadResponse, partEtags, cancellationToken);

        // assert
        Assert.True(result.IsSuccess);

        await ExecuteInDb(async db =>
        {
            MediaAsset? mediaAsset = await db.MediaAssets
                .FirstOrDefaultAsync(m => m.Id == startMultipartUploadResponse.MediaAssetId, cancellationToken);

            Assert.Equal(MediaStatus.UPLOADED, mediaAsset?.Status);
            Assert.NotNull(mediaAsset);

            IAmazonS3 amazonS3Client = _factory.Services.GetRequiredService<IAmazonS3>();

            GetObjectResponse objectResponse = await amazonS3Client.GetObjectAsync(
                mediaAsset.UploadKey.Location,
                mediaAsset.UploadKey.Value,
                cancellationToken);

            Assert.Equal(objectResponse.ContentLength, fileInfo.Length);
            Assert.Equal(objectResponse.Key, mediaAsset.UploadKey.Value);
        });
    }

    private async Task<StartMultipartUploadResponse> StartMultipartUpload(FileInfo fileInfo, CancellationToken cancellationToken)
    {
        var request = new StartMultipartUploadRequest(
            fileInfo.Name,
            "video",
            "video/mp4",
            fileInfo.Length);

        // act
        HttpResponseMessage startMultipartResponse = await AppHttpClient
            .PostAsJsonAsync("/files/multipart-upload", request, cancellationToken);

        Result<StartMultipartUploadResponse, Error> startMultipartResult = await startMultipartResponse
            .HandleResponseAsync<StartMultipartUploadResponse>(cancellationToken);

        // assert
        Assert.True(startMultipartResult.IsSuccess);
        Assert.NotNull(startMultipartResult.Value.UploadId);

        await ExecuteInDb(async db =>
        {
            MediaAsset? mediaAsset = await db.MediaAssets
                .FirstOrDefaultAsync(m => m.Id == startMultipartResult.Value.MediaAssetId, cancellationToken);

            Assert.Equal(MediaStatus.UPLOADING, mediaAsset?.Status);
            Assert.NotNull(mediaAsset);
        });

        return startMultipartResult.Value;
    }

    private async Task<IReadOnlyList<PartETagDto>> UploadChunks(
        FileInfo fileInfo,
        StartMultipartUploadResponse startMultipartResponse,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = fileInfo.OpenRead();

        var parts = new List<PartETagDto>();

        foreach (ChunkUploadUrl chunkUploadUrl in startMultipartResponse.ChunkUploadUrls.OrderBy(c => c.PartNumber))
        {
            byte[] chunk = new byte[startMultipartResponse.ChunkSize];
            int bytesRead = await stream.ReadAsync(chunk.AsMemory(0, startMultipartResponse.ChunkSize), cancellationToken);
            if (bytesRead == 0)
                break;

            var content = new ByteArrayContent(chunk);

            HttpResponseMessage response = await HttpClient.PutAsync(chunkUploadUrl.UploadUrl, content, cancellationToken);

            string? etag = response.Headers.ETag?.Tag.Trim('"');

            parts.Add(new PartETagDto(chunkUploadUrl.PartNumber, etag!));
        }

        return parts;
    }

    private async Task<UnitResult<Error>> CompleteMultipartUpload(
        StartMultipartUploadResponse startMultipartUploadResponse,
        IEnumerable<PartETagDto> partETags,
        CancellationToken cancellationToken)
    {
        var completeRequest = new CompleteMultipartUploadRequest(
            startMultipartUploadResponse.MediaAssetId,
            startMultipartUploadResponse.UploadId,
            partETags.ToList());

        HttpResponseMessage completeResponse = await AppHttpClient.PostAsJsonAsync("/files/complete-upload", completeRequest, cancellationToken);

        UnitResult<Error> completeMultipart = await completeResponse
            .HandleResponseAsync(cancellationToken);

        return completeMultipart;
    }
}
