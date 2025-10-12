using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synka.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateFileIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileMetadata_WindowsFileId_UnixFileId",
                table: "FileMetadata");

            // Migrate data: Use WindowsFileId if available, otherwise UnixFileId
            migrationBuilder.Sql(
                "UPDATE FileMetadata SET WindowsFileId = COALESCE(WindowsFileId, UnixFileId)");

            migrationBuilder.DropColumn(
                name: "UnixFileId",
                table: "FileMetadata");

            migrationBuilder.RenameColumn(
                name: "WindowsFileId",
                table: "FileMetadata",
                newName: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_FileId",
                table: "FileMetadata",
                column: "FileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileMetadata_FileId",
                table: "FileMetadata");

            migrationBuilder.RenameColumn(
                name: "FileId",
                table: "FileMetadata",
                newName: "WindowsFileId");

            migrationBuilder.AddColumn<string>(
                name: "UnixFileId",
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
