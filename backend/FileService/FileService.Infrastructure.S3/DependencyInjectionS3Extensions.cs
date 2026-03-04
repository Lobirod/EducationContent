using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using FileService.Core;
using FileService.Core.FileStorage;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3;

public static class DependencyInjectionS3Extensions
{
    public static IServiceCollection AddS3(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection(nameof(FileStorageOptions)));
        
        services.AddScoped<IFileStorageProvider,  S3Provider>();
        
        services.AddSingleton<IAmazonS3>(sp =>
        {
            FileStorageOptions fileStorageOptions = sp.GetRequiredService<IOptions<FileStorageOptions>>().Value;

            var config = new AmazonS3Config
            {
                ServiceURL = fileStorageOptions.Endpoint, UseHttp = !fileStorageOptions.WithSsl, ForcePathStyle = true,
            };
            return new AmazonS3Client(fileStorageOptions.AccessKey, fileStorageOptions.SecretKey, config);
        });
        
        services.AddHostedService<S3BucketInitializationService>();
        services.AddTransient<IChunkSizeCalculator, ChunkSizeCalculator>();

        return services;
    }
}