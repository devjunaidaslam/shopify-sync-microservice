using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopifySync_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPredikoIdToSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "supplier_prediko_id",
                table: "Suppliers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "supplier_prediko_id",
                table: "Suppliers");
        }
    }
}
