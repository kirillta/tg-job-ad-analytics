using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgJobAdAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class AddStackClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsUnique = table.Column<bool>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    MessageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StackId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ads", x => x.Id);
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
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdVectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Chats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TelegramId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LshBuckets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Band = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    AdId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LshBuckets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TelegramChatId = table.Column<long>(type: "INTEGER", nullable: false),
                    TelegramMessageId = table.Column<long>(type: "INTEGER", nullable: false),
                    TelegramMessageDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TextEntries = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Salaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AdId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Currency = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrencyNormalized = table.Column<int>(type: "INTEGER", nullable: false),
                    LowerBound = table.Column<double>(type: "REAL", nullable: true),
                    LowerBoundNormalized = table.Column<double>(type: "REAL", nullable: false),
                    UpperBound = table.Column<double>(type: "REAL", nullable: true),
                    UpperBoundNormalized = table.Column<double>(type: "REAL", nullable: false),
                    Period = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salaries", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "VectorModelVersions",
                columns: table => new
                {
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NormalizationVersion = table.Column<string>(type: "TEXT", nullable: false),
                    ShingleSize = table.Column<int>(type: "INTEGER", nullable: false),
                    HashFunctionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MinHashSeed = table.Column<int>(type: "INTEGER", nullable: false),
                    LshBandCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LshRowsPerBand = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularySize = table.Column<int>(type: "INTEGER", nullable: false),
                    DuplicateThreshold = table.Column<double>(type: "REAL", nullable: false),
                    SimilarThreshold = table.Column<double>(type: "REAL", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VectorModelVersions", x => x.Version);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ads_IsUnique",
                table: "Ads",
                column: "IsUnique");

            migrationBuilder.CreateIndex(
                name: "IX_Ads_MessageId",
                table: "Ads",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Ads_StackId",
                table: "Ads",
                column: "StackId");

            migrationBuilder.CreateIndex(
                name: "IX_AdVectors_AdId_Version",
                table: "AdVectors",
                columns: new[] { "AdId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chats_TelegramId",
                table: "Chats",
                column: "TelegramId");

            migrationBuilder.CreateIndex(
                name: "IX_LshBuckets_AdId",
                table: "LshBuckets",
                column: "AdId");

            migrationBuilder.CreateIndex(
                name: "IX_LshBuckets_Version_Band_Key",
                table: "LshBuckets",
                columns: new[] { "Version", "Band", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_TelegramChatId_TelegramMessageId",
                table: "Messages",
                columns: new[] { "TelegramChatId", "TelegramMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Salaries_AdId",
                table: "Salaries",
                column: "AdId");

            migrationBuilder.CreateIndex(
                name: "IX_Salaries_Level",
                table: "Salaries",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyStacks_Name",
                table: "TechnologyStacks",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VectorModelVersions_IsActive",
                table: "VectorModelVersions",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ads");

            migrationBuilder.DropTable(
                name: "AdVectors");

            migrationBuilder.DropTable(
                name: "Chats");

            migrationBuilder.DropTable(
                name: "LshBuckets");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Salaries");

            migrationBuilder.DropTable(
                name: "TechnologyStacks");

            migrationBuilder.DropTable(
                name: "VectorModelVersions");
        }
    }
}
