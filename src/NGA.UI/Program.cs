using NGA.UI.Extensions;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using Scalar.AspNetCore;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Application Start");

    var builder = WebApplication.CreateBuilder(args);

    // 配置 NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
    builder.Services.AddCustomCoreAPI()
        .AddCustomApiVersioning()
        .AddCustomFluentValidation()
        .AddCustomOptions(builder.Configuration)
        .AddCustomAuthentication(builder.Configuration) 
        .AddCustomInjection(builder.Configuration);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseAuthentication();

    app.UseAuthorization();

    app.UseCustomExceptionHandler();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application start failed");
    throw;
}
finally
{
    LogManager.Shutdown();
}
