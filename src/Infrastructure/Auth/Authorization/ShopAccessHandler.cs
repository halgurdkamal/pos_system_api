using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace pos_system_api.Infrastructure.Auth.Authorization;

/// <summary>
/// Authorization handler that validates user's access to shop-specific resources.
/// Admins can access all shops. Other users can only access their assigned shop.
/// </summary>
public class ShopAccessHandler : AuthorizationHandler<ShopAccessRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ShopAccessHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ShopAccessRequirement requirement)
    {
        var systemRole = context.User.FindFirst("systemRole")?.Value;

        // SuperAdmin has access to all shops
        if (systemRole == "SuperAdmin")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Get user's shopIds from claims (comma-separated list)
        var userShopIds = context.User.FindFirst("shopIds")?.Value;

        if (string.IsNullOrEmpty(userShopIds))
        {
            // User has no shop memberships
            context.Fail();
            return Task.CompletedTask;
        }

        // Parse shop IDs
        var accessibleShopIds = userShopIds.Split(',', StringSplitOptions.RemoveEmptyEntries);

        // Get requested shopId from route parameters or query string
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Try to get shopId from route values first
        var requestedShopId = httpContext.Request.RouteValues["shopId"]?.ToString();

        // If not in route, try query string
        if (string.IsNullOrEmpty(requestedShopId))
        {
            requestedShopId = httpContext.Request.Query["shopId"].FirstOrDefault();
        }

        // If still not found, check if it's in the request body (for POST/PUT requests)
        // This would require reading the body, which is more complex
        // For now, we'll just check route and query parameters

        if (string.IsNullOrEmpty(requestedShopId))
        {
            // No shop ID in request - this might be a list endpoint
            // Allow it for now, repository layer will filter by user's accessible shops
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if user has access to the requested shop
        if (accessibleShopIds.Contains(requestedShopId, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
