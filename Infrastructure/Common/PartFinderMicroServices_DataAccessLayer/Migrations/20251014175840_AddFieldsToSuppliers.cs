using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartFinderMicroServices_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldsToSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "Suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "Suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "supplier_number",
                table: "Suppliers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "code",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "supplier_number",
                table: "Suppliers");
        }
    }
}
