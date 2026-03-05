using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xplore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupPrivacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessType",
                table: "Groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Groups",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_AccessType",
                table: "Groups",
                column: "AccessType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Groups_AccessType",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "AccessType",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Groups");
        }
    }
}
