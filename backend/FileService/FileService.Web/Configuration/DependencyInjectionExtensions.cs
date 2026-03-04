using FileService.Core;
using FileService.Infrastructure.Postgres;
using FileService.Infrastructure.S3;
using Framework.Endpoints;
using Framework.Logging;
using Framework.Swagger;

namespace FileService.Web.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddCore(configuration)
            .AddSerilogLogging(configuration, "FileService")
            .AddOpenApiSpec("FileService", "v1")
            .AddEndpoints(typeof(DependencyInjectionCoreExtensions).Assembly)
            .AddS3(configuration);
        
        services
            .AddCore(configuration)
            .AddInfrastructurePostgres(configuration);
        return services;
    }
}