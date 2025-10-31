namespace pos_system_api.Core.Domain.Auth.Enums;

/// <summary>
/// Shop-level roles (predefined permission sets)
/// Users can be assigned a role plus additional custom permissions
/// </summary>
public enum ShopRole
{
    /// <summary>
    /// Shop owner - full access to all shop features (all permissions)
    /// </summary>
    Owner = 0,

    /// <summary>
    /// Manager - most permissions except ownership transfer and staff removal
    /// Can: manage inventory, orders, sales, reports, suppliers
    /// Cannot: remove owners, backup data, delete critical records
    /// </summary>
    Manager = 1,

    /// <summary>
    /// Cashier - POS operations only
    /// Can: process sales, view sales, apply discounts, view inventory
    /// Cannot: manage inventory, orders, settings, reports
    /// </summary>
    Cashier = 2,

    /// <summary>
    /// Inventory Clerk - stock management only
    /// Can: view inventory, add stock, reduce stock, stock audit, view orders
    /// Cannot: update pricing, process sales, manage settings
    /// </summary>
    InventoryClerk = 3,

    /// <summary>
    /// Viewer - read-only access
    /// Can: view sales, inventory, orders, reports
    /// Cannot: modify any data
    /// </summary>
    Viewer = 4,

    /// <summary>
    /// Custom - permissions defined individually (no predefined set)
    /// </summary>
    Custom = 99
}

/// <summary>
/// Helper class to get default permissions for each role
/// </summary>
public static class ShopRolePermissions
{
    public static List<Permission> GetPermissionsForRole(ShopRole role)
    {
        return role switch
        {
            ShopRole.Owner => new List<Permission>
            {
                // Owner has ALL permissions
                Permission.ProcessSales, Permission.ViewSales, Permission.RefundSales, Permission.ApplyDiscounts,
                Permission.ViewInventory, Permission.AddStock, Permission.ReduceStock, Permission.UpdatePricing,
                Permission.ManageProducts, Permission.StockAudit,
                Permission.ViewOrders, Permission.CreateOrders, Permission.ApproveOrders, Permission.CancelOrders, Permission.ReceiveOrders,
                Permission.ViewSuppliers, Permission.ManageSuppliers,
                Permission.ViewCustomers, Permission.ManageCustomers,
                Permission.ViewStaff, Permission.InviteStaff, Permission.RemoveStaff, Permission.UpdateStaffPermissions,
                Permission.ViewReports, Permission.ExportReports, Permission.ViewAnalytics,
                Permission.UpdateShopInfo, Permission.UpdateReceiptConfig, Permission.UpdateHardwareConfig,
                Permission.ManagePaymentMethods, Permission.ManageTaxes,
                Permission.ViewFinancials, Permission.RecordExpenses, Permission.CloseCashRegister,
                Permission.DeleteRecords, Permission.ViewAuditLogs, Permission.BackupData
            },

            ShopRole.Manager => new List<Permission>
            {
                // Manager has most permissions except critical operations
                Permission.ProcessSales, Permission.ViewSales, Permission.RefundSales, Permission.ApplyDiscounts,
                Permission.ViewInventory, Permission.AddStock, Permission.ReduceStock, Permission.UpdatePricing,
                Permission.ManageProducts, Permission.StockAudit,
                Permission.ViewOrders, Permission.CreateOrders, Permission.ApproveOrders, Permission.CancelOrders, Permission.ReceiveOrders,
                Permission.ViewSuppliers, Permission.ManageSuppliers,
                Permission.ViewCustomers, Permission.ManageCustomers,
                Permission.ViewStaff, Permission.InviteStaff, // Cannot remove staff or update permissions
                Permission.ViewReports, Permission.ExportReports, Permission.ViewAnalytics,
                Permission.UpdateReceiptConfig, // Cannot update shop info, hardware, payment methods
                Permission.ViewFinancials, Permission.RecordExpenses, Permission.CloseCashRegister,
                Permission.ViewAuditLogs // Cannot delete records or backup data
            },

            ShopRole.Cashier => new List<Permission>
            {
                // Cashier focuses on POS operations
                Permission.ProcessSales, Permission.ViewSales, Permission.RefundSales, Permission.ApplyDiscounts,
                Permission.ViewInventory, // Can check stock levels
                Permission.ViewCustomers, Permission.ManageCustomers, // Can add/edit customer info at POS
                Permission.CloseCashRegister // Can close their shift
            },

            ShopRole.InventoryClerk => new List<Permission>
            {
                // Inventory Clerk focuses on stock management
                Permission.ViewInventory, Permission.AddStock, Permission.ReduceStock, Permission.StockAudit,
                Permission.ViewOrders, Permission.CreateOrders, Permission.ReceiveOrders, // Can manage purchase orders
                Permission.ViewSuppliers // Can view supplier info when ordering
            },

            ShopRole.Viewer => new List<Permission>
            {
                // Viewer has read-only access
                Permission.ViewSales, Permission.ViewInventory, Permission.ViewOrders,
                Permission.ViewSuppliers, Permission.ViewCustomers, Permission.ViewStaff,
                Permission.ViewReports, Permission.ViewAnalytics, Permission.ViewFinancials
            },

            ShopRole.Custom => new List<Permission>(), // Custom role has no default permissions

            _ => new List<Permission>()
        };
    }
}
