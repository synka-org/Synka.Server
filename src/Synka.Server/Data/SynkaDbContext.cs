using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Synka.Server.Data.Entities;

namespace Synka.Server.Data;

public class SynkaDbContext(DbContextOptions<SynkaDbContext> options)
    : IdentityDbContext<ApplicationUserEntity, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<FileMetadataEntity> FileMetadata => Set<FileMetadataEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        builder.Entity<FileMetadataEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(512);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(256);
            entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.WindowsFileId).HasMaxLength(100);
            entity.Property(e => e.UnixFileId).HasMaxLength(100);
            entity.Property(e => e.ContentHash).HasMaxLength(64); // SHA-256 hex = 64 chars

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ContentHash);
            entity.HasIndex(e => new { e.WindowsFileId, e.UnixFileId });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
