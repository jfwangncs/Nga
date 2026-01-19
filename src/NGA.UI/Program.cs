using NGA.UI.Extensions;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using OpenTelemetry.Logs;
using Scalar.AspNetCore;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Application Start");

    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddNLog();
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.AddOtlpExporter();
    });

    builder.Services.AddControllers();
    builder.Services.AddCustomCoreAPI()
        .AddCustomScalar()
        .AddCustomApiVersioning()
        .AddCustomFluentValidation()
        .AddCustomOpenTelemetry()
        .AddCustomHttpLog()
        .AddCustomOptions(builder.Configuration)
        .AddCustomAuthentication(builder.Configuration)
        .AddCustomInjection(builder.Configuration);

    var app = builder.Build();
    app.UseHttpLogging();
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
