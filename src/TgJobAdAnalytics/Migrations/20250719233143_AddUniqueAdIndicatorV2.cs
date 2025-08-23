using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgJobAdAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueAdIndicatorV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Ads_IsUnique",
                table: "Ads",
                column: "IsUnique");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ads_IsUnique",
                table: "Ads");
        }
    }
}
