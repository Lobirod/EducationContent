using EducationContentService.Core;
using EducationContentService.Infrastructure.Postgres;
using Framework.Endpoints;
using Framework.Logging;
using Framework.Swagger;

namespace EducationContentService.Web.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddCore(configuration)
            .AddInfrastructurePostgres(configuration)
            .AddSerilogLogging(configuration, "EducationContentService")
            .AddOpenApiSpec("EducationContentService", "v1")
            .AddEndpoints(typeof(DependencyInjectionCoreExtensions).Assembly);
    }
}