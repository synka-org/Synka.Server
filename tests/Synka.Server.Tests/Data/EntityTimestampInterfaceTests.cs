using System;
using System.Linq;
using System.Reflection;
using Synka.Server.Data.Entities;

namespace Synka.Server.Tests.Data;

internal sealed class EntityTimestampInterfaceTests
{
    private static readonly Assembly EntitiesAssembly = typeof(IHasCreatedAt).Assembly;

    [Test]
    public async Task EntitiesWithCreatedAtImplementInterface()
    {
        var failingTypes = EntitiesAssembly
            .GetTypes()
            .Where(type => IsConcreteEntity(type) &&
                           type.GetProperty(nameof(IHasCreatedAt.CreatedAt)) is not null &&
                           !typeof(IHasCreatedAt).IsAssignableFrom(type))
            .ToList();

        await Assert.That(failingTypes).IsEmpty();
    }

    [Test]
    public async Task EntitiesWithUpdatedAtImplementInterface()
    {
        var failingTypes = EntitiesAssembly
            .GetTypes()
            .Where(type => IsConcreteEntity(type) &&
                           type.GetProperty(nameof(IHasUpdatedAt.UpdatedAt)) is not null &&
                           !typeof(IHasUpdatedAt).IsAssignableFrom(type))
            .ToList();

        await Assert.That(failingTypes).IsEmpty();
    }

    private static bool IsConcreteEntity(Type type) =>
        type is { IsClass: true, IsAbstract: false } &&
        string.Equals(type.Namespace, typeof(IHasCreatedAt).Namespace, StringComparison.Ordinal);
}
