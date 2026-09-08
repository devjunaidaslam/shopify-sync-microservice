using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopifySync_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldstoQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PageNumber",
                table: "ShopifyDataQueues",
                newName: "ProductShopifyId");

            migrationBuilder.AddColumn<string>(
                name: "Cursor",
                table: "ShopifyDataQueues",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Filter",
                table: "ShopifyDataQueues",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cursor",
                table: "ShopifyDataQueues");

            migrationBuilder.DropColumn(
                name: "Filter",
                table: "ShopifyDataQueues");

            migrationBuilder.RenameColumn(
                name: "ProductShopifyId",
                table: "ShopifyDataQueues",
                newName: "PageNumber");
        }
    }
}
