using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pos_system_api.src.Infrastructure.Data.Migrations
{
    public partial class AddInventoryAlertsSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryAlerts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ShopId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DrugId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AlertType = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CurrentQuantity = table.Column<int>(type: "integer", nullable: true),
                    ThresholdQuantity = table.Column<int>(type: "integer", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAlerts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlerts_AlertType",
                table: "InventoryAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlerts_DrugId",
                table: "InventoryAlerts",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlerts_GeneratedAt",
                table: "InventoryAlerts",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlerts_Severity",
                table: "InventoryAlerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlerts_ShopId",
                table: "InventoryAlerts",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlerts_ShopId_AlertType_Status",
                table: "InventoryAlerts",
                columns: new[] { "ShopId", "AlertType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlerts_ShopId_Status",
                table: "InventoryAlerts",
                columns: new[] { "ShopId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlerts_Status",
                table: "InventoryAlerts",
                column: "Status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryAlerts");
        }
    }
}
