using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class ExpandZeroDayTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CWE",
                table: "ZeroDayTickets",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommitHash",
                table: "ZeroDayTickets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedTrackId",
                table: "ZeroDayTickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "ZeroDayTickets",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LineNumber",
                table: "ZeroDayTickets",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Module",
                table: "ZeroDayTickets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoC",
                table: "ZeroDayTickets",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Severity",
                table: "ZeroDayTickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CWE",
                table: "ZeroDayTickets");

            migrationBuilder.DropColumn(
                name: "CommitHash",
                table: "ZeroDayTickets");

            migrationBuilder.DropColumn(
                name: "CreatedTrackId",
                table: "ZeroDayTickets");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "ZeroDayTickets");

            migrationBuilder.DropColumn(
                name: "LineNumber",
                table: "ZeroDayTickets");

            migrationBuilder.DropColumn(
                name: "Module",
                table: "ZeroDayTickets");

            migrationBuilder.DropColumn(
                name: "PoC",
                table: "ZeroDayTickets");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "ZeroDayTickets");
        }
    }
}
