using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Synka.Server.Contracts;
using Synka.Server.Data;
using Synka.Server.Data.Entities;
using Synka.Server.Services;
using Synka.Server.Tests.Infrastructure;

namespace Synka.Server.Tests.Services;

internal sealed class FolderServiceTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly IServiceScope _scope;
    private readonly SynkaDbContext _context;
    private readonly FolderService _folderService;

    public FolderServiceTests()
    {
        _factory = new TestWebApplicationFactory();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
        _folderService = new FolderService(_context);

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
    public async Task CreateFolderAsync_WithValidData_CreatesFolder()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        const string folderName = "Test Folder";
        const string physicalPath = "/test/folder";

        // Act
        var folder = await _folderService.CreateFolderAsync(
            ownerId,
            folderName,
            null,
            physicalPath);

        // Assert
        await Assert.That(folder).IsNotNull();
        await Assert.That(folder.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(folder.OwnerId).IsEqualTo(ownerId);
        await Assert.That(folder.Name).IsEqualTo(folderName);
        await Assert.That(folder.PhysicalPath).IsEqualTo(physicalPath);
        await Assert.That(folder.ParentFolderId).IsNull();
        await Assert.That(folder.IsUserRoot).IsTrue();
        await Assert.That(folder.IsSharedRoot).IsFalse();
    }

    [Test]
    public async Task CreateFolderAsync_WithNullOwner_CreatesSharedRoot()
    {
        // Arrange
        const string folderName = "Shared Folder";
        const string physicalPath = "/shared/folder";

        // Act
        var folder = await _folderService.CreateFolderAsync(
            null,
            folderName,
            null,
            physicalPath);

        // Assert
        await Assert.That(folder).IsNotNull();
        await Assert.That(folder.OwnerId).IsNull();
        await Assert.That(folder.IsSharedRoot).IsTrue();
        await Assert.That(folder.IsUserRoot).IsFalse();
    }

    [Test]
    public async Task CreateFolderAsync_WithParent_CreatesSubfolder()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var parentFolder = await _folderService.CreateFolderAsync(
            ownerId,
            "Parent",
            null,
            "/parent");

        // Act
        var subfolder = await _folderService.CreateFolderAsync(
            ownerId,
            "Child",
            parentFolder.Id,
            null);

        // Assert
        await Assert.That(subfolder).IsNotNull();
        await Assert.That(subfolder.ParentFolderId).IsEqualTo(parentFolder.Id);
        await Assert.That(subfolder.PhysicalPath).IsEqualTo("/parent/Child");
        await Assert.That(subfolder.IsUserRoot).IsFalse();
        await Assert.That(subfolder.IsSharedRoot).IsFalse();
    }

    [Test]
    public async Task CreateFolderAsync_WithInvalidParent_ThrowsArgumentException()
    {
        // Arrange
        var invalidParentId = Guid.NewGuid();

        // Act & Assert
        await Assert.That(async () => await _folderService.CreateFolderAsync(
            Guid.NewGuid(),
            "Child",
            invalidParentId,
            null))
            .Throws<ArgumentException>()
            .And.HasMessageContaining("does not exist");
    }

    [Test]
    public async Task CreateFolderAsync_RootWithoutPhysicalPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.That(async () => await _folderService.CreateFolderAsync(
            Guid.NewGuid(),
            "Root",
            null,
            null))
            .Throws<ArgumentException>()
            .And.HasMessageContaining("Physical path is required");
    }

    [Test]
    public async Task GetFolderAsync_WithExistingFolder_ReturnsFolder()
    {
        // Arrange
        var folder = await _folderService.CreateFolderAsync(
            Guid.NewGuid(),
            "Test",
            null,
            "/test");

        // Act
        var retrieved = await _folderService.GetFolderAsync(folder.Id);

        // Assert
        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved.Id).IsEqualTo(folder.Id);
        await Assert.That(retrieved.Name).IsEqualTo(folder.Name);
    }

    [Test]
    public async Task GetFolderAsync_WithNonExistingFolder_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act & Assert
        await Assert.That(async () => await _folderService.GetFolderAsync(nonExistingId))
            .Throws<InvalidOperationException>()
            .And.HasMessageContaining("not found");
    }

    [Test]
    public async Task GetUserRootFoldersAsync_ReturnsOnlyUserRoots()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var userRoot1 = await _folderService.CreateFolderAsync(userId, "Root1", null, "/root1");
        var userRoot2 = await _folderService.CreateFolderAsync(userId, "Root2", null, "/root2");
        await _folderService.CreateFolderAsync(otherUserId, "OtherRoot", null, "/other");
        await _folderService.CreateFolderAsync(null, "SharedRoot", null, "/shared");
        await _folderService.CreateFolderAsync(userId, "Subfolder", userRoot1.Id, null);

        // Act
        var roots = await _folderService.GetUserRootFoldersAsync(userId);

        // Assert
        await Assert.That(roots).HasCount().EqualTo(2);
        await Assert.That(roots.All(f => f.OwnerId == userId && f.ParentFolderId == null)).IsTrue();
    }

    [Test]
    public async Task GetSharedRootFoldersAsync_ReturnsOnlySharedRoots()
    {
        // Arrange
        var sharedRoot1 = await _folderService.CreateFolderAsync(null, "Shared1", null, "/shared1");
        var sharedRoot2 = await _folderService.CreateFolderAsync(null, "Shared2", null, "/shared2");
        await _folderService.CreateFolderAsync(Guid.NewGuid(), "UserRoot", null, "/user");

        // Act
        var roots = await _folderService.GetSharedRootFoldersAsync();

        // Assert
        await Assert.That(roots).HasCount().EqualTo(2);
        await Assert.That(roots.All(f => f.OwnerId == null && f.ParentFolderId == null)).IsTrue();
    }

    [Test]
    public async Task GetSubfoldersAsync_ReturnsOnlyDirectChildren()
    {
        // Arrange
        var parent = await _folderService.CreateFolderAsync(Guid.NewGuid(), "Parent", null, "/parent");
        var child1 = await _folderService.CreateFolderAsync(Guid.NewGuid(), "Child1", parent.Id, null);
        var child2 = await _folderService.CreateFolderAsync(Guid.NewGuid(), "Child2", parent.Id, null);
        await _folderService.CreateFolderAsync(Guid.NewGuid(), "Grandchild", child1.Id, null);

        // Act
        var subfolders = await _folderService.GetSubfoldersAsync(parent.Id);

        // Assert
        await Assert.That(subfolders).HasCount().EqualTo(2);
        await Assert.That(subfolders.All(f => f.ParentFolderId == parent.Id)).IsTrue();
    }

    [Test]
    public async Task DeleteFolderAsync_SoftDelete_MarksFolderAsDeleted()
    {
        // Arrange
        var folder = await _folderService.CreateFolderAsync(Guid.NewGuid(), "Test", null, "/test");

        // Act
        await _folderService.DeleteFolderAsync(folder.Id, softDelete: true);

        // Assert
        var deleted = await _context.Folders.FindAsync(folder.Id);
        await Assert.That(deleted).IsNotNull();
        await Assert.That(deleted!.IsDeleted).IsTrue();
    }

    [Test]
    public async Task DeleteFolderAsync_SoftDelete_RecursivelyMarksChildren()
    {
        // Arrange
        var parent = await _folderService.CreateFolderAsync(Guid.NewGuid(), "Parent", null, "/parent");
        var child = await _folderService.CreateFolderAsync(Guid.NewGuid(), "Child", parent.Id, null);

        // Add a file to the child folder
        var file = new FileMetadataEntity
        {
            UploadedById = Guid.NewGuid(),
            FolderId = child.Id,
            FileName = "test.txt",
            ContentType = "text/plain",
            SizeBytes = 100,
            StoragePath = "/parent/Child/test.txt"
        };
        _context.FileMetadata.Add(file);
        await _context.SaveChangesAsync();

        // Act
        await _folderService.DeleteFolderAsync(parent.Id, softDelete: true);

        // Assert
        var deletedParent = await _context.Folders
            .Include(f => f.ChildFolders)
            .Include(f => f.Files)
            .FirstAsync(f => f.Id == parent.Id);

        await Assert.That(deletedParent.IsDeleted).IsTrue();
        await Assert.That(deletedParent.ChildFolders.First().IsDeleted).IsTrue();
        await Assert.That(deletedParent.ChildFolders.First().Files.First().IsDeleted).IsTrue();
    }

    [Test]
    public async Task DeleteFolderAsync_HardDelete_RemovesFolder()
    {
        // Arrange
        var folder = await _folderService.CreateFolderAsync(Guid.NewGuid(), "Test", null, "/test");

        // Act
        await _folderService.DeleteFolderAsync(folder.Id, softDelete: false);

        // Assert
        var deleted = await _context.Folders.FindAsync(folder.Id);
        await Assert.That(deleted).IsNull();
    }

    [Test]
    public async Task RestoreFolderAsync_RestoresSoftDeletedFolder()
    {
        // Arrange
        var folder = await _folderService.CreateFolderAsync(Guid.NewGuid(), "Test", null, "/test");
        await _folderService.DeleteFolderAsync(folder.Id, softDelete: true);

        // Act
        await _folderService.RestoreFolderAsync(folder.Id);

        // Assert
        var restored = await _context.Folders.FindAsync(folder.Id);
        await Assert.That(restored).IsNotNull();
        await Assert.That(restored!.IsDeleted).IsFalse();
    }

    [Test]
    public async Task RestoreFolderAsync_RecursivelyRestoresChildren()
    {
        // Arrange
        var parent = await _folderService.CreateFolderAsync(Guid.NewGuid(), "Parent", null, "/parent");
        var child = await _folderService.CreateFolderAsync(Guid.NewGuid(), "Child", parent.Id, null);
        await _folderService.DeleteFolderAsync(parent.Id, softDelete: true);

        // Act
        await _folderService.RestoreFolderAsync(parent.Id);

        // Assert
        var restoredParent = await _context.Folders
            .Include(f => f.ChildFolders)
            .FirstAsync(f => f.Id == parent.Id);

        await Assert.That(restoredParent.IsDeleted).IsFalse();
        await Assert.That(restoredParent.ChildFolders.First().IsDeleted).IsFalse();
    }

    [Test]
    public async Task ExistsAsync_WithExistingFolder_ReturnsTrue()
    {
        // Arrange
        var folder = await _folderService.CreateFolderAsync(Guid.NewGuid(), "Test", null, "/test");

        // Act
        var exists = await _folderService.ExistsAsync(folder.Id);

        // Assert
        await Assert.That(exists).IsTrue();
    }

    [Test]
    public async Task ExistsAsync_WithDeletedFolder_ReturnsFalse()
    {
        // Arrange
        var folder = await _folderService.CreateFolderAsync(Guid.NewGuid(), "Test", null, "/test");
        await _folderService.DeleteFolderAsync(folder.Id, softDelete: true);

        // Act
        var exists = await _folderService.ExistsAsync(folder.Id);

        // Assert
        await Assert.That(exists).IsFalse();
    }

    [Test]
    public async Task ExistsAsync_WithNonExistingFolder_ReturnsFalse()
    {
        // Act
        var exists = await _folderService.ExistsAsync(Guid.NewGuid());

        // Assert
        await Assert.That(exists).IsFalse();
    }

    [Test]
    public async Task GetAccessibleFoldersAsync_IncludesOwnedFolders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var owned = await _folderService.CreateFolderAsync(userId, "Owned", null, "/owned");
        await _folderService.CreateFolderAsync(Guid.NewGuid(), "NotOwned", null, "/notowned");

        // Act
        var accessible = await _folderService.GetAccessibleFoldersAsync(userId);

        // Assert
        await Assert.That(accessible).HasCount().EqualTo(1);
        await Assert.That(accessible[0].Id).IsEqualTo(owned.Id);
    }

    [Test]
    public async Task GetAccessibleFoldersAsync_IncludesSharedRoots()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sharedRoot = await _folderService.CreateFolderAsync(null, "Shared", null, "/shared");

        // Act
        var accessible = await _folderService.GetAccessibleFoldersAsync(userId);

        // Assert
        await Assert.That(accessible).HasCount().EqualTo(1);
        await Assert.That(accessible[0].Id).IsEqualTo(sharedRoot.Id);
    }

    [Test]
    public async Task GetAccessibleFoldersAsync_ExcludesDeletedFolders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var folder = await _folderService.CreateFolderAsync(userId, "Test", null, "/test");
        await _folderService.DeleteFolderAsync(folder.Id, softDelete: true);

        // Act
        var accessible = await _folderService.GetAccessibleFoldersAsync(userId);

        // Assert
        await Assert.That(accessible).IsEmpty();
    }
}
