using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synka.Server.Data.Entities;

namespace Synka.Server.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="FolderAccessEntity"/>.
/// </summary>
internal sealed class FolderAccessEntityConfiguration : IEntityTypeConfiguration<FolderAccessEntity>
{
    public void Configure(EntityTypeBuilder<FolderAccessEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.FolderId, e.UserId }).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.GrantedById);

        builder.HasOne(e => e.Folder)
            .WithMany(f => f.SharedWith)
            .HasForeignKey(e => e.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.GrantedBy)
            .WithMany()
            .HasForeignKey(e => e.GrantedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
