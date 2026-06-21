using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiLevelFlagModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmissionRecords_ParticipationId_MatchedFlagId",
                table: "SubmissionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ChallengeFlags_ChallengeId_FlagValue",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "FlagMatchMode",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "FlagMatchMode",
                table: "ExerciseChallenges");

            migrationBuilder.DropColumn(
                name: "Cwe",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "FlawNo",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "Module",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "Repository",
                table: "ChallengeFlags");

            migrationBuilder.RenameColumn(
                name: "Score",
                table: "ChallengeFlags",
                newName: "Level");

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "SubmissionRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionType",
                table: "SubmissionRecords",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TrackId",
                table: "SubmissionRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LockDurationMinutes",
                table: "GameChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LockDurationMinutes",
                table: "ExerciseChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TrackId",
                table: "ChallengeFlags",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ChallengeLevelConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    SubmissionLimit = table.Column<int>(type: "integer", nullable: false),
                    PenaltyScore = table.Column<int>(type: "integer", nullable: false),
                    LockOnAnswers = table.Column<string>(type: "text", nullable: false),
                    BanOnAnswers = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeLevelConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeLevelConfigs_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeLockStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParticipationId = table.Column<int>(type: "integer", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    TriggeringLevel = table.Column<int>(type: "integer", nullable: false),
                    TriggeringTrackId = table.Column<Guid>(type: "uuid", nullable: true),
                    TriggerReason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TriggerAnswer = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: true),
                    LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UnlocksAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeLockStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeLockStates_Participations_ParticipationId",
                        column: x => x.ParticipationId,
                        principalTable: "Participations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeTrackLevelBanStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParticipationId = table.Column<int>(type: "integer", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    TrackId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    BanAnswer = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false),
                    BannedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeTrackLevelBanStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeTracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FlawNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Repository = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Module = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FilePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReferenceUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeTracks_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeWrongCountStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParticipationId = table.Column<int>(type: "integer", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    WrongCount = table.Column<int>(type: "integer", nullable: false),
                    LastResetAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeWrongCountStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRecords_ParticipationId_ChallengeId_TrackId_Level",
                table: "SubmissionRecords",
                columns: new[] { "ParticipationId", "ChallengeId", "TrackId", "Level" },
                filter: "\"ParticipationId\" IS NOT NULL AND \"TrackId\" IS NOT NULL AND \"ScoreChange\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeFlags_ChallengeId_FlagValue",
                table: "ChallengeFlags",
                columns: new[] { "ChallengeId", "FlagValue" });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeFlags_ChallengeId_Level_TrackId",
                table: "ChallengeFlags",
                columns: new[] { "ChallengeId", "Level", "TrackId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeLevelConfigs_ChallengeId_Level",
                table: "ChallengeLevelConfigs",
                columns: new[] { "ChallengeId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeLockStates_ParticipationId_ChallengeId",
                table: "ChallengeLockStates",
                columns: new[] { "ParticipationId", "ChallengeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeLockStates_ParticipationId_ChallengeId_UnlocksAt",
                table: "ChallengeLockStates",
                columns: new[] { "ParticipationId", "ChallengeId", "UnlocksAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeLockStates_UnlocksAt",
                table: "ChallengeLockStates",
                column: "UnlocksAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeTrackLevelBanStates_ParticipationId_ChallengeId_Tr~",
                table: "ChallengeTrackLevelBanStates",
                columns: new[] { "ParticipationId", "ChallengeId", "TrackId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeTracks_ChallengeId",
                table: "ChallengeTracks",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeTracks_ChallengeId_SortOrder",
                table: "ChallengeTracks",
                columns: new[] { "ChallengeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeWrongCountStates_ParticipationId_ChallengeId_Level",
                table: "ChallengeWrongCountStates",
                columns: new[] { "ParticipationId", "ChallengeId", "Level" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeLevelConfigs");

            migrationBuilder.DropTable(
                name: "ChallengeLockStates");

            migrationBuilder.DropTable(
                name: "ChallengeTrackLevelBanStates");

            migrationBuilder.DropTable(
                name: "ChallengeTracks");

            migrationBuilder.DropTable(
                name: "ChallengeWrongCountStates");

            migrationBuilder.DropIndex(
                name: "IX_SubmissionRecords_ParticipationId_ChallengeId_TrackId_Level",
                table: "SubmissionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ChallengeFlags_ChallengeId_FlagValue",
                table: "ChallengeFlags");

            migrationBuilder.DropIndex(
                name: "IX_ChallengeFlags_ChallengeId_Level_TrackId",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "SubmissionRecords");

            migrationBuilder.DropColumn(
                name: "SubmissionType",
                table: "SubmissionRecords");

            migrationBuilder.DropColumn(
                name: "TrackId",
                table: "SubmissionRecords");

            migrationBuilder.DropColumn(
                name: "LockDurationMinutes",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "LockDurationMinutes",
                table: "ExerciseChallenges");

            migrationBuilder.DropColumn(
                name: "TrackId",
                table: "ChallengeFlags");

            migrationBuilder.RenameColumn(
                name: "Level",
                table: "ChallengeFlags",
                newName: "Score");

            migrationBuilder.AddColumn<byte>(
                name: "FlagMatchMode",
                table: "GameChallenges",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "FlagMatchMode",
                table: "ExerciseChallenges",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "Cwe",
                table: "ChallengeFlags",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "ChallengeFlags",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlawNo",
                table: "ChallengeFlags",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Module",
                table: "ChallengeFlags",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Repository",
                table: "ChallengeFlags",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRecords_ParticipationId_MatchedFlagId",
                table: "SubmissionRecords",
                columns: new[] { "ParticipationId", "MatchedFlagId" },
                unique: true,
                filter: "\"ParticipationId\" IS NOT NULL AND \"MatchedFlagId\" IS NOT NULL AND \"ScoreChange\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeFlags_ChallengeId_FlagValue",
                table: "ChallengeFlags",
                columns: new[] { "ChallengeId", "FlagValue" },
                unique: true);
        }
    }
}
