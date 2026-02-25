using System.Net;
using EducationContentService.Domain.Shared;
using Microsoft.AspNetCore.Http;

namespace EducationContentService.Core.Endpoints;

public class SuccessResult : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var envelope = Envelope.Ok();
        
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        
        return httpContext.Response.WriteAsJsonAsync(envelope);
    }
}

public class SuccessResult<TValue> : IResult
{
    private readonly TValue _value;
    
    public SuccessResult(TValue value)
    {
        _value = value;
    }
    
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var envelope = Envelope.Ok(_value);
        
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        
        return httpContext.Response.WriteAsJsonAsync(envelope);
    }
}