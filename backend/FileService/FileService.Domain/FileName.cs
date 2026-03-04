using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.Domain;

public sealed record FileName
{
    public string Value { get; }

    public string Name { get; }

    public string Extension { get; }

    private FileName(string name, string extension)
    {
        Name = name;
        Extension = extension;
        Value = name + "." + extension;
    }

    public static Result<FileName, Error> Create(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return GeneralErrors.ValueIsInvalid(nameof(fileName));

        int lastDot = fileName.LastIndexOf('.');
        if (lastDot == -1 || lastDot == fileName.Length - 1)
            return GeneralErrors.ValueIsInvalid("File must have extension");

        string extension = fileName[(lastDot + 1)..].ToLowerInvariant();
        return new FileName(fileName, extension);
    }
}
