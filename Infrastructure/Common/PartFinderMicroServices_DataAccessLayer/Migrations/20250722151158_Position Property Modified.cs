using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartFinderMicroServices_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class PositionPropertyModified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "position",
                table: "ProductOptions",
                newName: "Position");

            migrationBuilder.AddColumn<int>(
                name: "OEMId",
                table: "OEMParts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Products_VendorId",
                table: "Products",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Vendors_VendorId",
                table: "Products",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Vendors_VendorId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_VendorId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OEMId",
                table: "OEMParts");

            migrationBuilder.RenameColumn(
                name: "Position",
                table: "ProductOptions",
                newName: "position");
        }
    }
}
