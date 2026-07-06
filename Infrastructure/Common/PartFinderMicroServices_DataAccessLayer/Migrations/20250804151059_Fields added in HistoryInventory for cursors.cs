using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartFinderMicroServices_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class FieldsaddedinHistoryInventoryforcursors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalRecords",
                table: "HistoryInventory",
                newName: "TotalProductsProcessed");

            migrationBuilder.AddColumn<DateTime>(
                name: "LocalCreatedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LocalUpdatedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalCursors",
                table: "HistoryInventory",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCursorsFailed",
                table: "HistoryInventory",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCursorsProcessed",
                table: "HistoryInventory",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocalCreatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LocalUpdatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TotalCursors",
                table: "HistoryInventory");

            migrationBuilder.DropColumn(
                name: "TotalCursorsFailed",
                table: "HistoryInventory");

            migrationBuilder.DropColumn(
                name: "TotalCursorsProcessed",
                table: "HistoryInventory");

            migrationBuilder.RenameColumn(
                name: "TotalProductsProcessed",
                table: "HistoryInventory",
                newName: "TotalRecords");
        }
    }
}
