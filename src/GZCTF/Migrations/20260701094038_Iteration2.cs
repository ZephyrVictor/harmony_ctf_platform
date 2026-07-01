using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class Iteration2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ZeroDayBaseScore",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ZeroDayInfoMarkdown",
                table: "Games",
                type: "character varying(8192)",
                maxLength: 8192,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "BannedBy",
                table: "ChallengeTrackLevelBanStates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManual",
                table: "ChallengeTrackLevelBanStates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "ChallengeTrackLevelBanStates",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScoreAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Delta = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RelatedInfo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreAdjustments_AspNetUsers_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScoreAdjustments_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScoreAdjustments_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAdjustments_Category",
                table: "ScoreAdjustments",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAdjustments_CreatedAt",
                table: "ScoreAdjustments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAdjustments_GameId",
                table: "ScoreAdjustments",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAdjustments_GameId_Category",
                table: "ScoreAdjustments",
                columns: new[] { "GameId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAdjustments_GameId_TeamId",
                table: "ScoreAdjustments",
                columns: new[] { "GameId", "TeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAdjustments_OperatorId",
                table: "ScoreAdjustments",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAdjustments_TeamId",
                table: "ScoreAdjustments",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScoreAdjustments");

            migrationBuilder.DropColumn(
                name: "ZeroDayBaseScore",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ZeroDayInfoMarkdown",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "BannedBy",
                table: "ChallengeTrackLevelBanStates");

            migrationBuilder.DropColumn(
                name: "IsManual",
                table: "ChallengeTrackLevelBanStates");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ChallengeTrackLevelBanStates");
        }
    }
}
