using Microsoft.AspNetCore.Identity;

namespace Synka.Server.Data.Entities;

public class ApplicationUserEntity : IdentityUser<Guid>, IHasCreatedAt
{
    public string? DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
