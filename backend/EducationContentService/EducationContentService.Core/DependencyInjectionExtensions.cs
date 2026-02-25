using EducationContentService.Core.Features.Lessons;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EducationContentService.Core;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
       services.AddScoped<CreateHandler>();
       services.AddScoped<GetHandler>();
       services.AddScoped<SoftDeleteHandler>();
       services.AddScoped<UpdateInfoHandler>();
       
       services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtensions).Assembly);
       
       return services;
    }
}