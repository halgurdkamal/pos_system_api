using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using pos_system_api.API.Extensions;
using pos_system_api.API.Middleware;
using Serilog;
using Serilog.Events;

// Bootstrap configuration so the logger can read sink settings (e.g. Seq URL) before
// the WebApplication builder runs. Logger creation deliberately happens before the
// builder so that any startup errors are still captured.
var bootstrapEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var bootstrapConfig = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{bootstrapEnv}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
    )
    .WriteTo.File(
        path: "logs/pos-system-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10485760
    ); // 10MB

// Optional structured-log shipping to Seq. Only enabled when configured.
// Local dev: docker run -d --name seq -p 5341:5341 -e ACCEPT_EULA=Y datalust/seq
// Then set Serilog:Seq:ServerUrl=http://localhost:5341 in appsettings.Development.json.
var seqUrl = bootstrapConfig["Serilog:Seq:ServerUrl"];
if (!string.IsNullOrWhiteSpace(seqUrl))
{
    var seqApiKey = bootstrapConfig["Serilog:Seq:ApiKey"];
    loggerConfig.WriteTo.Seq(
        seqUrl,
        apiKey: string.IsNullOrWhiteSpace(seqApiKey) ? null : seqApiKey
    );
}

Log.Logger = loggerConfig.CreateLogger();

try
{
    Log.Information("Starting POS System API...");

    var builder = WebApplication.CreateBuilder(args);

    // Fail fast if required secrets are missing or weak.
    // Set them via environment variables (Jwt__SecretKey, ConnectionStrings__DefaultConnection)
    // or appsettings.Development.json (gitignored). Never commit real secrets.
    ValidateRequiredSecrets(builder.Configuration);

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Configure Kestrel for IIS hosting
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;
    });

    // Add layers (Clean Architecture)
    builder.Services.AddApplicationLayer();
    builder.Services.AddInfrastructureLayer(builder.Configuration);
    builder.Services.AddAPILayer(builder.Configuration);

    var app = builder.Build();

    // Seed database on startup (Development only)
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var seeder =
                services.GetRequiredService<pos_system_api.Infrastructure.Data.Seeders.DatabaseSeeder>();
            await seeder.SeedAllAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }

    // Configure the HTTP request pipeline.

    // Global exception handling (must be first)
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    // Request/Response logging (after exception handling)
    app.UseMiddleware<RequestResponseLoggingMiddleware>();

    // Use Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set(
                "UserAgent",
                httpContext.Request.Headers["User-Agent"].ToString()
            );

            // Add user info if authenticated
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("UserName", httpContext.User.Identity.Name);
                diagnosticContext.Set(
                    "UserId",
                    httpContext
                        .User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                        ?.Value
                );
            }
        };
    });

    app.UseSwagger();
    app.UseSwaggerUI();

    // Enable CORS
    app.UseCors();

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Map controllers (Clean Architecture endpoints)
    app.MapControllers();

    // Health check endpoints.
    //   /health/live  — process is up; never checks dependencies. Used by orchestrators
    //                   to decide whether to restart the container.
    //   /health/ready — dependencies (DB) are reachable; load balancers should send traffic.
    //   /health       — alias for liveness, for back-compat with existing callers.
    var liveOptions = new HealthCheckOptions
    {
        Predicate = _ => false, // run no checks; just confirm process is responding
        ResponseWriter = WriteHealthResponse,
    };
    var readyOptions = new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteHealthResponse,
    };
    app.MapHealthChecks("/health", liveOptions).AllowAnonymous();
    app.MapHealthChecks("/health/live", liveOptions).AllowAnonymous();
    app.MapHealthChecks("/health/ready", readyOptions).AllowAnonymous();

    app.MapGet("/", () => "Welcome to the POS System API! Visit /swagger for API documentation.");

    Log.Information("POS System API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Shutting down POS System API...");
    Log.CloseAndFlush();
}

static Task WriteHealthResponse(
    HttpContext context,
    Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report
)
{
    context.Response.ContentType = "application/json";
    var payload = new
    {
        status = report.Status.ToString(),
        timestamp = DateTime.UtcNow,
        totalDuration = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration = e.Value.Duration.TotalMilliseconds,
            description = e.Value.Description,
            error = e.Value.Exception?.Message,
        }),
    };
    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}

static void ValidateRequiredSecrets(IConfiguration configuration)
{
    var errors = new List<string>();

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        errors.Add(
            "ConnectionStrings:DefaultConnection is not set. Set it via the env var ConnectionStrings__DefaultConnection or appsettings.Development.json."
        );
    }

    var jwtSecret = configuration["Jwt:SecretKey"];
    if (string.IsNullOrWhiteSpace(jwtSecret))
    {
        errors.Add(
            "Jwt:SecretKey is not set. Set it via the env var Jwt__SecretKey or appsettings.Development.json."
        );
    }
    else if (jwtSecret.Length < 32)
    {
        errors.Add(
            $"Jwt:SecretKey is too short ({jwtSecret.Length} chars). Use at least 32 characters of random data; 64+ is recommended."
        );
    }
    else if (
        jwtSecret.Contains("YourSuperSecret", StringComparison.OrdinalIgnoreCase)
        || jwtSecret.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase)
        || jwtSecret.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
    )
    {
        errors.Add(
            "Jwt:SecretKey appears to be a placeholder. Generate a real random secret (e.g. `openssl rand -base64 64`)."
        );
    }

    if (errors.Count > 0)
    {
        var message =
            "Application cannot start due to missing or invalid configuration:\n  - "
            + string.Join("\n  - ", errors)
            + "\nSee SECURITY_SETUP.md for details.";
        throw new InvalidOperationException(message);
    }
}
