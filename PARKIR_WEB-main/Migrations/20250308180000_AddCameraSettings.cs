using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkIRC.Migrations
{
    /// <inheritdoc />
    public partial class AddCameraSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CameraSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileName = table.Column<string>(type: "TEXT", nullable: false),
                    ResolutionWidth = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1280),
                    ResolutionHeight = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 720),
                    ROI_X = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ROI_Y = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ROI_Width = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 640),
                    ROI_Height = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 480),
                    Exposure = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 100),
                    Gain = table.Column<double>(type: "REAL", nullable: false, defaultValue: 1.0),
                    Brightness = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 50),
                    Contrast = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 50),
                    LightingCondition = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Normal"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValue: DateTime.UtcNow),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CameraSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CameraSettings");
        }
    }
} 