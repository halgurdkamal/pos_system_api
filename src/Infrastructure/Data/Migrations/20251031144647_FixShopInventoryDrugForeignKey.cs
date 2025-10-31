using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pos_system_api.src.Infrastructure.Data.Migrations
{
    public partial class FixShopInventoryDrugForeignKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShopInventory_Drugs_DrugId",
                table: "ShopInventory");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Drugs_DrugId",
                table: "Drugs",
                column: "DrugId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShopInventory_Drugs_DrugId",
                table: "ShopInventory",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "DrugId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShopInventory_Drugs_DrugId",
                table: "ShopInventory");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Drugs_DrugId",
                table: "Drugs");

            migrationBuilder.AddForeignKey(
                name: "FK_ShopInventory_Drugs_DrugId",
                table: "ShopInventory",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
