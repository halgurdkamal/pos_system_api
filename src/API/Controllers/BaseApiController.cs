using Microsoft.AspNetCore.Mvc;

namespace pos_system_api.API.Controllers;

/// <summary>
/// Base controller with common error handling functionality
/// </summary>
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Creates a BadRequest response with full error details including stack trace
    /// </summary>
    protected ActionResult CreateErrorResponse(Exception ex, int statusCode = 400)
    {
        var errorResponse = new
        {
            error = ex.Message,
            exceptionType = ex.GetType().FullName,
            stackTrace = ex.StackTrace,
            innerException = ex.InnerException?.Message,
            innerStackTrace = ex.InnerException?.StackTrace
        };

        return statusCode switch
        {
            400 => BadRequest(errorResponse),
            404 => NotFound(errorResponse),
            500 => StatusCode(500, errorResponse),
            _ => StatusCode(statusCode, errorResponse)
        };
    }

    /// <summary>
    /// Creates a BadRequest response with full error details
    /// </summary>
    protected BadRequestObjectResult BadRequestWithDetails(Exception ex)
    {
        return BadRequest(new
        {
            error = ex.Message,
            exceptionType = ex.GetType().FullName,
            stackTrace = ex.StackTrace,
            innerException = ex.InnerException?.Message,
            innerStackTrace = ex.InnerException?.StackTrace
        });
    }

    /// <summary>
    /// Creates a NotFound response with full error details
    /// </summary>
    protected NotFoundObjectResult NotFoundWithDetails(Exception ex)
    {
        return NotFound(new
        {
            error = ex.Message,
            exceptionType = ex.GetType().FullName,
            stackTrace = ex.StackTrace,
            innerException = ex.InnerException?.Message,
            innerStackTrace = ex.InnerException?.StackTrace
        });
    }
}
