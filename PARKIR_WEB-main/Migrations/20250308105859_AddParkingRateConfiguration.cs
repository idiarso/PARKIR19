using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkIRC.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingRateConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Journals_Operators_OperatorId",
                table: "Journals");

            migrationBuilder.DropForeignKey(
                name: "FK_Journals_ParkingTickets_ParkingTicketId",
                table: "Journals");

            migrationBuilder.DropForeignKey(
                name: "FK_Journals_Shifts_ShiftId",
                table: "Journals");

            migrationBuilder.DropForeignKey(
                name: "FK_Journals_Vehicles_VehicleId",
                table: "Journals");

            migrationBuilder.DropIndex(
                name: "IX_Journals_ParkingTicketId",
                table: "Journals");

            migrationBuilder.DropIndex(
                name: "IX_Journals_ShiftId",
                table: "Journals");

            migrationBuilder.DropIndex(
                name: "IX_Journals_VehicleId",
                table: "Journals");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Journals");

            migrationBuilder.DropColumn(
                name: "JournalNumber",
                table: "Journals");

            migrationBuilder.DropColumn(
                name: "ParkingTicketId",
                table: "Journals");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "Journals");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "Journals");

            migrationBuilder.RenameColumn(
                name: "TransactionType",
                table: "Journals",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "Journals",
                newName: "Action");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Operators",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Operators",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "Operators",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Operators",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Operators",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "OperatorId",
                table: "Journals",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Journals",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ParkingRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VehicleType = table.Column<string>(type: "TEXT", nullable: false),
                    BaseRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WeeklyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MonthlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PenaltyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingRates", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Journals_Operators_OperatorId",
                table: "Journals",
                column: "OperatorId",
                principalTable: "Operators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Journals_Operators_OperatorId",
                table: "Journals");

            migrationBuilder.DropTable(
                name: "ParkingRates");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Operators");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "Journals",
                newName: "TransactionType");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "Journals",
                newName: "TransactionDate");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Operators",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "OperatorId",
                table: "Journals",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Journals",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Journals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "JournalNumber",
                table: "Journals",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ParkingTicketId",
                table: "Journals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "Journals",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "Journals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Journals_ParkingTicketId",
                table: "Journals",
                column: "ParkingTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Journals_ShiftId",
                table: "Journals",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Journals_VehicleId",
                table: "Journals",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Journals_Operators_OperatorId",
                table: "Journals",
                column: "OperatorId",
                principalTable: "Operators",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Journals_ParkingTickets_ParkingTicketId",
                table: "Journals",
                column: "ParkingTicketId",
                principalTable: "ParkingTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Journals_Shifts_ShiftId",
                table: "Journals",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Journals_Vehicles_VehicleId",
                table: "Journals",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
