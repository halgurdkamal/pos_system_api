using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pos_system_api.src.Infrastructure.Data.Migrations
{
    public partial class AddStockAdjustmentAuditTrail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockAdjustments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShopId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DrugId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AdjustmentType = table.Column<int>(type: "integer", nullable: false),
                    QuantityChanged = table.Column<int>(type: "integer", nullable: false),
                    QuantityBefore = table.Column<int>(type: "integer", nullable: false),
                    QuantityAfter = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AdjustedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AdjustedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_AdjustedAt",
                table: "StockAdjustments",
                column: "AdjustedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_AdjustmentType",
                table: "StockAdjustments",
                column: "AdjustmentType");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_DrugId",
                table: "StockAdjustments",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_ShopId",
                table: "StockAdjustments",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_ShopId_AdjustedAt",
                table: "StockAdjustments",
                columns: new[] { "ShopId", "AdjustedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockAdjustments");
        }
    }
}
