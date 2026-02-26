using System.Globalization;
using EducationContentService.Web.Configuration;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web application");
    
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    
    var environment = builder.Environment.EnvironmentName;
    
    builder.Configuration.AddJsonFile($"appsettings.{environment}.json", true, true);
    
    builder.Configuration.AddEnvironmentVariables();

    builder.Services.AddConfiguration(builder.Configuration);

    WebApplication app = builder.Build();
    
    app.Configure();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
