using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.Domain.Assets;

public class VideoAsset : MediaAsset
{
    public const long MAX_SIZE = 5_368_709_120;

    public const string LOCATION = "videos";
    public const string RAW_PREFIX = "raw";
    public const string ALLOWED_CONTENT_TYPE = "video";

    public const string HLS_PREFIX = "hls";

    public const string MASTER_PLAYLIST_NAME = "master.m3u8";
    public const string STREAM_PLAYLIST_PATTERN = "%v_stream.m3u8";
    public const string SEGMENT_FILE_PATTERN = "%v_%06d.ts";

    public static readonly string[] AllowedExtensions = ["mp4", "mkv", "avi", "mov"];

    public VideoMetadata? Metadata { get; private set; }

    private VideoAsset()
    {
    }

    private VideoAsset(
        Guid id,
        MediaData mediaData,
        MediaStatus status,
        StorageKey key)
        : base(id, mediaData, status, AssetType.VIDEO, key)
    {
    }

    public static UnitResult<Error> Validate(MediaData mediaData)
    {
        if (!AllowedExtensions.Contains(mediaData.FileName.Extension))
        {
            return Error.Validation(
                "video.invalid.extension",
                $"File extension must be one of: {string.Join(", ", AllowedExtensions)}");
        }

        if (mediaData.ContentType.Category != MediaType.VIDEO)
        {
            return Error.Validation("video.invalid.content-type", $"File content type must be {ALLOWED_CONTENT_TYPE}");
        }

        if (mediaData.Size > MAX_SIZE)
        {
            return Error.Validation("video.invalid.size", $"File size must be less than {MAX_SIZE} bytes");
        }

        return UnitResult.Success<Error>();
    }

    public static Result<VideoAsset, Error> CreateForUpload(Guid id, MediaData mediaData)
    {
        UnitResult<Error> validationResult = Validate(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        Result<StorageKey, Error> key = StorageKey.Create(LOCATION, RAW_PREFIX, id.ToString());
        if (key.IsFailure)
            return key.Error;

        return new VideoAsset(
            id,
            mediaData,
            MediaStatus.UPLOADING,
            key.Value);
    }

    public Result<StorageKey, Error> GetHlsRootKey()
    {
        return StorageKey.Create(LOCATION, HLS_PREFIX, Id.ToString());
    }

    public Result<StorageKey, Error> GetHlsMasterPlaylistKey()
    {
        Result<StorageKey, Error> hlsRoot = GetHlsRootKey();
        if (hlsRoot.IsFailure)
            return hlsRoot.Error;

        return hlsRoot.Value.AppendKey(MASTER_PLAYLIST_NAME);
    }

    public void SetMetadata(VideoMetadata metadata)
    {
        Metadata = metadata;
    }

    public override bool RequiresProcessing() => true;

    public UnitResult<Error> StartProcessing()
    {
        if (Status != MediaStatus.UPLOADED)
        {
            return Error.Validation(
                "asset.invalid.status.transition",
                "Can only start processing from UPLOADED status");
        }

        if (!RequiresProcessing())
            return Error.Validation("asset.processing.not.required", "This asset type does not require processing");

        Status = MediaStatus.PROCESSING;
        UpdatedAt = DateTime.UtcNow;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> SetHlsMasterPlaylistKey(StorageKey value)
    {
        if (Status != MediaStatus.PROCESSING)
            return Error.Validation("video.invalid.status", "Can only set processed data during processing");

        Key = value;
        UpdatedAt = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> CompleteProcessing()
    {
        if (Status != MediaStatus.PROCESSING)
            return Error.Validation("asset.invalid.status.transition",
                "Can only complete processing from PROCESSING status");

        Status = MediaStatus.READY;
        UpdatedAt = DateTime.UtcNow;
        return UnitResult.Success<Error>();
    }
}