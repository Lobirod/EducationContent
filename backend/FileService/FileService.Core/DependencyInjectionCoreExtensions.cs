using FileService.Core.Features;
using FluentValidation;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.Core;

public static class DependencyInjectionCoreExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
       services.AddValidatorsFromAssembly(typeof(DependencyInjectionCoreExtensions).Assembly);

       services.AddScoped<StartMultipartUploadHandler>();
       
       services.AddScoped<CompleteMultipartUploadHandler>();

       services.AddStackExchangeRedisCache(setup =>
       {
           setup.Configuration = "localhost:6379";
       });

       services.AddHybridCache(options =>
       {
           options.DefaultEntryOptions = new HybridCacheEntryOptions()
           {
               LocalCacheExpiration = TimeSpan.FromMinutes(5), 
               Expiration = TimeSpan.FromMinutes(30),
           };
       });
       
       return services;
    }
}