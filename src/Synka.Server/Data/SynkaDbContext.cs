using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Synka.Server.Data.Configurations;
using Synka.Server.Data.Entities;

namespace Synka.Server.Data;

public class SynkaDbContext(
    DbContextOptions<SynkaDbContext> options,
    TimeProvider timeProvider)
    : IdentityDbContext<ApplicationUserEntity, IdentityRole<Guid>, Guid>(options)
{
    private readonly TimeProvider _timeProvider = timeProvider;

    public DbSet<FolderEntity> Folders => Set<FolderEntity>();
    public DbSet<FolderAccessEntity> FolderAccess => Set<FolderAccessEntity>();
    public DbSet<FileMetadataEntity> FileMetadata => Set<FileMetadataEntity>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new FolderEntityConfiguration())
            .ApplyConfiguration(new FolderAccessEntityConfiguration())
            .ApplyConfiguration(new FileMetadataEntityConfiguration());
    }

    private void ApplyTimestamps()
    {
        var now = _timeProvider.GetUtcNow();

        foreach (var entry in ChangeTracker.Entries<IHasCreatedAt>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IHasUpdatedAt>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<FileMetadataEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.UploadedAt == default)
            {
                entry.Entity.UploadedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<FolderAccessEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.GrantedAt == default)
            {
                entry.Entity.GrantedAt = now;
            }
        }

    }
}
