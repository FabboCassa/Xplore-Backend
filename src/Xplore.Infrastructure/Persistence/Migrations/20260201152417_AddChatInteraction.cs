using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xplore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatInteraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatInteractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MuseumId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserQuestion = table.Column<string>(type: "text", nullable: false),
                    AiResponse = table.Column<string>(type: "text", nullable: false),
                    RetrievedContext = table.Column<string>(type: "text", nullable: true),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    UserFeedback = table.Column<bool>(type: "boolean", nullable: true),
                    Sentiment = table.Column<float>(type: "real", nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    TokensUsed = table.Column<int>(type: "integer", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatInteractions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatInteractions_CreatedAt",
                table: "ChatInteractions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatInteractions_MuseumId",
                table: "ChatInteractions",
                column: "MuseumId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatInteractions_SessionId",
                table: "ChatInteractions",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatInteractions");
        }
    }
}
