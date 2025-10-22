using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synka.Server.Data;
using Synka.Server.Services;

namespace Synka.Server.Tests.Infrastructure;

/// <summary>
/// Base test factory that provides database isolation using in-memory SQLite.
/// Each instance gets a unique connection that lives for the lifetime of the factory.
/// </summary>
#pragma warning disable CA1852 // Type can be sealed - intentionally not sealed as this is a base class
internal class TestWebApplicationFactory : WebApplicationFactory<Program>
#pragma warning restore CA1852
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing to skip migrations
        builder.UseEnvironment("Testing")
            .ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                services.RemoveAll<DbContextOptions<SynkaDbContext>>();
                services.RemoveAll<SynkaDbContext>();

                // Create a unique in-memory database for this test instance
                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                services.AddDbContext<SynkaDbContext>(options => options.UseSqlite(_connection));

                // Replace the current user accessor with a test implementation
                services.RemoveAll<ICurrentUserAccessor>();
                services.AddSingleton<ICurrentUserAccessor, TestCurrentUserAccessor>();

                // Replace the file system service with a mock
                services.RemoveAll<IFileSystemService>();
                services.AddSingleton<IFileSystemService, MockFileSystemService>();

                // Ensure the database schema is created for the in-memory database
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
                dbContext.Database.EnsureCreated();

                // Disable foreign key constraints for testing to allow test data without strict referential integrity
                dbContext.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");
            });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
        await base.DisposeAsync();
    }
}
