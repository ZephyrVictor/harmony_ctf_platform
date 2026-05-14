using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddFlagMatchMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddCheckConstraint(
                name: "CK_Ticket_Approved_Scores",
                table: "Tickets",
                sql: "\"Status\" <> 'Approved' OR (\"RefundScore\" >= 0 AND \"BonusScore\" >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Ticket_Pending_Nulls",
                table: "Tickets",
                sql: "\"Status\" <> 'Pending' OR (\"ReviewerId\" IS NULL AND \"ReviewComment\" IS NULL AND \"RefundScore\" = 0 AND \"BonusScore\" = 0 AND \"ReviewedAt\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Ticket_Rejected_Comment",
                table: "Tickets",
                sql: "\"Status\" <> 'Rejected' OR \"ReviewComment\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Ticket_Resolved_Reviewer",
                table: "Tickets",
                sql: "\"Status\" IN ('Pending') OR (\"ReviewerId\" IS NOT NULL AND \"ReviewedAt\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubmissionRecord_NonPenalty_RequiresFlag",
                table: "SubmissionRecords",
                sql: "\"IsPenalty\" = true OR (\"ScoreChange\" >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubmissionRecord_Penalty_NullFlag",
                table: "SubmissionRecords",
                sql: "\"IsPenalty\" = false OR \"MatchedFlagId\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Ticket_Approved_Scores",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Ticket_Pending_Nulls",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Ticket_Rejected_Comment",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Ticket_Resolved_Reviewer",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubmissionRecord_NonPenalty_RequiresFlag",
                table: "SubmissionRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubmissionRecord_Penalty_NullFlag",
                table: "SubmissionRecords");

            migrationBuilder.DropColumn(
                name: "FlagMatchMode",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "FlagMatchMode",
                table: "ExerciseChallenges");
        }
    }
}
