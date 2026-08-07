using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clausio.Legal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_MemoryAndContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaseMemories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseTitle = table.Column<string>(type: "text", nullable: false),
                    CaseType = table.Column<string>(type: "text", nullable: false),
                    ShortSummary = table.Column<string>(type: "text", nullable: false),
                    CurrentStatus = table.Column<string>(type: "text", nullable: false),
                    KeyFacts = table.Column<string>(type: "text", nullable: false),
                    ImportantDates = table.Column<string>(type: "text", nullable: false),
                    Parties = table.Column<string>(type: "text", nullable: false),
                    LegalIssues = table.Column<string>(type: "text", nullable: false),
                    CurrentObjective = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseMemories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConversationMemories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationSummary = table.Column<string>(type: "text", nullable: false),
                    ImportantDecisions = table.Column<string>(type: "text", nullable: false),
                    PreviousAiSuggestions = table.Column<string>(type: "text", nullable: false),
                    PendingTasks = table.Column<string>(type: "text", nullable: false),
                    MessageCountSinceLastSummary = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMemories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DraftMemories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftType = table.Column<string>(type: "text", nullable: false),
                    DraftVersion = table.Column<string>(type: "text", nullable: false),
                    DraftStatus = table.Column<string>(type: "text", nullable: false),
                    LastDraftContent = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DraftMemories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "text", nullable: false),
                    WritingStyle = table.Column<string>(type: "text", nullable: false),
                    CitationStyle = table.Column<string>(type: "text", nullable: false),
                    PreferredJurisdiction = table.Column<string>(type: "text", nullable: false),
                    DraftFormat = table.Column<string>(type: "text", nullable: false),
                    SignatureFormat = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseMemories");

            migrationBuilder.DropTable(
                name: "ConversationMemories");

            migrationBuilder.DropTable(
                name: "DraftMemories");

            migrationBuilder.DropTable(
                name: "UserPreferences");
        }
    }
}
