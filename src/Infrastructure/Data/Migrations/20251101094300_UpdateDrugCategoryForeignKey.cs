using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pos_system_api.src.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Align Drugs.CategoryId foreign key with Categories.CategoryId (public code) instead of Categories.Id (PK).
    /// </summary>
    public partial class UpdateDrugCategoryForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing records so they reference the public CategoryId
            migrationBuilder.Sql(@"
                UPDATE ""Drugs"" d
                SET ""CategoryId"" = c.""CategoryId""
                FROM ""Categories"" c
                WHERE d.""CategoryId"" = c.""Id"";
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_Drugs_Categories_CategoryId",
                table: "Drugs");

            migrationBuilder.AddForeignKey(
                name: "FK_Drugs_Categories_CategoryId",
                table: "Drugs",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drugs_Categories_CategoryId",
                table: "Drugs");

            // Revert CategoryId values back to the primary key Id
            migrationBuilder.Sql(@"
                UPDATE ""Drugs"" d
                SET ""CategoryId"" = c.""Id""
                FROM ""Categories"" c
                WHERE d.""CategoryId"" = c.""CategoryId"";
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Drugs_Categories_CategoryId",
                table: "Drugs",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
