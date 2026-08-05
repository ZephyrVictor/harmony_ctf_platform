using GZCTF.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260804190000_SubmissionQuotaControls")]
    public partial class SubmissionQuotaControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyTeamSubmissionLimit",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DailyTeamSubmissionLimit",
                table: "GameChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalTeamSubmissionLimit",
                table: "GameChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_GameId_ParticipationId_ChallengeId_SubmitTimeUtc",
                table: "Submissions",
                columns: new[] { "GameId", "ParticipationId", "ChallengeId", "SubmitTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_GameId_ParticipationId_SubmitTimeUtc",
                table: "Submissions",
                columns: new[] { "GameId", "ParticipationId", "SubmitTimeUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_GameId_ParticipationId_ChallengeId_SubmitTimeUtc",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_GameId_ParticipationId_SubmitTimeUtc",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "DailyTeamSubmissionLimit",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "DailyTeamSubmissionLimit",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "TotalTeamSubmissionLimit",
                table: "GameChallenges");
        }
    }
}
