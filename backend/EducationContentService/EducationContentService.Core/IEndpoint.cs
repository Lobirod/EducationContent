using Microsoft.AspNetCore.Routing;

namespace EducationContentService.Web.EndpointsSettings;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}