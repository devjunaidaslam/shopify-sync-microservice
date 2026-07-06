using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartFinderMicroServices_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class removeOEMPartTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropPrimaryKey(
            //    name: "PK_OEMParts",
            //    table: "OEMParts");

            migrationBuilder.DropPrimaryKey(
               name: "OEMParts_pkey",
               table: "OEMParts");

            migrationBuilder.RenameTable(
                name: "OEMParts",
                newName: "OemVehicles");

            migrationBuilder.RenameColumn(
                name: "OEM",
                table: "Variants",
                newName: "OEMMetaField");

            migrationBuilder.RenameColumn(
                name: "OEMPartsId",
                table: "OemVehicles",
                newName: "OEMVehicleId");

            migrationBuilder.AlterColumn<string>(
                name: "Local_name",
                table: "VehicleModels",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OemVehicles",
                table: "OemVehicles",
                column: "OEMVehicleId");
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

            migrationBuilder.DropPrimaryKey(
                name: "PK_OemVehicles",
                table: "OemVehicles");

            migrationBuilder.DropColumn(
                name: "OEMId",
                table: "Variants");

            migrationBuilder.RenameTable(
                name: "OemVehicles",
                newName: "OEMParts");

            migrationBuilder.RenameColumn(
                name: "OEMMetaField",
                table: "Variants",
                newName: "OEM");

            migrationBuilder.RenameColumn(
                name: "OEMVehicleId",
                table: "OEMParts",
                newName: "OEMPartsId");

            migrationBuilder.AlterColumn<string>(
                name: "Local_name",
                table: "VehicleModels",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OEMParts",
                table: "OEMParts",
                column: "OEMPartsId");
        }
    }
}
