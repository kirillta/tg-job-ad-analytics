using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgJobAdAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueAdIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsUnique",
                table: "Ads",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsUnique",
                table: "Ads");
        }
    }
}
