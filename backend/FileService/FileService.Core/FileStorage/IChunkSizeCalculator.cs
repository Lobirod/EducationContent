using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.Core.FileStorage;

public interface IChunkSizeCalculator
{
    Result<(int ChunkSize, int TotalChunks), Error> CalculateChunkSize(long fileSize);
}
