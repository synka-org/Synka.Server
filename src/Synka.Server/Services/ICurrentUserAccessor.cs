namespace Synka.Server.Services;

public interface ICurrentUserAccessor
{
    Guid GetCurrentUserId();
}
