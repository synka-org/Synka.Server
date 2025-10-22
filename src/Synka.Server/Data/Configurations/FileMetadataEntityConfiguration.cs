using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synka.Server.Data.Entities;

namespace Synka.Server.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="FileMetadataEntity"/>.
/// </summary>
internal sealed class FileMetadataEntityConfiguration : IEntityTypeConfiguration<FileMetadataEntity>
{
    public void Configure(EntityTypeBuilder<FileMetadataEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FileName).IsRequired().HasMaxLength(512);
        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(256);
        builder.Property(e => e.StoragePath).IsRequired().HasMaxLength(1024);
        builder.Property(e => e.ContentHash).HasMaxLength(64); // SHA-256 hex = 64 chars

        builder.HasIndex(e => e.UploadedById);
        builder.HasIndex(e => e.FolderId);
        builder.HasIndex(e => e.ContentHash);

        builder.HasOne(e => e.UploadedBy)
            .WithMany()
            .HasForeignKey(e => e.UploadedById)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Folder)
            .WithMany(f => f.Files)
            .HasForeignKey(e => e.FolderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
