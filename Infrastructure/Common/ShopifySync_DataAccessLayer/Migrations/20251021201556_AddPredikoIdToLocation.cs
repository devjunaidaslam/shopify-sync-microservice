using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopifySync_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPredikoIdToLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "prediko_id",
                table: "Locations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "prediko_id",
                table: "Locations");
        }
    }
}
