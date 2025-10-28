using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using pos_system_api.Core.Domain.Drugs.ValueObjects;

#nullable disable

namespace pos_system_api.src.Infrastructure.Data.Migrations
{
    public partial class AddPackagingInfoToDrugs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinimumSaleQuantity",
                table: "ShopInventory",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopSpecificSellUnit",
                table: "ShopInventory",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackagingInfo_BaseUnit",
                table: "Drugs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "tablet");

            migrationBuilder.AddColumn<string>(
                name: "PackagingInfo_BaseUnitDisplayName",
                table: "Drugs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Tablet");

            migrationBuilder.AddColumn<bool>(
                name: "PackagingInfo_IsSubdivisible",
                table: "Drugs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "PackagingInfo_UnitType",
                table: "Drugs",
                type: "text",
                nullable: false,
                defaultValue: "Count");

            // Add PackagingLevels column as nullable first
            migrationBuilder.AddColumn<string>(
                name: "PackagingInfo_PackagingLevels",
                table: "Drugs",
                type: "jsonb",
                nullable: true);

            // Set default packaging structure for existing drugs
            migrationBuilder.Sql(@"
                UPDATE ""Drugs""
                SET ""PackagingInfo_PackagingLevels"" = '[
                    {
                        ""LevelNumber"": 1,
                        ""UnitName"": ""Tablet"",
                        ""BaseUnitQuantity"": 1,
                        ""IsSellable"": true,
                        ""IsDefault"": true,
                        ""IsBreakable"": false,
                        ""Barcode"": null,
                        ""MinimumSaleQuantity"": null
                    }
                ]'::jsonb
                WHERE ""PackagingInfo_PackagingLevels"" IS NULL;
            ");

            // Now make it NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "PackagingInfo_PackagingLevels",
                table: "Drugs",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinimumSaleQuantity",
                table: "ShopInventory");

            migrationBuilder.DropColumn(
                name: "ShopSpecificSellUnit",
                table: "ShopInventory");

            migrationBuilder.DropColumn(
                name: "PackagingInfo_BaseUnit",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "PackagingInfo_BaseUnitDisplayName",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "PackagingInfo_IsSubdivisible",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "PackagingInfo_PackagingLevels",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "PackagingInfo_UnitType",
                table: "Drugs");
        }
    }
}
