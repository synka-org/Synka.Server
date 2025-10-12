using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synka.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFileMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    WindowsFileId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UnixFileId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileMetadata_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_ContentHash",
                table: "FileMetadata",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_UserId",
                table: "FileMetadata",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_WindowsFileId_UnixFileId",
                table: "FileMetadata",
                columns: new[] { "WindowsFileId", "UnixFileId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileMetadata");
        }
    }
}
