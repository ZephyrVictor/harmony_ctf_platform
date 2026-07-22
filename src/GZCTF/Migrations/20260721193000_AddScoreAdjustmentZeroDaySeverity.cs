using GZCTF.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260721193000_AddScoreAdjustmentZeroDaySeverity")]
    public partial class AddScoreAdjustmentZeroDaySeverity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Games"
                SET "ZeroDayBaseScore" = 1
                WHERE "ZeroDayBaseScore" < 1;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ZeroDayBaseScore",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ZeroDaySeverity",
                table: "ScoreAdjustments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "ScoreAdjustments"
                SET "ZeroDaySeverity" = CASE
                    WHEN lower(coalesce("RelatedInfo", '') || ' ' || coalesce("Reason", '')) LIKE '%critical%' THEN 'Critical'
                    WHEN lower(coalesce("RelatedInfo", '') || ' ' || coalesce("Reason", '')) LIKE '%high%' THEN 'High'
                    WHEN lower(coalesce("RelatedInfo", '') || ' ' || coalesce("Reason", '')) LIKE '%medium%' THEN 'Medium'
                    WHEN lower(coalesce("RelatedInfo", '') || ' ' || coalesce("Reason", '')) LIKE '%low%' THEN 'Low'
                    ELSE NULL
                END
                WHERE "Category" = 'ZeroDayBonus'
                  AND "ZeroDaySeverity" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZeroDaySeverity",
                table: "ScoreAdjustments");

            migrationBuilder.AlterColumn<int>(
                name: "ZeroDayBaseScore",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);
        }
    }
}
