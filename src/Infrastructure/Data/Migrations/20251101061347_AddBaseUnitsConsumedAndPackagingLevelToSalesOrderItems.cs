using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pos_system_api.src.Infrastructure.Data.Migrations
{
    public partial class AddBaseUnitsConsumedAndPackagingLevelToSalesOrderItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PackagingLevelSold",
                table: "SalesOrderItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseUnitsConsumed",
                table: "SalesOrderItems",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackagingLevelSold",
                table: "SalesOrderItems");

            migrationBuilder.DropColumn(
                name: "BaseUnitsConsumed",
                table: "SalesOrderItems");
        }
    }
}
