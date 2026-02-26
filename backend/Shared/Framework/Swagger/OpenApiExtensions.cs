using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Framework.Swagger;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiSpec(this IServiceCollection services, string title, string version)
    {
        services.AddOpenApi();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version,
                new OpenApiInfo
                {
                    Title = title,
                    Version = version,
                    Contact = new OpenApiContact { Name = "Aleev Nikita", Email = "aleev_nikita@mail.ru", }
                });
        });

        return services;
    }
}
