using EducationContentService.Web.EndpointsSettings;
using EducationContentService.Web.Features.Lessons;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Exceptions;

namespace EducationContentService.Web.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<CreateHandler>();
        
        return services
            .AddSerilogLogging(configuration)
            .AddOpenApiSpec()
            .AddEndpoints(typeof(IEndpoint).Assembly);
    }

    private static IServiceCollection AddOpenApiSpec(this IServiceCollection services)
    {
        services.AddOpenApi();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1",
                new OpenApiInfo
                {
                    Title = "Education Content Service",
                    Version = "v1",
                    Contact = new OpenApiContact { Name = "Aleev Nikita", Email = "aleev_nikita@mail.ru", },
                });
        });

        return services;
    }

    private static IServiceCollection AddSerilogLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((sp, lc) => lc
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(sp)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("ServiceName", "LessonService"));

        return services;
    }
}