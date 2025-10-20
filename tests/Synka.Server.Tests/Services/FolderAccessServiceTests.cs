using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Synka.Server.Contracts;
using Synka.Server.Data;
using Synka.Server.Data.Entities;
using Synka.Server.Services;
using Synka.Server.Tests.Infrastructure;

namespace Synka.Server.Tests.Services;

internal sealed class FolderAccessServiceTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly IServiceScope _scope;
    private readonly SynkaDbContext _context;
    private readonly TestCurrentUserAccessor _currentUserAccessor;
    private readonly FolderService _folderService;
    private readonly FolderAccessService _accessService;

    public FolderAccessServiceTests()
    {
        _factory = new TestWebApplicationFactory();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
        var timeProvider = _scope.ServiceProvider.GetRequiredService<TimeProvider>();
        _currentUserAccessor = new TestCurrentUserAccessor();
        _folderService = new FolderService(_context, timeProvider, _currentUserAccessor);
        _accessService = new FolderAccessService(_context, timeProvider);

        // Disable foreign key constraints for testing
        // (Database is already created by TestWebApplicationFactory)
        _context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");
    }

    public void Dispose()
    {
        _context.Dispose();
        _scope.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task HasAccessAsync_Owner_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(userId, "Test", null, "/test");

        // Act
        var hasAccess = await _accessService.HasAccessAsync(userId, folder.Id, FolderAccessLevel.Read);

        // Assert
        await Assert.That(hasAccess).IsTrue();
    }

    [Test]
    public async Task HasAccessAsync_SharedRoot_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(null, "Shared", null, "/shared");

        // Act
        var hasAccess = await _accessService.HasAccessAsync(userId, folder.Id, FolderAccessLevel.Read);

        // Assert
        await Assert.That(hasAccess).IsTrue();
    }

    [Test]
    public async Task HasAccessAsync_WithDirectGrant_ReturnsTrue()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        await _accessService.GrantAccessAsync(userId, folder.Id, ownerId, FolderAccessLevel.Read);

        // Act
        var hasAccess = await _accessService.HasAccessAsync(userId, folder.Id, FolderAccessLevel.Read);

        // Assert
        await Assert.That(hasAccess).IsTrue();
    }

    [Test]
    public async Task HasAccessAsync_WithInheritedAccess_ReturnsTrue()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var parentFolder = await _folderService.CreateFolderAsync(ownerId, "Parent", null, "/parent");
        var childFolder = await _folderService.CreateFolderAsync(ownerId, "Child", parentFolder.Id, null);

        await _accessService.GrantAccessAsync(userId, parentFolder.Id, ownerId, FolderAccessLevel.Admin);

        // Act
        var hasAccess = await _accessService.HasAccessAsync(userId, childFolder.Id, FolderAccessLevel.Read);

        // Assert
        await Assert.That(hasAccess).IsTrue();
    }

    [Test]
    public async Task HasAccessAsync_WithExpiredGrant_ReturnsFalse()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        var access = new FolderAccessEntity
        {
            FolderId = folder.Id,
            UserId = userId,
            GrantedById = ownerId,
            Permission = FolderAccessLevel.Read,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.FolderAccess.Add(access);
        await _context.SaveChangesAsync();

        // Act
        var hasAccess = await _accessService.HasAccessAsync(userId, folder.Id, FolderAccessLevel.Read);

        // Assert
        await Assert.That(hasAccess).IsFalse();
    }

    [Test]
    public async Task HasAccessAsync_WithInsufficientPermission_ReturnsFalse()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        await _accessService.GrantAccessAsync(userId, folder.Id, ownerId, FolderAccessLevel.Read);

        // Act
        var hasAccess = await _accessService.HasAccessAsync(userId, folder.Id, FolderAccessLevel.Admin);

        // Assert
        await Assert.That(hasAccess).IsFalse();
    }

    [Test]
    public async Task GetEffectivePermissionAsync_Owner_ReturnsAdmin()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(userId, "Test", null, "/test");

        // Act
        var permission = await _accessService.GetEffectivePermissionAsync(userId, folder.Id);

        // Assert
        await Assert.That(permission).IsEqualTo(FolderAccessLevel.Admin);
    }

    [Test]
    public async Task GetEffectivePermissionAsync_SharedRoot_ReturnsRead()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(null, "Shared", null, "/shared");

        // Act
        var permission = await _accessService.GetEffectivePermissionAsync(userId, folder.Id);

        // Assert
        await Assert.That(permission).IsEqualTo(FolderAccessLevel.Read);
    }

    [Test]
    public async Task GetEffectivePermissionAsync_WithDirectGrant_ReturnsGrantedPermission()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        await _accessService.GrantAccessAsync(userId, folder.Id, ownerId, FolderAccessLevel.Write);

        // Act
        var permission = await _accessService.GetEffectivePermissionAsync(userId, folder.Id);

        // Assert
        await Assert.That(permission).IsEqualTo(FolderAccessLevel.Write);
    }

    [Test]
    public async Task GetEffectivePermissionAsync_InheritedPermission_ReturnsParentPermission()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var parentFolder = await _folderService.CreateFolderAsync(ownerId, "Parent", null, "/parent");
        var childFolder = await _folderService.CreateFolderAsync(ownerId, "Child", parentFolder.Id, null);

        await _accessService.GrantAccessAsync(userId, parentFolder.Id, ownerId, FolderAccessLevel.Admin);

        // Act
        var permission = await _accessService.GetEffectivePermissionAsync(userId, childFolder.Id);

        // Assert
        await Assert.That(permission).IsEqualTo(FolderAccessLevel.Admin);
    }

    [Test]
    public async Task GetEffectivePermissionAsync_NoAccess_ReturnsNull()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        // Act
        var permission = await _accessService.GetEffectivePermissionAsync(userId, folder.Id);

        // Assert
        await Assert.That(permission).IsNull();
    }

    [Test]
    public async Task CanShareAsync_WithAdminPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(userId, "Test", null, "/test");

        // Act
        var canShare = await _accessService.CanShareAsync(userId, folder.Id);

        // Assert
        await Assert.That(canShare).IsTrue();
    }

    [Test]
    public async Task CanShareAsync_WithWritePermission_ReturnsFalse()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        await _accessService.GrantAccessAsync(userId, folder.Id, ownerId, FolderAccessLevel.Write);

        // Act
        var canShare = await _accessService.CanShareAsync(userId, folder.Id);

        // Assert
        await Assert.That(canShare).IsFalse();
    }

    [Test]
    public async Task GrantAccessAsync_WithValidData_CreatesAccess()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        // Act
        await _accessService.GrantAccessAsync(
            userId,
            folder.Id,
            ownerId,
            FolderAccessLevel.Write,
            DateTime.UtcNow.AddDays(30));

        // Assert
        var access = await _context.FolderAccess
            .FirstOrDefaultAsync(a => a.FolderId == folder.Id && a.UserId == userId);

        await Assert.That(access).IsNotNull();
        await Assert.That(access!.FolderId).IsEqualTo(folder.Id);
        await Assert.That(access.UserId).IsEqualTo(userId);
        await Assert.That(access.GrantedById).IsEqualTo(ownerId);
        await Assert.That(access.Permission).IsEqualTo(FolderAccessLevel.Write);
        await Assert.That(access.ExpiresAt).IsNotNull();
    }

    [Test]
    public async Task GrantAccessAsync_InvalidFolder_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidFolderId = Guid.NewGuid();

        // Act & Assert
        await Assert.That(async () => await _accessService.GrantAccessAsync(
            invalidFolderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            FolderAccessLevel.Read))
            .Throws<InvalidOperationException>()
            .And.HasMessageContaining("not found");
    }

    [Test]
    public async Task GrantAccessAsync_WithoutAdminPermission_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var granterId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        // Give granter only Write permission
        await _accessService.GrantAccessAsync(granterId, folder.Id, ownerId, FolderAccessLevel.Write);

        // Act & Assert
        await Assert.That(async () => await _accessService.GrantAccessAsync(
            userId,
            folder.Id,
            granterId,
            FolderAccessLevel.Read))
            .Throws<UnauthorizedAccessException>()
            .And.HasMessageContaining("does not have permission");
    }

    [Test]
    public async Task UpdateAccessAsync_ChangesPermission()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        await _accessService.GrantAccessAsync(userId, folder.Id, ownerId, FolderAccessLevel.Read);

        // Act
        await _accessService.UpdateAccessAsync(userId, folder.Id, FolderAccessLevel.Admin);

        // Assert
        var updated = await _context.FolderAccess
            .FirstOrDefaultAsync(a => a.FolderId == folder.Id && a.UserId == userId);
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Permission).IsEqualTo(FolderAccessLevel.Admin);
    }

    [Test]
    public async Task UpdateAccessAsync_ChangesExpiration()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        await _accessService.GrantAccessAsync(userId, folder.Id, ownerId, FolderAccessLevel.Read);
        var newExpiration = DateTime.UtcNow.AddDays(60);

        // Act
        await _accessService.UpdateAccessAsync(userId, folder.Id, FolderAccessLevel.Read, newExpiration);

        // Assert
        var updated = await _context.FolderAccess
            .FirstOrDefaultAsync(a => a.FolderId == folder.Id && a.UserId == userId);
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.ExpiresAt).IsNotNull();
        // Check expiration is approximately correct (within 1 second tolerance)
        var diff = Math.Abs((updated.ExpiresAt!.Value - newExpiration).TotalSeconds);
        await Assert.That(diff).IsLessThan(1.0);
    }

    [Test]
    public async Task RevokeAccessAsync_RemovesAccess()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        await _accessService.GrantAccessAsync(userId, folder.Id, ownerId, FolderAccessLevel.Read);

        // Act
        await _accessService.RevokeAccessAsync(userId, folder.Id);

        // Assert
        var revoked = await _context.FolderAccess
            .FirstOrDefaultAsync(a => a.FolderId == folder.Id && a.UserId == userId);
        await Assert.That(revoked).IsNull();
    }

    [Test]
    public async Task GetFolderAccessListAsync_ReturnsAllGrants()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        await _accessService.GrantAccessAsync(user1, folder.Id, ownerId, FolderAccessLevel.Read);
        await _accessService.GrantAccessAsync(user2, folder.Id, ownerId, FolderAccessLevel.Write);

        // Act
        var accessList = await _accessService.GetFolderAccessListAsync(folder.Id);

        // Assert
        await Assert.That(accessList).HasCount().EqualTo(2);
        await Assert.That(accessList.Any(a => a.UserId == user1)).IsTrue();
        await Assert.That(accessList.Any(a => a.UserId == user2)).IsTrue();
        var readGrant = accessList.Single(a => a.UserId == user1);
        await Assert.That(readGrant.Permission).IsEqualTo(FolderAccessPermissionLevel.Read);
        var writeGrant = accessList.Single(a => a.UserId == user2);
        await Assert.That(writeGrant.Permission).IsEqualTo(FolderAccessPermissionLevel.Write);
    }

    [Test]
    public async Task GetFolderAccessListAsync_ExcludesExpiredGrants()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        await _accessService.GrantAccessAsync(user1, folder.Id, ownerId, FolderAccessLevel.Read);

        var expiredAccess = new FolderAccessEntity
        {
            FolderId = folder.Id,
            UserId = user2,
            GrantedById = ownerId,
            Permission = FolderAccessLevel.Read,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.FolderAccess.Add(expiredAccess);
        await _context.SaveChangesAsync();

        // Act
        var accessList = await _accessService.GetFolderAccessListAsync(folder.Id);

        // Assert
        await Assert.That(accessList).HasCount().EqualTo(1);
        await Assert.That(accessList[0].UserId).IsEqualTo(user1);
    }

    [Test]
    public async Task GetAccessibleFolderIdsAsync_IncludesOwnedFolders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var folder1 = await _folderService.CreateFolderAsync(userId, "Test1", null, "/test1");
        var folder2 = await _folderService.CreateFolderAsync(userId, "Test2", null, "/test2");
        await _folderService.CreateFolderAsync(Guid.NewGuid(), "Other", null, "/other");

        // Act
        var accessible = await _accessService.GetAccessibleFolderIdsAsync(userId, FolderAccessLevel.Read);

        // Assert
        await Assert.That(accessible).HasCount().EqualTo(2);
        await Assert.That(accessible).Contains(folder1.Id);
        await Assert.That(accessible).Contains(folder2.Id);
    }

    [Test]
    public async Task GetAccessibleFolderIdsAsync_IncludesSharedRoots()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sharedRoot = await _folderService.CreateFolderAsync(null, "Shared", null, "/shared");

        // Act
        var accessible = await _accessService.GetAccessibleFolderIdsAsync(userId, FolderAccessLevel.Read);

        // Assert
        await Assert.That(accessible).Contains(sharedRoot.Id);
    }

    [Test]
    public async Task GetAccessibleFolderIdsAsync_IncludesGrantedAccess()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        await _accessService.GrantAccessAsync(userId, folder.Id, ownerId, FolderAccessLevel.Write);

        // Act
        var accessible = await _accessService.GetAccessibleFolderIdsAsync(userId, FolderAccessLevel.Read);

        // Assert
        await Assert.That(accessible).Contains(folder.Id);
    }

    [Test]
    public async Task GetAccessibleFolderIdsAsync_FiltersByPermissionLevel()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var readFolder = await _folderService.CreateFolderAsync(ownerId, "Read", null, "/read");
        var writeFolder = await _folderService.CreateFolderAsync(ownerId, "Write", null, "/write");

        await _accessService.GrantAccessAsync(userId, readFolder.Id, ownerId, FolderAccessLevel.Read);
        await _accessService.GrantAccessAsync(userId, writeFolder.Id, ownerId, FolderAccessLevel.Write);

        // Act - Request Write permission
        var accessible = await _accessService.GetAccessibleFolderIdsAsync(userId, FolderAccessLevel.Write);

        // Assert - Should only include folder with Write or Admin permission
        await Assert.That(accessible).HasCount().EqualTo(1);
        await Assert.That(accessible).Contains(writeFolder.Id);
    }

    [Test]
    public async Task GetAccessibleFolderIdsAsync_ExcludesExpiredGrants()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(ownerId, "Test", null, "/test");

        var expiredAccess = new FolderAccessEntity
        {
            FolderId = folder.Id,
            UserId = userId,
            GrantedById = ownerId,
            Permission = FolderAccessLevel.Admin,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.FolderAccess.Add(expiredAccess);
        await _context.SaveChangesAsync();

        // Act
        var accessible = await _accessService.GetAccessibleFolderIdsAsync(userId, FolderAccessLevel.Read);

        // Assert
        await Assert.That(accessible).DoesNotContain(folder.Id);
    }
}
