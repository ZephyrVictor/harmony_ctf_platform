using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseFlagValueLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Answer",
                table: "Submissions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(127)",
                oldMaxLength: 127);

            migrationBuilder.AlterColumn<string>(
                name: "BanAnswer",
                table: "ChallengeTrackLevelBanStates",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(127)",
                oldMaxLength: 127);

            migrationBuilder.AlterColumn<string>(
                name: "TriggerAnswer",
                table: "ChallengeLockStates",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(127)",
                oldMaxLength: 127,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RawAnswer",
                table: "ChallengeFlags",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(127)",
                oldMaxLength: 127,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedAnswerKey",
                table: "ChallengeFlags",
                type: "character varying(528)",
                maxLength: 528,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(143)",
                oldMaxLength: 143,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "FlagValue",
                table: "ChallengeFlags",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(127)",
                oldMaxLength: 127);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Answer",
                table: "Submissions",
                type: "character varying(127)",
                maxLength: 127,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "BanAnswer",
                table: "ChallengeTrackLevelBanStates",
                type: "character varying(127)",
                maxLength: 127,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "TriggerAnswer",
                table: "ChallengeLockStates",
                type: "character varying(127)",
                maxLength: 127,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RawAnswer",
                table: "ChallengeFlags",
                type: "character varying(127)",
                maxLength: 127,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedAnswerKey",
                table: "ChallengeFlags",
                type: "character varying(143)",
                maxLength: 143,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(528)",
                oldMaxLength: 528,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "FlagValue",
                table: "ChallengeFlags",
                type: "character varying(127)",
                maxLength: 127,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);
        }
    }
}
