using System.Net.Http.Json;
using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace EducationContentService.IntegrationTests;

public static class HttpResponseMessageExtensions
{
    public static async Task<Result<TResponse, Error>> HandleResponseAsync<TResponse>(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        try
        {
            Envelope<TResponse>? jsonResponse = await response.Content
                .ReadFromJsonAsync<Envelope<TResponse>>(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return jsonResponse?.Error ?? GeneralErrors.Failure("Error while reading response");
            }

            if (jsonResponse is null)
            {
                return GeneralErrors.Failure("Error while reading response");
            }

            if (jsonResponse.Error is not null)
            {
                return jsonResponse.Error;
            }

            if (jsonResponse.Result is null)
            {
                return GeneralErrors.Failure("Error while reading response");
            }

            return jsonResponse.Result;
        }
        catch
        {
            return GeneralErrors.Failure("Error while reading response");
        }
    }

    public static async Task<UnitResult<Error>> HandleResponseAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Envelope? startMultipartResponse = await response.Content
                .ReadFromJsonAsync<Envelope>(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return startMultipartResponse?.Error ?? GeneralErrors.Failure("Error while reading response");
            }

            if (startMultipartResponse is null)
            {
                return GeneralErrors.Failure("Error while reading response");
            }

            if (startMultipartResponse.Error is not null)
            {
                return startMultipartResponse.Error;
            }

            return UnitResult.Success<Error>();
        }
        catch
        {
            return GeneralErrors.Failure("Error while reading response");
        }
    }
}
