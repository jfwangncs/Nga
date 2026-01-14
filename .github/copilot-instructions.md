# NGA Forum Scraper - AI Agent Instructions

## Project Overview

This is a .NET web scraping system for the NGA Chinese forum (bbs.nga.cn) with a **Producer-Consumer architecture** using RabbitMQ for async processing. The same console application switches roles via environment variable `OTEL_SERVICE_NAME`.

**Architecture:**

- **NGA.Console**: Producer scrapes topics, Consumer processes replies with AI analysis
- **NGA.Models**: EF Core entities (Topic, Replay, User, Black) with MySQL backend
- **NGA.UI**: Web API for health checks and management (v2 controllers, FluentValidation)

## Critical Development Knowledge

### Producer-Consumer Switching Pattern

The console app runs as **either** Producer **or** Consumer based on `OTEL_SERVICE_NAME` environment variable:

```csharp
// In Program.cs
if (ServiceName == "NGA.Console.Producer")
    builder.Services.AddHostedService<Producer>();
else
    builder.Services.AddHostedService<Consumer>();
```

When debugging/running:

- Set `OTEL_SERVICE_NAME=NGA.Console.Producer` for scraping mode
- Set `OTEL_SERVICE_NAME=NGA.Console.Consumer` for processing mode
- `ConsumerType` env var controls consumer behavior: "All" (full crawl) or "New" (new replies only)

### Key External Dependencies

This project uses **JfYu.\*** custom libraries extensively:

- `JfYu.Data`: Generic repository pattern (`IService<TEntity, TContext>`)
- `JfYu.RabbitMQ`: Message queue abstraction (`IRabbitMQService`)
- `JfYu.Redis`: Redis operations with distributed locks (`IRedisService`)
- `JfYu.Request`: HTTP client wrapper (`IJfYuRequest`) with cookie management

**Important:** These are custom packages, not standard .NET. Check their usage patterns in existing code before modifications.

### Authentication & Session Management

NGA forum requires **cookie-based authentication** stored in Redis:

```csharp
// Token stored as NGBToken in Redis
_token = await _redisService.GetAsync<NGBToken>("Token");
_ngaClient.RequestCookies.Add(new Cookie() {
    Name = "ngaPassportCid",
    Value = _token?.Token,
    Domain = ".nga.cn",
    Path = "/"
});
```

Login is handled by `LoginHelper` using distributed lock (`_redisService.LockTakeAsync("Login", ...)`) to prevent race conditions. Sessions expire - code checks for "访客不能直接访问" or "未登录" in HTML responses.

### Data Processing Patterns

**1. Distributed Locking for Duplicate Prevention:**

```csharp
// In Consumer.cs - prevent duplicate topic processing
if (!await _redisService.LockTakeAsync(topic.Tid, TimeSpan.FromHours(1)))
    return true;
```

**2. Blacklist Filtering:**

- `Black` entity contains comma-separated keywords
- Loaded once at startup in `Producer.ExecuteAsync()` for performance
- Applied using compiled regex patterns in `Consumer`

**3. HTML Parsing:**

- Uses HtmlAgilityPack with GB18030 encoding (NGA forum Chinese encoding)
- Pre-compiled regex patterns for BBCode cleaning (see Consumer static fields)

### OpenTelemetry Observability

**All services** are instrumented with OTEL tracing, metrics, and logs:

- Traces: Custom `ActivitySource` with span names like `"producer.process-fid"`
- Metrics: Prometheus endpoint on `:9464/` (e.g., `nga_producer_queued_items_total`)
- Logs: NLog + OTLP exporter to centralized system

When adding new operations, follow the pattern:

```csharp
using var activity = _activitySource.StartActivity("operation-name", ActivityKind.Internal);
activity?.SetTag("custom.field", value);
```

### Database Migrations

EF Core migrations require `EFConString` environment variable:

```bash
# Windows PowerShell
$env:EFConString="server=...;database=NGB"
dotnet ef migrations add MigrationName --project src/NGA.Models

# Linux/Mac
export EFConString="server=...;database=NGB"
dotnet ef database update --project src/NGA.Models
```

Connection strings in `appsettings.json` use custom `JfYuDbContext` configuration (see `DataContextFactory` for design-time support).

### Build & Deployment

**Docker Multi-Stage Build:**

- Targets .NET 10.0 (check Dockerfile for base image updates)
- Includes diagnostic tools: dotnet-dump, dotnet-trace, dotnet-gcdump
- GitHub Actions builds on `src/NGA.Console/**` or `src/NGA.Models/**` changes
- Tagged as `jfwangncs/ngb:${{ github.run_number }}`

**Local Development:**

```bash
# Build solution
dotnet build src/NGA.sln

# Run Producer
cd src/NGA.Console
$env:OTEL_SERVICE_NAME="NGA.Console.Producer"
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run

# Run Consumer (separate terminal)
$env:OTEL_SERVICE_NAME="NGA.Console.Consumer"
$env:ConsumerType="New"
dotnet run
```

### API Design Patterns (NGA.UI)

- **API Versioning:** Uses Asp.Versioning with v1/v2 controllers (see `Controllers/V2/`)
- **Response Format:** All endpoints return `BaseResponse<T>` with `Code`, `Message`, `Data`
- **Validation:** FluentValidation with custom validators in `Validations/`
- **Error Handling:** Global exception handler in `ServicesExtension.UseCustomExceptionHandler()`
- **OpenAPI:** Uses Scalar UI (`app.MapScalarApiReference()`) instead of Swagger

### Configuration Hierarchy

1. `appsettings.{Environment}.json` - base settings
2. Environment variables - override via `builder.Configuration.AddEnvironmentVariables()`
3. Secrets in Redis - runtime tokens (NGBToken)

**Never commit credentials.** Current `appsettings.Development.json` contains production credentials - should be moved to Azure Key Vault or GitHub Secrets.

## Testing & Debugging Tips

- Monitor RabbitMQ queues: Check `"ex_topic"` exchange for topic backlog
- Check Redis locks: Use `KEYS *` in Redis CLI to see active processing locks
- Prometheus metrics: `curl http://localhost:9464/metrics` for producer/consumer stats
- NLog outputs: Check console and file logs (nlog.config in both projects)

## Common Pitfalls

1. **Encoding Issues:** Always use `Encoding.GetEncoding("GB18030")` for NGA HTML
2. **Rate Limiting:** Producer has `RandomDelayExtension` - maintain delays to avoid bans
3. **Scope Management:** `IService<T, TContext>` is scoped - always use `IServiceScopeFactory` in hosted services
4. **Regex Performance:** Use `RegexOptions.Compiled` for frequently used patterns (see Consumer)
5. **Cookie Domain:** Must be `.nga.cn` (with leading dot) for subdomain access
