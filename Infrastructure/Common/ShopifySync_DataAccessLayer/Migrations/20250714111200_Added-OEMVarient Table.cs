using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopifySync_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddedOEMVarientTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariantPrices_Locations_LocationId",
                table: "VariantPrices");

            migrationBuilder.DropIndex(
                name: "IX_VariantPrices_LocationId",
                table: "VariantPrices");

            migrationBuilder.DropColumn(
                name: "OEM",
                table: "Variants");

            migrationBuilder.DropColumn(
                name: "Vendor",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "LocationId",
                table: "VariantPrices",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "LocationId1",
                table: "VariantPrices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "CompareAtPriceMin",
                table: "Products",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "CompareAtPriceMax",
                table: "Products",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_VariantPrices_LocationId1",
                table: "VariantPrices",
                column: "LocationId1");

            migrationBuilder.AddForeignKey(
                name: "FK_VariantPrices_Locations_LocationId1",
                table: "VariantPrices",
                column: "LocationId1",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariantPrices_Locations_LocationId1",
                table: "VariantPrices");

            migrationBuilder.DropIndex(
                name: "IX_VariantPrices_LocationId1",
                table: "VariantPrices");

            migrationBuilder.DropColumn(
                name: "LocationId1",
                table: "VariantPrices");

            migrationBuilder.AddColumn<int>(
                name: "OEM",
                table: "Variants",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LocationId",
                table: "VariantPrices",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "CompareAtPriceMin",
                table: "Products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "CompareAtPriceMax",
                table: "Products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "Vendor",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariantPrices_LocationId",
                table: "VariantPrices",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_VariantPrices_Locations_LocationId",
                table: "VariantPrices",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
