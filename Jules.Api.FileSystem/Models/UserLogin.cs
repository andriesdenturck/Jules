using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Jules.Api.FileSystem.Models;

public class UserLogin
{
    [DefaultValue("JulesVerne")]
    [Required]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public string Username { get; set; }

    [DefaultValue("P@ssw0rd")]
    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
    public string Password { get; set; }

    [DefaultValue("User")]
    public string? Role { get; set; } = "User";
}