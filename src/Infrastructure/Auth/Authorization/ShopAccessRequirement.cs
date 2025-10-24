using Microsoft.AspNetCore.Authorization;

namespace pos_system_api.Infrastructure.Auth.Authorization;

/// <summary>
/// Authorization requirement for shop-specific access control.
/// Users can only access data for their assigned shop, except Admins who have access to all shops.
/// </summary>
public class ShopAccessRequirement : IAuthorizationRequirement
{
    public ShopAccessRequirement()
    {
    }
}
