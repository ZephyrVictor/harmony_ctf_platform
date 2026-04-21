using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddFlagScoringModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContainerExposePort",
                table: "GameChallenges",
                newName: "ExposePort");

            migrationBuilder.RenameColumn(
                name: "ContainerExposePort",
                table: "ExerciseChallenges",
                newName: "ExposePort");

            migrationBuilder.AddColumn<byte>(
                name: "NetworkMode",
                table: "GameChallenges",
                type: "smallint",
                nullable: true,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "NetworkMode",
                table: "ExerciseChallenges",
                type: "smallint",
                nullable: true,
                defaultValue: (byte)0);

            migrationBuilder.CreateTable(
                name: "BigScreenEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    TeamName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChallengeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ScoreChange = table.Column<int>(type: "integer", nullable: true),
                    IsConsumed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BigScreenEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BigScreenEvents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BigScreenEvents_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlagValue = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeFlags_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengePenalties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PenaltyScore = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengePenalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengePenalties_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetitionSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReviewComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RefundScore = table.Column<int>(type: "integer", nullable: false),
                    BonusScore = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubmissionId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoreChange = table.Column<int>(type: "integer", nullable: false),
                    IsPenalty = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmissionId = table.Column<int>(type: "integer", nullable: false),
                    MatchedFlagId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionRecords_ChallengeFlags_MatchedFlagId",
                        column: x => x.MatchedFlagId,
                        principalTable: "ChallengeFlags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SubmissionRecords_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BigScreenEvents_ChallengeId",
                table: "BigScreenEvents",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_BigScreenEvents_IsConsumed_CreatedAt",
                table: "BigScreenEvents",
                columns: new[] { "IsConsumed", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BigScreenEvents_UserId",
                table: "BigScreenEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeFlags_ChallengeId",
                table: "ChallengeFlags",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeFlags_ChallengeId_FlagValue",
                table: "ChallengeFlags",
                columns: new[] { "ChallengeId", "FlagValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChallengePenalties_ChallengeId",
                table: "ChallengePenalties",
                column: "ChallengeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionSettings_Key",
                table: "CompetitionSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRecords_IsPenalty",
                table: "SubmissionRecords",
                column: "IsPenalty",
                filter: "\"IsPenalty\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRecords_MatchedFlagId",
                table: "SubmissionRecords",
                column: "MatchedFlagId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRecords_SubmissionId",
                table: "SubmissionRecords",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ReviewerId",
                table: "Tickets",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status",
                table: "Tickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SubmissionId",
                table: "Tickets",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_UserId",
                table: "Tickets",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BigScreenEvents");

            migrationBuilder.DropTable(
                name: "ChallengePenalties");

            migrationBuilder.DropTable(
                name: "CompetitionSettings");

            migrationBuilder.DropTable(
                name: "SubmissionRecords");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "NetworkMode",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "NetworkMode",
                table: "ExerciseChallenges");

            migrationBuilder.RenameColumn(
                name: "ExposePort",
                table: "GameChallenges",
                newName: "ContainerExposePort");

            migrationBuilder.RenameColumn(
                name: "ExposePort",
                table: "ExerciseChallenges",
                newName: "ContainerExposePort");
        }
    }
}
