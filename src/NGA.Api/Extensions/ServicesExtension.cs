using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using JfYu.Data.Extension;
using JfYu.WeChat;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.OpenApi;
using NGA.Api.Model;
using NGA.Api.Options;
using NGA.Api.Services;
using NGA.Api.Services.Interfaces;
using NGA.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NGA.Api.Extensions
{
    public static class ServicesExtension
    {
        public static readonly string ServiceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "NGA.Api";
        public static IServiceCollection AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            return services;
        }


        public static IServiceCollection AddCustomInjection(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IJwtService, JwtService>();
            services.AddJfYuDbContext<DataContext>(options =>
            {
                configuration.GetSection("ConnectionStrings").Bind(options);
            });
            services.AddScoped<ITopicService, TopicService>();
            services.AddMiniProgram(options =>
            {
                configuration.GetSection("MiniProgram").Bind(options);
            });
            return services;
        }
        public static IServiceCollection AddCustomCoreAPI(this IServiceCollection services)
        {
            services.AddControllers().ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errorMsg = new Dictionary<string, ModelErrorCollection>();
                    if (!context.ModelState.IsValid)
                    {
                        var errors = context.ModelState.Where(q => q.Value != null && q.Value.Errors.Count > 0);
                        foreach (var error in errors)
                        {
                            if (error.Value != null)
                                errorMsg.Add(error.Key, error.Value.Errors);
                        }
                    }
                    return new BadRequestObjectResult(new BaseResponse<Dictionary<string, ModelErrorCollection>>() { Code = ResponseCode.Failed, ErrorCode = ErrorCode.ValidationError, Message = ErrorCode.ValidationError.GetDescription(), Data = errorMsg });
                };

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
                     new HeaderApiVersionReader("x-api-version"));
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
            return services;
        }

        public static IServiceCollection AddCustomScalar(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Components ??= new();
                    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                    // Define JWT Bearer scheme so Scalar can collect a token
                    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Description = "Input your JWT. The UI will send 'Authorization: Bearer {token}'."
                    };

                    return Task.CompletedTask;
                });
            });
            return services;
        }

        public static IServiceCollection AddCustomOpenTelemetry(this IServiceCollection services)
        {
            services.AddOpenTelemetry()
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
            return services;
        }

        public static IServiceCollection AddCustomNLog(this IServiceCollection services)
        {

            services.AddHttpLogging(logging =>
            {
                logging.LoggingFields = HttpLoggingFields.All;
                logging.RequestBodyLogLimit = 4096;
                logging.ResponseBodyLogLimit = 4096;
                logging.CombineLogs = true;
            });
            services.AddRedaction();
            services.AddHttpLoggingRedaction(op =>
            {
                op.RequestPathParameterRedactionMode = HttpRouteParameterRedactionMode.None;
                op.RequestPathLoggingMode = IncomingPathLoggingMode.Formatted;
                op.ExcludePathStartsWith.Add("/scalar");
                op.ExcludePathStartsWith.Add("/openapi");
                op.IncludeUnmatchedRoutes = true;
            });
            return services;
        }

        public static IServiceCollection AddCustomFluentValidation(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<Program>();
            services.AddFluentValidationAutoValidation(options => options.DisableDataAnnotationsValidation = true);
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
    }
}
