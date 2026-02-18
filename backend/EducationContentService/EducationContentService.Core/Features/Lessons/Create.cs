using EducationContentService.Web.EndpointsSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace EducationContentService.Web.Features.Lessons;

public class CreateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessons", async (CreateHandler handler) =>
        {
            await handler.Handle();
        });
    }
}

public sealed class CreateHandler
{
    private readonly ILogger<CreateHandler> _logger;

    public CreateHandler(ILogger<CreateHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle()
    {
        _logger.LogInformation("Creating a new lesson");
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}