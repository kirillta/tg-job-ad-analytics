using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgJobAdAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationAndWorkFormatToAd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Location",
                table: "Ads",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkFormat",
                table: "Ads",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Ads_Location",
                table: "Ads",
                column: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_Ads_WorkFormat",
                table: "Ads",
                column: "WorkFormat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ads_Location",
                table: "Ads");

            migrationBuilder.DropIndex(
                name: "IX_Ads_WorkFormat",
                table: "Ads");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Ads");

            migrationBuilder.DropColumn(
                name: "WorkFormat",
                table: "Ads");
        }
    }
}
