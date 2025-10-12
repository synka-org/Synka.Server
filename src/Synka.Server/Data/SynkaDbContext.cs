using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Synka.Server.Data.Entities;

namespace Synka.Server.Data;

public class SynkaDbContext(DbContextOptions<SynkaDbContext> options)
    : IdentityDbContext<ApplicationUserEntity, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<FolderEntity> Folders => Set<FolderEntity>();
    public DbSet<FolderAccessEntity> FolderAccess => Set<FolderAccessEntity>();
    public DbSet<FileMetadataEntity> FileMetadata => Set<FileMetadataEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

#pragma warning disable RCS1201 // Use method chaining - not applicable for EF Core entity configuration
        builder.Entity<FolderEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.PhysicalPath)
                .IsRequired()
                .HasMaxLength(2048);

            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.ParentFolderId);

            entity.HasIndex(e => e.PhysicalPath)
                .IsUnique();

            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentFolder)
                .WithMany(p => p.ChildFolders)
                .HasForeignKey(e => e.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FolderAccessEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.FolderId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.Folder)
                .WithMany(f => f.SharedWith)
                .HasForeignKey(e => e.FolderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.GrantedBy)
                .WithMany()
                .HasForeignKey(e => e.GrantedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FileMetadataEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(512);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(256);
            entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.ContentHash).HasMaxLength(64); // SHA-256 hex = 64 chars

            entity.HasIndex(e => e.UploadedById);
            entity.HasIndex(e => e.FolderId);
            entity.HasIndex(e => e.ContentHash);

            entity.HasOne(e => e.UploadedBy)
                .WithMany()
                .HasForeignKey(e => e.UploadedById)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Folder)
                .WithMany(f => f.Files)
                .HasForeignKey(e => e.FolderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
#pragma warning restore RCS1201
    }
}
