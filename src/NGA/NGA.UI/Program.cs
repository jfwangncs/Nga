using jfYu.Core.Data;
using jfYu.Core.Data.Extension;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NGA.Models;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
                .AddControllersAsServices()
               .AddNewtonsoftJson(options =>
               {
                   //json…Ë÷√
                   options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
                   options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                   options.SerializerSettings.DateFormatString = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";
                   options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
               });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(5);
    options.Cookie.HttpOnly = true;
});

builder.Services.AddJfYuDbContextService<DataContext>(builder.Configuration.GetRequiredSection("ConnectionStrings").Get<JfYuDBConfig>() ?? throw new NullReferenceException("ConnectionStrings is null"));


var app = builder.Build();


app.UseRouting();
//∆Ù”√cookie
app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });

app.UseSession();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run("http://*:5000");