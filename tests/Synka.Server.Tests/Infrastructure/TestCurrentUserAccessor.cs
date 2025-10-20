using Synka.Server.Services;

namespace Synka.Server.Tests.Infrastructure;

internal sealed class TestCurrentUserAccessor : ICurrentUserAccessor
{
    public Guid CurrentUserId { get; set; }

    public Guid GetCurrentUserId() => CurrentUserId;
}
