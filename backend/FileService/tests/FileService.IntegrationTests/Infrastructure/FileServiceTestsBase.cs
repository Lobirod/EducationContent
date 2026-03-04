using Amazon.S3;
using FileService.Infrastructure.Postgres;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.IntegrationTests.Infrastructure;

[Collection("FileTestsCollection")]
public abstract class FileServiceTestsBase : IAsyncLifetime, IClassFixture<IntegrationTestsWebFactory>
{
    protected const string TestFileName = "test-file.mp4";

    private readonly IntegrationTestsWebFactory _factory;

    protected FileServiceTestsBase(IntegrationTestsWebFactory factory)
    {
        _factory = factory;
        AppHttpClient = factory.CreateClient();
        HttpClient = new HttpClient();
        Services = factory.Services;
    }

    protected IServiceProvider Services { get; init; }

    protected HttpClient AppHttpClient { get; init; }
    
    protected HttpClient HttpClient { get; init; }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    protected async Task ExecuteInDb(Func<FileServiceDbContext, Task> action)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();

        FileServiceDbContext dbContext = scope.ServiceProvider.GetRequiredService<FileServiceDbContext>();

        await action(dbContext);
    }

    protected async Task ExecuteInS3(Func<IAmazonS3, Task> action)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();

        IAmazonS3 s3Client = scope.ServiceProvider.GetRequiredService<IAmazonS3>();

        await action(s3Client);
    }
}
