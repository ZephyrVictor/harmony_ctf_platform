using GZCTF.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260515110000_AddHybridLineFlagSpec")]
    public partial class AddHybridLineFlagSpec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "AnswerType",
                table: "ChallengeFlags",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "RawAnswer",
                table: "ChallengeFlags",
                type: "character varying(127)",
                maxLength: 127,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedAnswerKey",
                table: "ChallengeFlags",
                type: "character varying(143)",
                maxLength: 143,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "ChallengeFlags",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "ChallengeFlags",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "FlawNo",
                table: "ChallengeFlags",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Repository",
                table: "ChallengeFlags",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Module",
                table: "ChallengeFlags",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "ChallengeFlags",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cwe",
                table: "ChallengeFlags",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "ChallengeFlags"
                SET "RawAnswer" = "FlagValue"
                WHERE "RawAnswer" = ''
                """);

            migrationBuilder.AddColumn<int>(
                name: "GameId",
                table: "SubmissionRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChallengeId",
                table: "SubmissionRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParticipationId",
                table: "SubmissionRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeFlags_ChallengeId_NormalizedAnswerKey",
                table: "ChallengeFlags",
                columns: new[] { "ChallengeId", "NormalizedAnswerKey" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRecords_GameId",
                table: "SubmissionRecords",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRecords_ChallengeId_ParticipationId",
                table: "SubmissionRecords",
                columns: new[] { "ChallengeId", "ParticipationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRecords_ParticipationId_MatchedFlagId",
                table: "SubmissionRecords",
                columns: new[] { "ParticipationId", "MatchedFlagId" },
                unique: true,
                filter: "\"ParticipationId\" IS NOT NULL AND \"MatchedFlagId\" IS NOT NULL AND \"ScoreChange\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChallengeFlags_ChallengeId_NormalizedAnswerKey",
                table: "ChallengeFlags");

            migrationBuilder.DropIndex(
                name: "IX_SubmissionRecords_GameId",
                table: "SubmissionRecords");

            migrationBuilder.DropIndex(
                name: "IX_SubmissionRecords_ChallengeId_ParticipationId",
                table: "SubmissionRecords");

            migrationBuilder.DropIndex(
                name: "IX_SubmissionRecords_ParticipationId_MatchedFlagId",
                table: "SubmissionRecords");

            migrationBuilder.DropColumn(
                name: "AnswerType",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "RawAnswer",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "NormalizedAnswerKey",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "FlawNo",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "Repository",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "Module",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "Cwe",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "SubmissionRecords");

            migrationBuilder.DropColumn(
                name: "ChallengeId",
                table: "SubmissionRecords");

            migrationBuilder.DropColumn(
                name: "ParticipationId",
                table: "SubmissionRecords");
        }
    }
}
