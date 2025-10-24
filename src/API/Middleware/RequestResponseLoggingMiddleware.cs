using System.Diagnostics;
using Serilog;
using Serilog.Context;

namespace pos_system_api.API.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses with performance metrics
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Start timing
        var stopwatch = Stopwatch.StartNew();
        
        // Get request details
        var requestMethod = context.Request.Method;
        var requestPath = context.Request.Path;
        var requestQuery = context.Request.QueryString.ToString();
        
        // Add request context to logs
        using (LogContext.PushProperty("RequestMethod", requestMethod))
        using (LogContext.PushProperty("RequestPath", requestPath))
        using (LogContext.PushProperty("RequestQuery", requestQuery))
        using (LogContext.PushProperty("UserAgent", context.Request.Headers["User-Agent"].ToString()))
        using (LogContext.PushProperty("RemoteIpAddress", context.Connection.RemoteIpAddress?.ToString()))
        {
            try
            {
                // Log incoming request
                _logger.LogInformation(
                    "HTTP {RequestMethod} {RequestPath}{RequestQuery} started",
                    requestMethod,
                    requestPath,
                    requestQuery);

                // Call the next middleware
                await _next(context);
                
                stopwatch.Stop();
                
                // Log response
                var statusCode = context.Response.StatusCode;
                var elapsedMs = stopwatch.ElapsedMilliseconds;
                
                var logLevel = statusCode >= 500 ? LogLevel.Error :
                              statusCode >= 400 ? LogLevel.Warning :
                              LogLevel.Information;
                
                _logger.Log(
                    logLevel,
                    "HTTP {RequestMethod} {RequestPath}{RequestQuery} responded {StatusCode} in {ElapsedMs}ms",
                    requestMethod,
                    requestPath,
                    requestQuery,
                    statusCode,
                    elapsedMs);
                
                // Log slow requests (> 5 seconds)
                if (elapsedMs > 5000)
                {
                    _logger.LogWarning(
                        "SLOW REQUEST: HTTP {RequestMethod} {RequestPath}{RequestQuery} took {ElapsedMs}ms",
                        requestMethod,
                        requestPath,
                        requestQuery,
                        elapsedMs);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(
                    ex,
                    "HTTP {RequestMethod} {RequestPath}{RequestQuery} failed after {ElapsedMs}ms",
                    requestMethod,
                    requestPath,
                    requestQuery,
                    stopwatch.ElapsedMilliseconds);
                
                throw;
            }
        }
    }
}
