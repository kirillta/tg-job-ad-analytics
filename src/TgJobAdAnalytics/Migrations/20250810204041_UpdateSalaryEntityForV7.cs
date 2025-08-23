using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgJobAdAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSalaryEntityForV7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Period",
                table: "Salaries",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Period",
                table: "Salaries");
        }
    }
}
