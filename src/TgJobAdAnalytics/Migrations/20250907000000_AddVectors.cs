using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgJobAdAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class AddVectors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VectorModelVersions",
                columns: table => new
                {
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    NormalizationVersion = table.Column<string>(type: "TEXT", nullable: false),
                    ShingleSize = table.Column<int>(type: "INTEGER", nullable: false),
                    HashFunctionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MinHashSeed = table.Column<int>(type: "INTEGER", nullable: false),
                    LshBandCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LshRowsPerBand = table.Column<int>(type: "INTEGER", nullable: false),
                    DuplicateThreshold = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.92),
                    SimilarThreshold = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.8),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VectorModelVersions", x => x.Version);
                });

            migrationBuilder.CreateTable(
                name: "AdVectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AdId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Dim = table.Column<int>(type: "INTEGER", nullable: false),
                    Signature = table.Column<byte[]>(type: "BLOB", nullable: false),
                    SignatureHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ShingleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdVectors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdVectors_AdId_Version",
                table: "AdVectors",
                columns: new[] { "AdId", "Version" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "LshBuckets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Band = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    AdId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LshBuckets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LshBuckets_Version_Band_Key",
                table: "LshBuckets",
                columns: new[] { "Version", "Band", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_LshBuckets_AdId",
                table: "LshBuckets",
                column: "AdId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdVectors");

            migrationBuilder.DropTable(
                name: "LshBuckets");

            migrationBuilder.DropTable(
                name: "VectorModelVersions");
        }
    }
}
