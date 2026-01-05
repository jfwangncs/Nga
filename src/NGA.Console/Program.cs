using JfYu.Data.Extension;
using JfYu.RabbitMQ;
using JfYu.Redis.Extensions;
using JfYu.Request.Extension;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NGA.Models;
using NGA.Models.Constant;
using NLog;
using NLog.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NGA.Console
{
    class Program
    {
        public static readonly string ServiceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "NGA.Console.Consumer";
        private static readonly ActivitySource ActivitySource = new ActivitySource(ServiceName);
        static void Main(string[] args)
        {
            var logger = LogManager.Setup().GetCurrentClassLogger();
            try
            {
                logger.Info("{ServiceName}启动", ServiceName);
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json")
                    .AddEnvironmentVariables()
                    .Build();

                HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

                builder.Services.AddOpenTelemetry()
               .ConfigureResource(resource => resource
                   .AddService(ServiceName)
                   .AddAttributes(new Dictionary<string, object>
                   {
                       ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                   }))
               .WithTracing(tracing => tracing
                    .AddSource(ServiceName)
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddRabbitMQInstrumentation()
                    .AddRedisInstrumentation()
                    .AddOtlpExporter())
               .WithMetrics(metrics => metrics
                    .AddMeter(ServiceName)
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddPrometheusHttpListener(options =>
                    {
                        options.UriPrefixes = ["http://+:9464/"];
                    }));

                builder.Logging.ClearProviders(); // 清除默认的日志提供程序
                builder.Logging.AddNLog(); // 添加 NLog 

                builder.Logging.AddOpenTelemetry(logging =>
                {
                    logging.IncludeFormattedMessage = true;
                    logging.IncludeScopes = true;
                    logging.AddOtlpExporter();  // 可选: 发送日志到 Loki
                });
                builder.Services.AddJfYuDbContext<DataContext>(options =>
                {
                    config.GetSection("ConnectionStrings").Bind(options);
                });


                builder.Services.AddRabbitMQ((options, policy) => { config.GetSection("RabbitMQ").Bind(options); });
                builder.Services.AddRedisService(options => { config.GetSection("Redis").Bind(options); });
                if (ServiceName == "NGA.Console.Producer")
                    builder.Services.AddHostedService<Producer>();
                else
                    builder.Services.AddHostedService<Consumer>();
                builder.Services.AddJfYuHttpClient(q => { q.HttpClientName = HttpClientName.NgaClientName; }, q => q.LoggingFields = JfYu.Request.Enum.JfYuLoggingFields.None);
                builder.Services.AddJfYuHttpClient(q => { q.HttpClientName = HttpClientName.QianWenClientName; }, q => q.LoggingFields = JfYu.Request.Enum.JfYuLoggingFields.None);
                builder.Services.Configure<ConsoleOptions>(config.GetSection("Console"));
                builder.Services.AddScoped<ILoginHelper, LoginHelper>();
                using IHost host = builder.Build();
                host.Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "application stopped");
                throw;
            }

        }
    }
}
