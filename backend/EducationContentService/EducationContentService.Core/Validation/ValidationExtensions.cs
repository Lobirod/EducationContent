using System.Text.Json;
using EducationContentService.Domain.Shared;
using FluentValidation.Results;

namespace EducationContentService.Core.Validation;

public static class ValidationExtensions
{
    public static Error ToError(this ValidationResult validationResult)
    {
        List<ValidationFailure> validationErrors = validationResult.Errors;

        IEnumerable<IReadOnlyList<ErrorMessage>> errors =
            from validationError in validationErrors
            let errorMessage = validationError.ErrorMessage
            let error = JsonSerializer.Deserialize<Error>(errorMessage)
            select error.Messages;
        
        return Error.Validation(errors.SelectMany(e => e));
    }
}