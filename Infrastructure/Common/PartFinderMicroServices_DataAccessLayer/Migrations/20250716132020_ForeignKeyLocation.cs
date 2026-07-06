using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartFinderMicroServices_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class ForeignKeyLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

           

            // Step 2: Drop the old column
            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "VariantPrices");

            // Step 3: Add the column again as int
            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "VariantPrices",
                type: "integer",
                nullable: false,
                defaultValue: 0); // Set defaultValue if necessary to avoid issues on existing rows

            // Step 4: Recreate index and foreign key
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariantPrices_Locations_LocationId",
                table: "VariantPrices");

            migrationBuilder.DropIndex(
                name: "IX_VariantPrices_LocationId",
                table: "VariantPrices");

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
    }
}
