using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgJobAdAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class StackClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StackId",
                table: "Ads",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TechnologyStacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyStacks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ads_StackId",
                table: "Ads",
                column: "StackId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyStacks_Name",
                table: "TechnologyStacks",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TechnologyStacks");

            migrationBuilder.DropIndex(
                name: "IX_Ads_StackId",
                table: "Ads");

            migrationBuilder.DropColumn(
                name: "StackId",
                table: "Ads");
        }
    }
}
