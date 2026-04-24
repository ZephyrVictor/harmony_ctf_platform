using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFlagContexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add new columns to ChallengeFlags
            migrationBuilder.AddColumn<int?>(
                name: "AttachmentId",
                table: "ChallengeFlags",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "ExerciseId",
                table: "ChallengeFlags",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOccupied",
                table: "ChallengeFlags",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Step 2: Migrate data from FlagContexts to ChallengeFlags
            // Add temp column to store old FlagContext ID for FK remapping
            migrationBuilder.AddColumn<int>(
                name: "_OldFlagContextId",
                table: "ChallengeFlags",
                type: "integer",
                nullable: true);

            // Insert FlagContext rows that don't have a matching ChallengeFlag
            migrationBuilder.Sql(@"
                INSERT INTO ""ChallengeFlags"" (""Id"", ""FlagValue"", ""Score"", ""SortOrder"", ""IsOccupied"", ""AttachmentId"", ""ExerciseId"", ""ChallengeId"", ""CreatedAt"", ""UpdatedAt"", ""_OldFlagContextId"")
                SELECT gen_random_uuid(), fc.""Flag"", 100, 0, fc.""IsOccupied"", fc.""AttachmentId"", fc.""ExerciseId"", fc.""ChallengeId"", NOW(), NOW(), fc.""Id""
                FROM ""FlagContexts"" fc
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""ChallengeFlags"" cf
                    WHERE cf.""FlagValue"" = fc.""Flag"" AND cf.""ChallengeId"" = fc.""ChallengeId""
                );
            ");

            // Update existing ChallengeFlags with FlagContext data
            migrationBuilder.Sql(@"
                UPDATE ""ChallengeFlags"" cf
                SET ""IsOccupied"" = fc.""IsOccupied"",
                    ""AttachmentId"" = COALESCE(cf.""AttachmentId"", fc.""AttachmentId""),
                    ""ExerciseId"" = COALESCE(cf.""ExerciseId"", fc.""ExerciseId""),
                    ""_OldFlagContextId"" = fc.""Id""
                FROM ""FlagContexts"" fc
                WHERE cf.""FlagValue"" = fc.""Flag""
                  AND cf.""ChallengeId"" = fc.""ChallengeId""
                  AND cf.""_OldFlagContextId"" IS NULL;
            ");

            // Step 3: Drop old FKs and indices on Instance tables
            migrationBuilder.DropForeignKey(
                name: "FK_GameInstances_FlagContexts_FlagId",
                table: "GameInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseInstances_FlagContexts_FlagId",
                table: "ExerciseInstances");

            migrationBuilder.DropIndex(
                name: "IX_GameInstances_FlagId",
                table: "GameInstances");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseInstances_FlagId",
                table: "ExerciseInstances");

            // Step 4: Remap Instance FlagId from int to Guid using temp column
            // Add new uuid column, populate from mapping, drop old column, rename
            migrationBuilder.AddColumn<Guid>(
                name: "_NewFlagId",
                table: "GameInstances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "_NewFlagId",
                table: "ExerciseInstances",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""GameInstances"" gi
                SET ""_NewFlagId"" = cf.""Id""
                FROM ""ChallengeFlags"" cf
                WHERE cf.""_OldFlagContextId"" = gi.""FlagId"";
            ");

            migrationBuilder.Sql(@"
                UPDATE ""ExerciseInstances"" ei
                SET ""_NewFlagId"" = cf.""Id""
                FROM ""ChallengeFlags"" cf
                WHERE cf.""_OldFlagContextId"" = ei.""FlagId"";
            ");

            migrationBuilder.DropColumn(
                name: "FlagId",
                table: "GameInstances");

            migrationBuilder.DropColumn(
                name: "FlagId",
                table: "ExerciseInstances");

            migrationBuilder.RenameColumn(
                name: "_NewFlagId",
                table: "GameInstances",
                newName: "FlagId");

            migrationBuilder.RenameColumn(
                name: "_NewFlagId",
                table: "ExerciseInstances",
                newName: "FlagId");

            // Step 5: Drop FlagContexts table
            // Drop FKs from FlagContexts first
            migrationBuilder.Sql(@"
                ALTER TABLE ""FlagContexts"" DROP CONSTRAINT IF EXISTS ""FK_FlagContexts_Attachments_AttachmentId"";
                ALTER TABLE ""FlagContexts"" DROP CONSTRAINT IF EXISTS ""FK_FlagContexts_ExerciseChallenges_ExerciseId"";
                ALTER TABLE ""FlagContexts"" DROP CONSTRAINT IF EXISTS ""FK_FlagContexts_GameChallenges_ChallengeId"";
            ");

            migrationBuilder.DropTable(
                name: "FlagContexts");

            // Step 6: Drop temp column from ChallengeFlags
            migrationBuilder.DropColumn(
                name: "_OldFlagContextId",
                table: "ChallengeFlags");

            // Step 7: Create new indices and FKs
            migrationBuilder.CreateIndex(
                name: "IX_ChallengeFlags_AttachmentId",
                table: "ChallengeFlags",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeFlags_ExerciseId",
                table: "ChallengeFlags",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_GameInstances_FlagId",
                table: "GameInstances",
                column: "FlagId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseInstances_FlagId",
                table: "ExerciseInstances",
                column: "FlagId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChallengeFlags_Attachments_AttachmentId",
                table: "ChallengeFlags",
                column: "AttachmentId",
                principalTable: "Attachments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ChallengeFlags_ExerciseChallenges_ExerciseId",
                table: "ChallengeFlags",
                column: "ExerciseId",
                principalTable: "ExerciseChallenges",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameInstances_ChallengeFlags_FlagId",
                table: "GameInstances",
                column: "FlagId",
                principalTable: "ChallengeFlags",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseInstances_ChallengeFlags_FlagId",
                table: "ExerciseInstances",
                column: "FlagId",
                principalTable: "ChallengeFlags",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: Drop new FKs and indices
            migrationBuilder.DropForeignKey(
                name: "FK_GameInstances_ChallengeFlags_FlagId",
                table: "GameInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseInstances_ChallengeFlags_FlagId",
                table: "ExerciseInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_ChallengeFlags_Attachments_AttachmentId",
                table: "ChallengeFlags");

            migrationBuilder.DropForeignKey(
                name: "FK_ChallengeFlags_ExerciseChallenges_ExerciseId",
                table: "ChallengeFlags");

            migrationBuilder.DropIndex(
                name: "IX_ChallengeFlags_AttachmentId",
                table: "ChallengeFlags");

            migrationBuilder.DropIndex(
                name: "IX_ChallengeFlags_ExerciseId",
                table: "ChallengeFlags");

            migrationBuilder.DropIndex(
                name: "IX_GameInstances_FlagId",
                table: "GameInstances");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseInstances_FlagId",
                table: "ExerciseInstances");

            // Recreate FlagContexts table
            migrationBuilder.CreateTable(
                name: "FlagContexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttachmentId = table.Column<int>(type: "integer", nullable: true),
                    ChallengeId = table.Column<int>(type: "integer", nullable: true),
                    ExerciseId = table.Column<int>(type: "integer", nullable: true),
                    Flag = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false),
                    IsOccupied = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlagContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlagContexts_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlagContexts_ExerciseChallenges_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "ExerciseChallenges",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FlagContexts_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlagContexts_AttachmentId",
                table: "FlagContexts",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FlagContexts_ChallengeId",
                table: "FlagContexts",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_FlagContexts_ExerciseId",
                table: "FlagContexts",
                column: "ExerciseId");

            // Migrate data back from ChallengeFlags to FlagContexts
            migrationBuilder.Sql(@"
                INSERT INTO ""FlagContexts"" (""Flag"", ""IsOccupied"", ""AttachmentId"", ""ExerciseId"", ""ChallengeId"")
                SELECT cf.""FlagValue"", cf.""IsOccupied"", cf.""AttachmentId"", cf.""ExerciseId"", cf.""ChallengeId""
                FROM ""ChallengeFlags"" cf;
            ");

            // Revert Instance FlagId from Guid back to int
            // Add temp int column for old ID mapping
            migrationBuilder.AddColumn<int>(
                name: "_OldFlagId",
                table: "GameInstances",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "_OldFlagId",
                table: "ExerciseInstances",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""GameInstances"" gi
                SET ""_OldFlagId"" = fc.""Id""
                FROM ""ChallengeFlags"" cf
                JOIN ""FlagContexts"" fc ON fc.""Flag"" = cf.""FlagValue"" AND fc.""ChallengeId"" = cf.""ChallengeId""
                WHERE cf.""Id"" = gi.""FlagId"";
            ");

            migrationBuilder.Sql(@"
                UPDATE ""ExerciseInstances"" ei
                SET ""_OldFlagId"" = fc.""Id""
                FROM ""ChallengeFlags"" cf
                JOIN ""FlagContexts"" fc ON fc.""Flag"" = cf.""FlagValue"" AND fc.""ChallengeId"" = cf.""ChallengeId""
                WHERE cf.""Id"" = ei.""FlagId"";
            ");

            migrationBuilder.DropColumn(
                name: "FlagId",
                table: "GameInstances");

            migrationBuilder.DropColumn(
                name: "FlagId",
                table: "ExerciseInstances");

            migrationBuilder.RenameColumn(
                name: "_OldFlagId",
                table: "GameInstances",
                newName: "FlagId");

            migrationBuilder.RenameColumn(
                name: "_OldFlagId",
                table: "ExerciseInstances",
                newName: "FlagId");

            // Recreate indices and FKs for FlagContexts
            migrationBuilder.CreateIndex(
                name: "IX_GameInstances_FlagId",
                table: "GameInstances",
                column: "FlagId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseInstances_FlagId",
                table: "ExerciseInstances",
                column: "FlagId");

            migrationBuilder.AddForeignKey(
                name: "FK_GameInstances_FlagContexts_FlagId",
                table: "GameInstances",
                column: "FlagId",
                principalTable: "FlagContexts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseInstances_FlagContexts_FlagId",
                table: "ExerciseInstances",
                column: "FlagId",
                principalTable: "FlagContexts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Drop columns from ChallengeFlags
            migrationBuilder.DropColumn(
                name: "AttachmentId",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                table: "ChallengeFlags");

            migrationBuilder.DropColumn(
                name: "IsOccupied",
                table: "ChallengeFlags");
        }
    }
}
