using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopifySync_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddOemToTempVehicleImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "oem",
                table: "TempVehicleImports",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "oem",
                table: "TempVehicleImports");
        }
    }
}
