using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synka.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWindowsAndUnixFileIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileMetadata_WindowsFileId_UnixFileId",
                table: "FileMetadata");

            migrationBuilder.DropColumn(
                name: "UnixFileId",
                table: "FileMetadata");

            migrationBuilder.DropColumn(
                name: "WindowsFileId",
                table: "FileMetadata");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnixFileId",
                table: "FileMetadata",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WindowsFileId",
                table: "FileMetadata",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_WindowsFileId_UnixFileId",
                table: "FileMetadata",
                columns: new[] { "WindowsFileId", "UnixFileId" });
        }
    }
}
