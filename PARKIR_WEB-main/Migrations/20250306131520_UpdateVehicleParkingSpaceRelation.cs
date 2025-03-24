using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkIRC.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVehicleParkingSpaceRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_ParkingSpaces_AssignedSpaceId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CurrentVehicleId",
                table: "ParkingSpaces");

            migrationBuilder.RenameColumn(
                name: "AssignedSpaceId",
                table: "Vehicles",
                newName: "ParkingSpaceId");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_AssignedSpaceId",
                table: "Vehicles",
                newName: "IX_Vehicles_ParkingSpaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_ParkingSpaces_ParkingSpaceId",
                table: "Vehicles",
                column: "ParkingSpaceId",
                principalTable: "ParkingSpaces",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_ParkingSpaces_ParkingSpaceId",
                table: "Vehicles");

            migrationBuilder.RenameColumn(
                name: "ParkingSpaceId",
                table: "Vehicles",
                newName: "AssignedSpaceId");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_ParkingSpaceId",
                table: "Vehicles",
                newName: "IX_Vehicles_AssignedSpaceId");

            migrationBuilder.AddColumn<int>(
                name: "CurrentVehicleId",
                table: "ParkingSpaces",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_ParkingSpaces_AssignedSpaceId",
                table: "Vehicles",
                column: "AssignedSpaceId",
                principalTable: "ParkingSpaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
