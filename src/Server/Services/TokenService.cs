using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MannaHp.Server.Data;
using Microsoft.IdentityModel.Tokens;

namespace MannaHp.Server.Services;

public class TokenService(IConfiguration config)
{
	public (string Token, DateTime ExpiresAt) CreateToken(AppUser user)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, user.Id),
			new(ClaimTypes.Email, user.Email!),
			new(ClaimTypes.Name, user.DisplayName ?? user.Email!),
			new(ClaimTypes.Role, user.Role)
		};

		var key = new SymmetricSecurityKey(
			Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var expires = DateTime.UtcNow.AddMinutes(
			int.Parse(config["Jwt:ExpiresInMinutes"] ?? "1440"));

		var token = new JwtSecurityToken(
			issuer: config["Jwt:Issuer"],
			audience: config["Jwt:Audience"],
			claims: claims,
			expires: expires,
			signingCredentials: creds);

		return (new JwtSecurityTokenHandler().WriteToken(token), expires);
	}
}
