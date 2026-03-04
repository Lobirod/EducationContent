using EducationContentService.Core.Features.Lessons;
using FileService.Contracts.HttpCommunication;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EducationContentService.Core;

public static class DependencyInjectionCoreExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
       services.AddScoped<CreateHandler>();
       services.AddScoped<GetHandler>();
       services.AddScoped<SoftDeleteHandler>();
       services.AddScoped<UpdateInfoHandler>();
       
       services.AddValidatorsFromAssembly(typeof(DependencyInjectionCoreExtensions).Assembly);

       services.AddFileServiceHttpCommunication(configuration);
       
       return services;
    }
}