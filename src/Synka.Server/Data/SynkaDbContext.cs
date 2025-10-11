using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Synka.Server.Data.Entities;

namespace Synka.Server.Data;

public class SynkaDbContext(DbContextOptions<SynkaDbContext> options)
    : IdentityDbContext<ApplicationUserEntity, IdentityRole<Guid>, Guid>(options)
{
}
