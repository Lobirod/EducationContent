using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace EducationContentService.Domain.ValueObjects;

public record Title
{
    public const int MAX_LENGTH = 200;
    
    public string Value { get; }

    private Title(string value)
    {
        Value = value;
    }

    public static Result<Title, Error> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GeneralErrors.ValueIsInvalid("заголовок");
        }
        
        string normalized = Regex.Replace(value.Trim(), @"\s+", " ");

        if (normalized.Length > MAX_LENGTH)
        {
            return GeneralErrors.ValueIsInvalid("заголовок");
        }

        return new Title(value);
    }
}