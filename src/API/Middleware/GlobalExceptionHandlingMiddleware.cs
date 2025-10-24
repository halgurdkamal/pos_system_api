using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Common.Exceptions;

namespace pos_system_api.API.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions
/// and returns consistent ProblemDetails responses
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = exception switch
        {
            ValidationException validationException => HandleValidationException(validationException),
            UnauthorizedAccessException unauthorizedException => HandleUnauthorizedException(unauthorizedException),
            InvalidOperationException invalidOperationException => HandleInvalidOperationException(invalidOperationException),
            ArgumentException argumentException => HandleArgumentException(argumentException),
            NotFoundException notFoundException => HandleNotFoundException(notFoundException),
            KeyNotFoundException keyNotFoundException => HandleNotFoundException(keyNotFoundException),
            _ => HandleGenericException(exception)
        };

        // Log the exception
        LogException(exception, problemDetails);

        // Set response headers
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        // Write ProblemDetails to response
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private ProblemDetails HandleValidationException(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Error",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "One or more validation errors occurred.",
            Extensions = { ["errors"] = errors }
        };
    }

    private ProblemDetails HandleUnauthorizedException(UnauthorizedAccessException exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            Title = "Unauthorized",
            Status = (int)HttpStatusCode.Unauthorized,
            Detail = exception.Message
        };
    }

    private ProblemDetails HandleInvalidOperationException(InvalidOperationException exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = exception.Message
        };
    }

    private ProblemDetails HandleArgumentException(ArgumentException exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = exception.Message
        };
    }

    private ProblemDetails HandleNotFoundException(KeyNotFoundException exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Not Found",
            Status = (int)HttpStatusCode.NotFound,
            Detail = exception.Message
        };
    }

    private ProblemDetails HandleNotFoundException(NotFoundException exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Not Found",
            Status = (int)HttpStatusCode.NotFound,
            Detail = exception.Message
        };
    }

    private ProblemDetails HandleGenericException(Exception exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = "An unexpected error occurred. Please try again later."
            // Do not expose internal error details in production
        };
    }

    private void LogException(Exception exception, ProblemDetails problemDetails)
    {
        var logLevel = problemDetails.Status switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(
            logLevel,
            exception,
            "Exception handled: {ExceptionType} - {Message}",
            exception.GetType().Name,
            exception.Message
        );
    }
}
