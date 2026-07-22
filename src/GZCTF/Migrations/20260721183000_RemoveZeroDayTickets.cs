using GZCTF.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260721183000_RemoveZeroDayTickets")]
    public partial class RemoveZeroDayTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZeroDayTickets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZeroDayTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BonusScore = table.Column<int>(type: "integer", nullable: false),
                    CWE = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ChallengeId = table.Column<int>(type: "integer", nullable: true),
                    CommitHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedTrackId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    LineNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Module = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PoC = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZeroDayTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZeroDayTickets_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ZeroDayTickets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZeroDayTickets_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ZeroDayTickets_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZeroDayTickets_ChallengeId",
                table: "ZeroDayTickets",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ZeroDayTickets_GameId",
                table: "ZeroDayTickets",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_ZeroDayTickets_ReviewerId",
                table: "ZeroDayTickets",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_ZeroDayTickets_Status",
                table: "ZeroDayTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ZeroDayTickets_UserId",
                table: "ZeroDayTickets",
                column: "UserId");
        }
    }
}
