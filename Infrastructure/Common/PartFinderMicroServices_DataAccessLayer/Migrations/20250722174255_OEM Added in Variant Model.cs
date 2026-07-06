using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartFinderMicroServices_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class OEMAddedinVariantModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OEM",
                table: "Variants",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OEM",
                table: "Variants");
        }
    }
}
