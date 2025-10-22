using Synka.Server.Services;

namespace Synka.Server.Tests.Infrastructure;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes - instantiated via DI
internal sealed class TestCurrentUserAccessor : ICurrentUserAccessor
#pragma warning restore CA1812
{
    public Guid CurrentUserId { get; set; }

    public Guid GetCurrentUserId() => CurrentUserId;
}
