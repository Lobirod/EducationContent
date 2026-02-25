using Microsoft.AspNetCore.Routing;

namespace EducationContentService.Core.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}