using Microsoft.AspNetCore.Identity;

namespace EventManagementSystem.Data;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;
}