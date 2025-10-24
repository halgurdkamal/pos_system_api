using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace pos_system_api.src.Infrastructure.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Drugs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DrugId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BarcodeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BrandName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GenericName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OriginCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ImageUrls = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SideEffects = table.Column<List<string>>(type: "jsonb", nullable: false),
                    InteractionNotes = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Tags = table.Column<List<string>>(type: "jsonb", nullable: false),
                    RelatedDrugs = table.Column<List<string>>(type: "jsonb", nullable: false),
                    FormulationForm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FormulationStrength = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RouteOfAdministration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalStock = table.Column<int>(type: "integer", nullable: false),
                    ReorderPoint = table.Column<int>(type: "integer", nullable: false),
                    StorageLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IsPrescriptionRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsHighRisk = table.Column<bool>(type: "boolean", nullable: false),
                    DrugAuthorityNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ControlSchedule = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupplierId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SupplierContactNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SupplierEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drugs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DrugBatches",
                columns: table => new
                {
                    InventoryDrugId = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuantityOnHand = table.Column<int>(type: "integer", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_Drugs_Barcode",
                table: "Drugs",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drugs_DrugId",
                table: "Drugs",
                column: "DrugId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrugBatches");

            migrationBuilder.DropTable(
                name: "Drugs");
        }
    }
}
