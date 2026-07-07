using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseManager.Migrations
{
    /// <inheritdoc />
    public partial class ZoneFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Items_ZoneId",
                table: "Items",
                column: "ZoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Zones_ZoneId",
                table: "Items",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Zones_ZoneId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_ZoneId",
                table: "Items");
        }
    }
}
