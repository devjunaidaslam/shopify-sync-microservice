using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartFinderMicroServices_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class OemIdaddedinvariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OEMId",
                table: "Variants",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Variants_OEMId",
                table: "Variants",
                column: "OEMId");

            migrationBuilder.AddForeignKey(
                name: "FK_Variants_OEMs_OEMId",
                table: "Variants",
                column: "OEMId",
                principalTable: "OEMs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Variants_OEMs_OEMId",
                table: "Variants");

            migrationBuilder.DropIndex(
                name: "IX_Variants_OEMId",
                table: "Variants");

            migrationBuilder.DropColumn(
                name: "OEMId",
                table: "Variants");
        }
    }
}
