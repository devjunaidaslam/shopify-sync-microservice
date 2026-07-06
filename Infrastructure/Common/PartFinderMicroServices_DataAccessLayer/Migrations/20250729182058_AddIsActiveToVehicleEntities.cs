using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PartFinderMicroServices_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToVehicleEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add is_active column to VehicleTypes table
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "VehicleTypes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Add is_active column to VehicleYears table
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "VehicleYears",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Add is_active column to VehicleMakes table
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "VehicleMakes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Add is_active column to VehicleModels table
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "VehicleModels",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove is_active column from VehicleTypes table
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "VehicleTypes");

            // Remove is_active column from VehicleYears table
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "VehicleYears");

            // Remove is_active column from VehicleMakes table
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "VehicleMakes");

            // Remove is_active column from VehicleModels table
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "VehicleModels");
        }
    }
}
