using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pos_system_api.src.Infrastructure.Data.Migrations
{
    public partial class AddShopPackagingOverridesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopPackagingOverrides",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShopId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DrugId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PackagingLevelId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ParentPackagingLevelId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ParentOverrideId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CustomUnitName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OverrideQuantityPerParent = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    SellingPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MinimumSaleQuantity = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    IsSellable = table.Column<bool>(type: "boolean", nullable: true),
                    IsDefaultSellUnit = table.Column<bool>(type: "boolean", nullable: true),
                    CustomLevelOrder = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopPackagingOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopPackagingOverrides_ShopInventory_ShopId_DrugId",
                        columns: x => new { x.ShopId, x.DrugId },
                        principalTable: "ShopInventory",
                        principalColumns: new[] { "ShopId", "DrugId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopPackagingOverride_DefaultSellUnit",
                table: "ShopPackagingOverrides",
                columns: new[] { "ShopId", "DrugId", "IsDefaultSellUnit" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopPackagingOverride_ShopDrugLevel",
                table: "ShopPackagingOverrides",
                columns: new[] { "ShopId", "DrugId", "PackagingLevelId" },
                unique: true,
                filter: "\"PackagingLevelId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ShopPackagingOverrides_ParentOverrideId",
                table: "ShopPackagingOverrides",
                column: "ParentOverrideId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopPackagingOverrides_ShopId_DrugId",
                table: "ShopPackagingOverrides",
                columns: new[] { "ShopId", "DrugId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopPackagingOverrides");
        }
    }
}
