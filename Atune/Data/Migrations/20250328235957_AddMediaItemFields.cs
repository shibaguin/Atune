using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atune.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaItemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverArt",
                table: "MediaItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "MediaItems",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleaseDate",
                table: "MediaItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_Artist_Album",
                table: "MediaItems",
                columns: new[] { "Artist", "Album" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaItems_Artist_Album",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "CoverArt",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "ReleaseDate",
                table: "MediaItems");
        }
    }
}
