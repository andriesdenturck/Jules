using Jules.Util.Security.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jules.Util.Security;

public class SecurityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public SecurityDbContext(DbContextOptions<SecurityDbContext> options)
        : base(options)
    {
    }
}