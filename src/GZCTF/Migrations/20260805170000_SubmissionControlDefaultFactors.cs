using GZCTF.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260805170000_SubmissionControlDefaultFactors")]
    public partial class SubmissionControlDefaultFactors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChallengeCooldownWrongAnswerLimitFactor",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "ChallengeDailyTeamSubmissionLimitFactor",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.AddColumn<int>(
                name: "ChallengeTotalTeamSubmissionLimitFactor",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 200);

            migrationBuilder.AddColumn<int>(
                name: "CooldownWrongAnswerLimitMode",
                table: "GameChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DailyTeamSubmissionLimitMode",
                table: "GameChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalTeamSubmissionLimitMode",
                table: "GameChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "GameChallenges"
                SET "CooldownWrongAnswerLimitMode" = CASE
                    WHEN "CooldownWrongAnswerLimitOverride" > 0 THEN 1
                    ELSE 0
                END,
                "DailyTeamSubmissionLimitMode" = CASE
                    WHEN "DailyTeamSubmissionLimit" > 0 THEN 1
                    ELSE 2
                END,
                "TotalTeamSubmissionLimitMode" = CASE
                    WHEN "TotalTeamSubmissionLimit" > 0 THEN 1
                    ELSE 2
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChallengeCooldownWrongAnswerLimitFactor",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ChallengeDailyTeamSubmissionLimitFactor",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ChallengeTotalTeamSubmissionLimitFactor",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "CooldownWrongAnswerLimitMode",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "DailyTeamSubmissionLimitMode",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "TotalTeamSubmissionLimitMode",
                table: "GameChallenges");
        }
    }
}
