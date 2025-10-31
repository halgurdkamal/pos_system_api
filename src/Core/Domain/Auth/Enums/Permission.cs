namespace pos_system_api.Core.Domain.Auth.Enums;

/// <summary>
/// Granular permissions that can be assigned to users per shop
/// Permissions are additive and can be combined
/// </summary>
public enum Permission
{
    // ==================== Sales ====================
    /// <summary>Process sales transactions (POS)</summary>
    ProcessSales = 1,

    /// <summary>View sales history</summary>
    ViewSales = 2,

    /// <summary>Process refunds and returns</summary>
    RefundSales = 3,

    /// <summary>Apply discounts to sales</summary>
    ApplyDiscounts = 4,

    // ==================== Inventory ====================
    /// <summary>View inventory levels</summary>
    ViewInventory = 10,

    /// <summary>Add new stock</summary>
    AddStock = 11,

    /// <summary>Reduce stock (wastage, damage)</summary>
    ReduceStock = 12,

    /// <summary>Update product pricing</summary>
    UpdatePricing = 13,

    /// <summary>Manage product catalog (add/edit drugs)</summary>
    ManageProducts = 14,

    /// <summary>Perform stock counts and audits</summary>
    StockAudit = 15,

    // ==================== Orders (Purchase Orders) ====================
    /// <summary>View purchase orders</summary>
    ViewOrders = 20,

    /// <summary>Create new purchase orders</summary>
    CreateOrders = 21,

    /// <summary>Approve purchase orders</summary>
    ApproveOrders = 22,

    /// <summary>Cancel purchase orders</summary>
    CancelOrders = 23,

    /// <summary>Receive orders (mark as delivered)</summary>
    ReceiveOrders = 24,

    // ==================== Suppliers ====================
    /// <summary>View supplier information</summary>
    ViewSuppliers = 30,

    /// <summary>Add/edit/remove suppliers</summary>
    ManageSuppliers = 31,

    // ==================== Customers ====================
    /// <summary>View customer information</summary>
    ViewCustomers = 40,

    /// <summary>Add/edit customer records</summary>
    ManageCustomers = 41,

    // ==================== Staff/Users ====================
    /// <summary>View shop staff members</summary>
    ViewStaff = 50,

    /// <summary>Invite new staff to the shop</summary>
    InviteStaff = 51,

    /// <summary>Remove staff from the shop</summary>
    RemoveStaff = 52,

    /// <summary>Update staff roles and permissions</summary>
    UpdateStaffPermissions = 53,

    // ==================== Reports ====================
    /// <summary>View financial and sales reports</summary>
    ViewReports = 60,

    /// <summary>Export reports to CSV/PDF</summary>
    ExportReports = 61,

    /// <summary>View detailed analytics</summary>
    ViewAnalytics = 62,

    // ==================== Shop Settings ====================
    /// <summary>Update shop information (name, address, etc.)</summary>
    UpdateShopInfo = 70,

    /// <summary>Configure receipt templates</summary>
    UpdateReceiptConfig = 71,

    /// <summary>Configure hardware (printers, scanners)</summary>
    UpdateHardwareConfig = 72,

    /// <summary>Manage payment methods</summary>
    ManagePaymentMethods = 73,

    /// <summary>Configure tax rates</summary>
    ManageTaxes = 74,

    // ==================== Financial ====================
    /// <summary>View financial data (revenue, expenses)</summary>
    ViewFinancials = 80,

    /// <summary>Record expenses</summary>
    RecordExpenses = 81,

    /// <summary>Close cash register/end-of-day</summary>
    CloseCashRegister = 82,

    // ==================== Advanced ====================
    /// <summary>Delete records (sales, orders, etc.)</summary>
    DeleteRecords = 90,

    /// <summary>View audit logs</summary>
    ViewAuditLogs = 91,

    /// <summary>Backup/restore shop data</summary>
    BackupData = 92
}
