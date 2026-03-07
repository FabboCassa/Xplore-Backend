using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xplore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionsAndVisitedPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Competitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitedPlaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PlaceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VisitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitedPlaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompetitionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: false),
                    TargetPlaceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitionRules_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionRules_CompetitionId",
                table: "CompetitionRules",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_GroupId",
                table: "Competitions",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitedPlaces_UserId",
                table: "VisitedPlaces",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitedPlaces_UserId_PlaceId",
                table: "VisitedPlaces",
                columns: new[] { "UserId", "PlaceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetitionRules");

            migrationBuilder.DropTable(
                name: "VisitedPlaces");

            migrationBuilder.DropTable(
                name: "Competitions");
        }
    }
}
