using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.Domain;

public sealed record VideoMetadata
{
    public TimeSpan Duration { get; }

    public int Width { get; }

    public int Height { get; }

    private VideoMetadata()
    {
    }

    private VideoMetadata(TimeSpan duration, int width, int height)
    {
        Duration = duration;
        Width = width;
        Height = height;
    }

    public static Result<VideoMetadata, Error> Create(TimeSpan duration, int width, int height)
    {
        if (duration <= TimeSpan.Zero)
            return GeneralErrors.ValueIsInvalid("duration");

        if (width <= 0 || height <= 0)
            return GeneralErrors.ValueIsInvalid("resolution");

        return new VideoMetadata(duration, width, height);
    }
}
