using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics;
using NGA.UI.Model;
using NGA.UI.Options;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NGA.UI.Extensions
{
    public static class ServicesExtension
    {
        public static IServiceCollection AddCustomCoreAPI(this IServiceCollection services)
        {
            services.AddControllers().ConfigureApiBehaviorOptions(options =>
            {
                //options.InvalidModelStateResponseFactory = context =>
                //{
                //    var errorMsg = new Dictionary<string, ModelErrorCollection>();
                //    if (!context.ModelState.IsValid)
                //    {
                //        var errors = context.ModelState.Where(q => q.Value != null && q.Value.Errors.Count > 0);
                //        foreach (var error in errors)
                //        {
                //            if (error.Value != null)
                //                errorMsg.Add(error.Key, error.Value.Errors);
                //        }
                //    }
                //    return new BadRequestObjectResult(new BaseResponse<Dictionary<string, ModelErrorCollection>>() { Code = ResponseCode.VALIDATION_ERROR, Message = "数据验证错误", Data = errorMsg });
                //};

            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
            services.AddHttpContextAccessor();
            return services;
        }
        public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
        {
            // API Versioning
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                     new QueryStringApiVersionReader("api-version"),
                     new HeaderApiVersionReader("x-api-version"),
                     new MediaTypeApiVersionReader("v"));
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
            return services;
        }



        public static IServiceCollection AddCustomFluentValidation(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<Program>();
            services.AddFluentValidationAutoValidation(options => options.DisableDataAnnotationsValidation = true);
            return services;
        }

        public static IServiceCollection AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<Jwt>(configuration.GetSection("Jwt"));
            return services;
        }

        public static IServiceCollection AddCustomInjection(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

        public static void UseCustomExceptionHandler(this WebApplication app)
        {
            app.UseExceptionHandler(exceptionHandlerApp =>
            {
                exceptionHandlerApp.Run(async context =>
                {
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("GlobalExceptionHandler");
                    if (exceptionHandlerPathFeature != null)
                    {
                        var exception = exceptionHandlerPathFeature.Error;
                        var errorMessage = exceptionHandlerPathFeature.Error.InnerException?.Message ?? exceptionHandlerPathFeature.Error.Message;
                        logger.LogError(exception, "GlobalException: {ErrorMessage}", errorMessage);
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new BaseResponse<string>()
                        {
                            Code = ResponseCode.Error,
                            Message = ErrorCode.SystemError.GetDescription(),
                            ErrorCode = ErrorCode.SystemError,
                        }));
                    }
                });
            });
        }

        public static IServiceCollection AddCustomNlog(this IServiceCollection services)
        {

            return services;
        }
    }
}