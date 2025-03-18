using Microsoft.AspNetCore.Identity;

namespace Jules.Util.Security.Models;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole(string name) : base(name)
    {
    }
}