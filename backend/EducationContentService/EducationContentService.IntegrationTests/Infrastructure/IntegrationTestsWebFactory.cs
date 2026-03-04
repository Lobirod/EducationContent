using System.Data.Common;
using EducationContentService.Core.Database;
using EducationContentService.Infrastructure.Postgres;
using EducationContentService.IntegrationTests.Mocks;
using FileService.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Npgsql;
using Testcontainers.PostgreSql;

namespace EducationContentService.IntegrationTests.Infrastructure;

public class IntegrationTestsWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres")
        .WithDatabase("education_service_db_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    // private Respawner _respawner = null!;
    private DbConnection _dbConnection = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__Database", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__RabbitMq", "amqp://guest:guest@localhost:5672");

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        EducationDbContext dbContext = scope.ServiceProvider.GetRequiredService<EducationDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
        // await WolverineSchemaHelper.CreateTablesAsync(dbContext);

        _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();
        await InitializeRespawner();
    }

    public new async Task DisposeAsync()
    {
        if (_dbConnection is not null)
        {
            await _dbConnection.CloseAsync();
            await _dbConnection.DisposeAsync();
        }

        await base.DisposeAsync();

        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__Database", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__RabbitMq", null);
    }

    public async Task ResetDatabaseAsync()
    {
        // await _respawner.ResetAsync(_dbConnection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Tests");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Tests.json"), optional: true);
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _dbContainer.GetConnectionString(),
                ["ConnectionStrings:RabbitMq"] = "amqp://guest:guest@localhost:5672",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<EducationDbContext>();
            services.RemoveAll<IEducationReadDbContext>();
            //services.RemoveAll<IDbContextOutbox<EducationDbContext>>();

            services.AddDbContextPool<EducationDbContext>((sp, options) =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            services.AddDbContextPool<IEducationReadDbContext, EducationDbContext>((sp, options) =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            // services.DisableAllExternalWolverineTransports();
            // services.DisableAllWolverineMessagePersistence();

            services.RemoveAll<IFileCommunicationService>();
            services.AddScoped<IFileCommunicationService, FileServiceCommunicationMock>();
        });
    }

    private async Task InitializeRespawner()
    {
        /*
        _respawner = await Respawner.CreateAsync(
            _dbConnection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
            });
        */
    }
}
