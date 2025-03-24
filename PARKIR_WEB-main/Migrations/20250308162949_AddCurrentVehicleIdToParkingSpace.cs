using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkIRC.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentVehicleIdToParkingSpace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_ParkingSpaces_ParkingSpaceId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_ParkingSpaceId",
                table: "Vehicles");

            migrationBuilder.AddColumn<int>(
                name: "CurrentVehicleId",
                table: "ParkingSpaces",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ParkingSpaces",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ParkingSpaces",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpaces_CurrentVehicleId",
                table: "ParkingSpaces",
                column: "CurrentVehicleId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSpaces_Vehicles_CurrentVehicleId",
                table: "ParkingSpaces",
                column: "CurrentVehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSpaces_Vehicles_CurrentVehicleId",
                table: "ParkingSpaces");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSpaces_CurrentVehicleId",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "CurrentVehicleId",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ParkingSpaces");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_ParkingSpaceId",
                table: "Vehicles",
                column: "ParkingSpaceId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_ParkingSpaces_ParkingSpaceId",
                table: "Vehicles",
                column: "ParkingSpaceId",
                principalTable: "ParkingSpaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
