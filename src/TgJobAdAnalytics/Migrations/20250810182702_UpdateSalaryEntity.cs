using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgJobAdAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSalaryEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OriginalCurrency",
                table: "Salaries",
                newName: "Status");

            migrationBuilder.AddColumn<int>(
                name: "CurrencyNormalized",
                table: "Salaries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyNormalized",
                table: "Salaries");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Salaries",
                newName: "OriginalCurrency");
        }
    }
}
