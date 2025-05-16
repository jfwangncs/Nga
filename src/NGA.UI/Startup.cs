using jfYu.Core.Data;
using jfYu.Core.Data.Extension;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NGA.Models;
using System;

namespace NGA.UI
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {          
            var builder = new ConfigurationBuilder()
             .SetBasePath(env.ContentRootPath)
              .AddJsonFile($"appsettings.{env.EnvironmentName ?? "Development"}.json", optional: true, reloadOnChange: true);
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews()
                .AddControllersAsServices()
               .AddNewtonsoftJson(options =>
               {
                   //jsonÉèÖÃ
                   options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
                   options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                   options.SerializerSettings.DateFormatString = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";
                   options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
               });


            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(5);
                options.Cookie.HttpOnly = true;
            });

            services.AddJfYuDbContextService<DataContext>(Configuration.GetRequiredSection("ConnectionStrings").Get<JfYuDBConfig>() ?? throw new NullReferenceException("ConnectionStrings is null"));

        }



        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            //ÆôÓÃcookie
            app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });

            app.UseSession();

            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllers();
            });

        }
    }
}
