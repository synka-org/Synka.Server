using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synka.Server.Data;
using Synka.Server.Data.Entities;
using Synka.Server.Services;
using Synka.Server.Tests.Infrastructure;

namespace Synka.Server.Tests.Services;

internal sealed class RootFolderSynchronizationServiceTests : IDisposable
{
    private const string UserBasePath = "/mnt/users";
    private const string SharedDocsPath = "/mnt/shared/docs";

    private readonly TestWebApplicationFactory _baseFactory;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly SynkaDbContext _dbContext;
    private readonly IRootFolderSynchronizationService _synchronizer;
    private readonly MockFileSystemService _fileSystem;

    public RootFolderSynchronizationServiceTests()
    {
        _baseFactory = new TestWebApplicationFactory();
        _factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FileSystemWatcher:UserRootBasePath"] = UserBasePath,
                    ["FileSystemWatcher:SharedRootPaths:0"] = SharedDocsPath
                });
            });
        });

        var services = _factory.Services ?? throw new InvalidOperationException("Factory services were not initialized.");

        _scope = services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
        _synchronizer = _scope.ServiceProvider.GetRequiredService<IRootFolderSynchronizationService>();
        _fileSystem = (MockFileSystemService)_scope.ServiceProvider.GetRequiredService<IFileSystemService>();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _scope.Dispose();
        _factory.Dispose();
        _baseFactory.Dispose();
    }

    [Test]
    public async Task EnsureRootFoldersAsync_CreatesMissingSharedRoot()
    {
        await _synchronizer.EnsureRootFoldersAsync();

        var folder = await _dbContext.Folders
            .SingleAsync(f => f.ParentFolderId == null && f.OwnerId == null);

        var normalizedSharedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(SharedDocsPath));

        await Assert.That(folder.PhysicalPath).IsEqualTo(normalizedSharedPath);
        await Assert.That(_fileSystem.DirectoryExists(normalizedSharedPath)).IsTrue();
    }

    [Test]
    public async Task EnsureRootFoldersAsync_CreatesUserRootForExistingUser()
    {
        var userId = Guid.NewGuid();
        AddUser(userId);

        await _synchronizer.EnsureRootFoldersAsync();

        var folder = await _dbContext.Folders
            .SingleAsync(f => f.ParentFolderId == null && f.OwnerId == userId);

        var expectedPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(Path.Combine(UserBasePath, userId.ToString("D"))));

        await Assert.That(folder.PhysicalPath).IsEqualTo(expectedPath);
        await Assert.That(folder.Name).IsEqualTo(userId.ToString("D"));
        await Assert.That(_fileSystem.DirectoryExists(expectedPath)).IsTrue();
    }

    [Test]
    public async Task EnsureRootFoldersAsync_RestoresSoftDeletedSharedRoot()
    {
        await _synchronizer.EnsureRootFoldersAsync();

        var sharedRoot = await _dbContext.Folders
            .SingleAsync(f => f.ParentFolderId == null && f.OwnerId == null);

        sharedRoot.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        await _synchronizer.EnsureRootFoldersAsync();

        await _dbContext.Entry(sharedRoot).ReloadAsync();

        await Assert.That(sharedRoot.IsDeleted).IsFalse();
        await Assert.That(_fileSystem.DirectoryExists(sharedRoot.PhysicalPath)).IsTrue();
    }

    [Test]
    public async Task EnsureRootFoldersAsync_UpdatesMismatchedUserRootPath()
    {
        var userId = Guid.NewGuid();
        AddUser(userId);

        const string legacyPath = "/legacy/path";
        var folder = new FolderEntity
        {
            Id = Guid.NewGuid(),
            Name = "legacy",
            OwnerId = userId,
            ParentFolderId = null,
            PhysicalPath = legacyPath,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Folders.Add(folder);
        await _dbContext.SaveChangesAsync();

        await _synchronizer.EnsureRootFoldersAsync();

        await _dbContext.Entry(folder).ReloadAsync();

        var expectedPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(Path.Combine(UserBasePath, userId.ToString("D"))));

        await Assert.That(folder.PhysicalPath).IsEqualTo(expectedPath);
        await Assert.That(folder.Name).IsEqualTo(userId.ToString("D"));
        await Assert.That(_fileSystem.DirectoryExists(expectedPath)).IsTrue();
    }

    private void AddUser(Guid userId)
    {
        var email = $"{userId}@example.com";

        var user = new ApplicationUserEntity
        {
            Id = userId,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("D"),
            ConcurrencyStamp = Guid.NewGuid().ToString("D")
        };

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
    }
}
