namespace pos_system_api.Core.Domain.Auth.Enums;

/// <summary>
/// System-level roles (across entire application)
/// </summary>
public enum SystemRole
{
    /// <summary>
    /// Super administrator - can access all shops and system settings
    /// </summary>
    SuperAdmin = 0,

    /// <summary>
    /// Regular user - access controlled per shop via ShopUser permissions
    /// </summary>
    User = 1
}
