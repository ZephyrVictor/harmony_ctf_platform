using GZCTF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260706193000_LevelAnswerStrategies")]
    public partial class LevelAnswerStrategies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CaseSensitive",
                table: "ChallengeLevelConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "OrSeparators",
                table: "ChallengeLevelConfigs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "、,;");

            migrationBuilder.AddColumn<bool>(
                name: "PathCaseSensitive",
                table: "ChallengeLevelConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "RangeSeparator",
                table: "ChallengeLevelConfigs",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "-");

            migrationBuilder.AddColumn<bool>(
                name: "StripLinePrefix",
                table: "ChallengeLevelConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaseSensitive",
                table: "ChallengeLevelConfigs");

            migrationBuilder.DropColumn(
                name: "OrSeparators",
                table: "ChallengeLevelConfigs");

            migrationBuilder.DropColumn(
                name: "PathCaseSensitive",
                table: "ChallengeLevelConfigs");

            migrationBuilder.DropColumn(
                name: "RangeSeparator",
                table: "ChallengeLevelConfigs");

            migrationBuilder.DropColumn(
                name: "StripLinePrefix",
                table: "ChallengeLevelConfigs");
        }
    }
}
