using Microsoft.AspNetCore.Identity;

namespace MannaHp.Server.Data;

public class AppUser : IdentityUser
{
	public string? DisplayName { get; set; }
	public string Role { get; set; } = "Customer";
	public DateTime CreatedAt { get; set; }
}
