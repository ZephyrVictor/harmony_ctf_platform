using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueParticipationPerTeamGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    duplicate_summary text;
                BEGIN
                    SELECT string_agg(
                        format('game=%s team=%s ids=%s', "GameId", "TeamId", ids),
                        '; ')
                    INTO duplicate_summary
                    FROM (
                        SELECT "GameId", "TeamId", array_agg("Id" ORDER BY "Id") AS ids
                        FROM "Participations"
                        GROUP BY "GameId", "TeamId"
                        HAVING count(*) > 1
                    ) duplicates;

                    IF duplicate_summary IS NOT NULL THEN
                        RAISE EXCEPTION
                            'Cannot enforce unique team participation because duplicates exist: %',
                            duplicate_summary
                        USING HINT =
                            'Review and merge the listed Participation IDs before applying this migration.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Participations_TeamId_GameId",
                table: "Participations");

            migrationBuilder.CreateIndex(
                name: "IX_Participations_TeamId_GameId",
                table: "Participations",
                columns: new[] { "TeamId", "GameId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Participations_TeamId_GameId",
                table: "Participations");

            migrationBuilder.CreateIndex(
                name: "IX_Participations_TeamId_GameId",
                table: "Participations",
                columns: new[] { "TeamId", "GameId" });
        }
    }
}
