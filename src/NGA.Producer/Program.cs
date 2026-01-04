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
using System;
using System.Text;

namespace NGA.Producer
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = LogManager.Setup().GetCurrentClassLogger();
            try
            {
                logger.Info("启动");
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json").AddEnvironmentVariables().Build();
                HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

                builder.Logging.ClearProviders(); // 清除默认的日志提供程序
                builder.Logging.AddNLog(); // 添加 NLog 

                builder.Services.AddJfYuDbContext<DataContext>(options =>
                {
                    config.GetSection("ConnectionStrings").Bind(options);
                });
              

                builder.Services.AddRabbitMQ((options, policy) => { config.GetSection("RabbitMQ").Bind(options); });
                builder.Services.AddRedisService(options => { config.GetSection("Redis").Bind(options); });
                builder.Services.AddHostedService<Producer>();
                //if (Environment.GetEnvironmentVariable("ASPNETCORE_APPLICATION") == "Producer")
                //    builder.Services.AddHostedService<Producer>();
                //else
                //    builder.Services.AddHostedService<Consumer>();
                builder.Services.AddJfYuHttpClient(null, q => q.LoggingFields = JfYu.Request.Enum.JfYuLoggingFields.None);
                builder.Services.Configure<Ejiaimg>(config.GetSection("Ejiaimg"));
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
