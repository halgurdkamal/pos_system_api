using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace pos_system_api.src.Infrastructure.Data.Migrations
{
    public partial class AddMultiTenantEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrugBatches");

            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "ReorderPoint",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "StorageLocation",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "SupplierContactNumber",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "SupplierEmail",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "TotalStock",
                table: "Drugs");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "Drugs",
                newName: "BasePricing_Currency");

            migrationBuilder.RenameColumn(
                name: "TaxRate",
                table: "Drugs",
                newName: "BasePricing_SuggestedTaxRate");

            migrationBuilder.RenameColumn(
                name: "SellingPrice",
                table: "Drugs",
                newName: "BasePricing_SuggestedRetailPrice");

            migrationBuilder.AlterColumn<string>(
                name: "BasePricing_Currency",
                table: "Drugs",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AddColumn<DateTime>(
                name: "BasePricing_LastPriceUpdate",
                table: "Drugs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Shops",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShopName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    LicenseNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address_Street = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Address_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address_State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address_ZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Address_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Contact_Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Contact_Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Contact_Website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DefaultTaxRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AutoReorderEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LowStockAlertThreshold = table.Column<int>(type: "integer", nullable: false),
                    OperatingHours = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shops", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SupplierType = table.Column<int>(type: "integer", nullable: false),
                    ContactNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address_Street = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Address_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address_State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address_ZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Address_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PaymentTerms = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeliveryLeadTime = table.Column<int>(type: "integer", nullable: false),
                    MinimumOrderValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TaxId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LicenseNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopInventory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShopId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DrugId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalStock = table.Column<int>(type: "integer", nullable: false),
                    ReorderPoint = table.Column<int>(type: "integer", nullable: false),
                    StorageLocation = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Batches = table.Column<string>(type: "jsonb", nullable: false),
                    Pricing_CostPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Pricing_SellingPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Pricing_Discount = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Pricing_Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Pricing_TaxRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Pricing_LastPriceUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    LastRestockDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopInventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopInventory_Drugs_DrugId",
                        column: x => x.DrugId,
                        principalTable: "Drugs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopInventory_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopInventory_DrugId",
                table: "ShopInventory",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopInventory_IsAvailable",
                table: "ShopInventory",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_ShopInventory_LastRestockDate",
                table: "ShopInventory",
                column: "LastRestockDate");

            migrationBuilder.CreateIndex(
                name: "IX_ShopInventory_ShopId",
                table: "ShopInventory",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopInventory_ShopId_DrugId",
                table: "ShopInventory",
                columns: new[] { "ShopId", "DrugId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopInventory_TotalStock",
                table: "ShopInventory",
                column: "TotalStock");

            migrationBuilder.CreateIndex(
                name: "IX_Shops_LicenseNumber",
                table: "Shops",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shops_RegistrationDate",
                table: "Shops",
                column: "RegistrationDate");

            migrationBuilder.CreateIndex(
                name: "IX_Shops_ShopName",
                table: "Shops",
                column: "ShopName");

            migrationBuilder.CreateIndex(
                name: "IX_Shops_Status",
                table: "Shops",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Email",
                table: "Suppliers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_IsActive",
                table: "Suppliers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierName",
                table: "Suppliers",
                column: "SupplierName");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierType",
                table: "Suppliers",
                column: "SupplierType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopInventory");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Shops");

            migrationBuilder.DropColumn(
                name: "BasePricing_LastPriceUpdate",
                table: "Drugs");

            migrationBuilder.RenameColumn(
                name: "BasePricing_Currency",
                table: "Drugs",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "BasePricing_SuggestedTaxRate",
                table: "Drugs",
                newName: "TaxRate");

            migrationBuilder.RenameColumn(
                name: "BasePricing_SuggestedRetailPrice",
                table: "Drugs",
                newName: "SellingPrice");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Drugs",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "Drugs",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Drugs",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ReorderPoint",
                table: "Drugs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StorageLocation",
                table: "Drugs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupplierContactNumber",
                table: "Drugs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupplierEmail",
                table: "Drugs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupplierId",
                table: "Drugs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "Drugs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalStock",
                table: "Drugs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DrugBatches",
                columns: table => new
                {
                    InventoryDrugId = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    QuantityOnHand = table.Column<int>(type: "integer", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugBatches", x => new { x.InventoryDrugId, x.Id });
                    table.ForeignKey(
                        name: "FK_DrugBatches_Drugs_InventoryDrugId",
                        column: x => x.InventoryDrugId,
                        principalTable: "Drugs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
