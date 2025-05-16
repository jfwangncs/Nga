using jfYu.Core.Data.Extension;
using jfYu.Core.jfYuRequest;
using jfYu.Core.RabbitMQ;
using jfYu.Core.Redis.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NGA.Models;
using NGA.Models.Constant;
using System;
using System.Text;
using System.Threading;

namespace NGA.Producer
{
    class Program
    {
        static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(200, 200);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            System.Net.ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json").Build();
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddJfYuDbContextService<DataContext>(options =>
            {
                config.GetSection("ConnectionStrings").Bind(options);
            });
            builder.Logging.ClearProviders();
            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;          // 单行输出（可选）
                    options.IncludeScopes = false;      // 不显示作用域信息
                    options.TimestampFormat = null;     // 不显示时间戳
                    options.UseUtcTimestamp = false;    // 不使用UTC时间
                });
                loggingBuilder.SetMinimumLevel(LogLevel.Debug);
                loggingBuilder.AddFilter("System.Net.Http.HttpClient.*", LogLevel.Warning);
                loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore.Database.*", LogLevel.Warning);
            });
            builder.Services.AddRabbitMQService((options, policy) => { config.GetSection("RabbitMQ").Bind(options); });
            builder.Services.AddRedisService(options => { config.GetSection("Redis").Bind(options); });
            if (Environment.GetEnvironmentVariable("ASPNETCORE_APPLICATION") == "Producer")
                builder.Services.AddHostedService<Producer>();
            else
                builder.Services.AddHostedService<Consumer>();
            builder.Services.AddJfYuHttpRequestService(q => q.LoggingFields = jfYu.Core.jfYuRequest.Enum.JfYuLoggingFields.None);
            builder.Services.Configure<Ejiaimg>(config.GetSection("Ejiaimg"));

            using IHost host = builder.Build();
            host.Run();
        }
    }
}
