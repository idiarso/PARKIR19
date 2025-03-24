using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkIRC.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVehicleParkingSpaceOnDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_ParkingSpaces_ParkingSpaceId",
                table: "Vehicles");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_ParkingSpaces_ParkingSpaceId",
                table: "Vehicles",
                column: "ParkingSpaceId",
                principalTable: "ParkingSpaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_ParkingSpaces_ParkingSpaceId",
                table: "Vehicles");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_ParkingSpaces_ParkingSpaceId",
                table: "Vehicles",
                column: "ParkingSpaceId",
                principalTable: "ParkingSpaces",
                principalColumn: "Id");
        }
    }
}
