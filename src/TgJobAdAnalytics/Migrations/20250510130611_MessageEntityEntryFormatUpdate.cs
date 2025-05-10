using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgJobAdAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class MessageEntityEntryFormatUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RawText",
                table: "Messages",
                newName: "TextEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TextEntries",
                table: "Messages",
                newName: "RawText");
        }
    }
}
