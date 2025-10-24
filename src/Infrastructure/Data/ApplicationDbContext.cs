using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Domain.Drugs.Entities;
using pos_system_api.Core.Domain.Shops.Entities;
using pos_system_api.Core.Domain.Suppliers.Entities;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Auth.Entities;
using pos_system_api.Core.Domain.PurchaseOrders.Entities;
using pos_system_api.Core.Domain.Sales.Entities;

namespace pos_system_api.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<Drug> Drugs { get; set; } = null!;
    public DbSet<Shop> Shops { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<ShopInventory> ShopInventory { get; set; } = null!;
    public DbSet<StockAdjustment> StockAdjustments { get; set; } = null!;
    public DbSet<StockTransfer> StockTransfers { get; set; } = null!;
    public DbSet<StockCount> StockCounts { get; set; } = null!;
    public DbSet<InventoryAlert> InventoryAlerts { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<ShopUser> ShopUsers { get; set; } = null!;
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } = null!;
    public DbSet<ReceiptRecord> ReceiptRecords { get; set; } = null!;
    public DbSet<SalesOrder> SalesOrders { get; set; } = null!;
    public DbSet<SalesOrderItem> SalesOrderItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
