using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pos_system_api.src.Infrastructure.Data.Migrations
{
    public partial class AddStockTransferSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockTransfers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromShopId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ToShopId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DrugId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InitiatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InitiatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_FromShopId",
                table: "StockTransfers",
                column: "FromShopId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_FromShopId_Status",
                table: "StockTransfers",
                columns: new[] { "FromShopId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_InitiatedAt",
                table: "StockTransfers",
                column: "InitiatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_Status",
                table: "StockTransfers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ToShopId",
                table: "StockTransfers",
                column: "ToShopId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ToShopId_Status",
                table: "StockTransfers",
                columns: new[] { "ToShopId", "Status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockTransfers");
        }
    }
}
