using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atune.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Artist = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Album = table.Column<string>(type: "TEXT", nullable: false),
                    Year = table.Column<uint>(type: "INTEGER", nullable: false),
                    Genre = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<long>(type: "BIGINT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_Artist",
                table: "MediaItems",
                column: "Artist");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_Artist_Title",
                table: "MediaItems",
                columns: new[] { "Artist", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_Genre",
                table: "MediaItems",
                column: "Genre");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_Path",
                table: "MediaItems",
                column: "Path",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaItems");
        }
    }
}
