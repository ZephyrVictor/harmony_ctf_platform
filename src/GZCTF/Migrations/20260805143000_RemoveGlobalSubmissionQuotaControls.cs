using GZCTF.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260805143000_RemoveGlobalSubmissionQuotaControls")]
    public partial class RemoveGlobalSubmissionQuotaControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_Submissions_GameId_ChallengeId";
                DROP INDEX IF EXISTS "IX_Submissions_GameId_ChallengeId_SubmitTimeUtc";
                ALTER TABLE "GameChallenges" DROP COLUMN IF EXISTS "DailyGlobalSubmissionLimit";
                ALTER TABLE "GameChallenges" DROP COLUMN IF EXISTS "TotalGlobalSubmissionLimit";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyGlobalSubmissionLimit",
                table: "GameChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalGlobalSubmissionLimit",
                table: "GameChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_GameId_ChallengeId",
                table: "Submissions",
                columns: new[] { "GameId", "ChallengeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_GameId_ChallengeId_SubmitTimeUtc",
                table: "Submissions",
                columns: new[] { "GameId", "ChallengeId", "SubmitTimeUtc" });
        }
    }
}
