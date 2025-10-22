using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synka.Server.Data.Entities;

namespace Synka.Server.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="FolderEntity"/>.
/// </summary>
internal sealed class FolderEntityConfiguration : IEntityTypeConfiguration<FolderEntity>
{
    public void Configure(EntityTypeBuilder<FolderEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.PhysicalPath)
            .IsRequired()
            .HasMaxLength(2048);

        builder.HasIndex(e => e.OwnerId);
        builder.HasIndex(e => e.ParentFolderId);

        builder.HasIndex(e => e.PhysicalPath)
            .IsUnique();

        builder.HasOne(e => e.Owner)
            .WithMany()
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ParentFolder)
            .WithMany(p => p.ChildFolders)
            .HasForeignKey(e => e.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
