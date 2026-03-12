using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xplore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreAvatarInDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvatarUrl",
                table: "AspNetUsers",
                newName: "AvatarContentType");

            migrationBuilder.AddColumn<byte[]>(
                name: "AvatarData",
                table: "AspNetUsers",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarData",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "AvatarContentType",
                table: "AspNetUsers",
                newName: "AvatarUrl");
        }
    }
}
