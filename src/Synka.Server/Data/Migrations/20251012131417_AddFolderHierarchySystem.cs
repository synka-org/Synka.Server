using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synka.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderHierarchySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileMetadata_AspNetUsers_UserId",
                table: "FileMetadata");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "FileMetadata",
                newName: "UploadedById");

            migrationBuilder.RenameIndex(
                name: "IX_FileMetadata_UserId",
                table: "FileMetadata",
                newName: "IX_FileMetadata_UploadedById");

            migrationBuilder.AddColumn<Guid>(
                name: "FolderId",
                table: "FileMetadata",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FileMetadata",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ParentFolderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PhysicalPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Folders_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Folders_Folders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FolderAccess",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FolderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GrantedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    Permission = table.Column<int>(type: "INTEGER", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FolderAccess_AspNetUsers_GrantedById",
                        column: x => x.GrantedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FolderAccess_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FolderAccess_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_FolderId",
                table: "FileMetadata",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderAccess_FolderId_UserId",
                table: "FolderAccess",
                columns: new[] { "FolderId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FolderAccess_GrantedById",
                table: "FolderAccess",
                column: "GrantedById");

            migrationBuilder.CreateIndex(
                name: "IX_FolderAccess_UserId",
                table: "FolderAccess",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_OwnerId",
                table: "Folders",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_ParentFolderId",
                table: "Folders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_PhysicalPath",
                table: "Folders",
                column: "PhysicalPath",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FileMetadata_AspNetUsers_UploadedById",
                table: "FileMetadata",
                column: "UploadedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileMetadata_Folders_FolderId",
                table: "FileMetadata",
                column: "FolderId",
                principalTable: "Folders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileMetadata_AspNetUsers_UploadedById",
                table: "FileMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_FileMetadata_Folders_FolderId",
                table: "FileMetadata");

            migrationBuilder.DropTable(
                name: "FolderAccess");

            migrationBuilder.DropTable(
                name: "Folders");

            migrationBuilder.DropIndex(
                name: "IX_FileMetadata_FolderId",
                table: "FileMetadata");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "FileMetadata");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FileMetadata");

            migrationBuilder.RenameColumn(
                name: "UploadedById",
                table: "FileMetadata",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_FileMetadata_UploadedById",
                table: "FileMetadata",
                newName: "IX_FileMetadata_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileMetadata_AspNetUsers_UserId",
                table: "FileMetadata",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
