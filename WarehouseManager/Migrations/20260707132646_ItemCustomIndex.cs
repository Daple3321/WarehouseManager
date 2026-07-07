using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseManager.Migrations
{
    /// <inheritdoc />
    public partial class ItemCustomIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_CategoryId",
                table: "Items");

            migrationBuilder.CreateIndex(
                name: "IDX_Items_CategoryId_State",
                table: "Items",
                columns: new[] { "CategoryId", "State" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IDX_Items_CategoryId_State",
                table: "Items");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryId",
                table: "Items",
                column: "CategoryId");
        }
    }
}
