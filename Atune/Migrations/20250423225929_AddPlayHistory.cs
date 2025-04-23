using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atune.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlaybackQueueItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybackQueueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaybackQueueItems_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PercentPlayed = table.Column<double>(type: "REAL", nullable: false),
                    AppVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OS = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayHistories_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaybackQueueItems_MediaItemId",
                table: "PlaybackQueueItems",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayHistories_MediaItem_PlayedAt",
                table: "PlayHistories",
                columns: new[] { "MediaItemId", "PlayedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaybackQueueItems");

            migrationBuilder.DropTable(
                name: "PlayHistories");
        }
    }
}
