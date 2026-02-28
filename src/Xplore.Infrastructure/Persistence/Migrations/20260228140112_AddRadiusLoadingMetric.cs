using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xplore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRadiusLoadingMetric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RadiusLoadingMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RadiusKm = table.Column<double>(type: "double precision", nullable: false),
                    LoadingTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadiusLoadingMetrics", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RadiusLoadingMetrics_RadiusKm",
                table: "RadiusLoadingMetrics",
                column: "RadiusKm");

            migrationBuilder.CreateIndex(
                name: "IX_RadiusLoadingMetrics_RecordedAt",
                table: "RadiusLoadingMetrics",
                column: "RecordedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RadiusLoadingMetrics");
        }
    }
}
