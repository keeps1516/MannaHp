using System.Security.Claims;
using MannaHp.Server.Data;
using MannaHp.Server.Filters;
using MannaHp.Server.Services;
using MannaHp.Shared.DTOs;
using Microsoft.AspNetCore.Identity;

namespace MannaHp.Server.Endpoints;

public static class AuthEndpoints
{
	public static void MapAuthEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/api/auth").WithTags("Auth");

		// POST /api/auth/login
		group.MapPost("/login", async (LoginRequest req,
			UserManager<AppUser> userManager, TokenService tokenService) =>
		{
			var user = await userManager.FindByEmailAsync(req.Email);
			if (user is null)
				return Results.Unauthorized();

			var valid = await userManager.CheckPasswordAsync(user, req.Password);
			if (!valid)
				return Results.Unauthorized();

			var (token, expiresAt) = tokenService.CreateToken(user);
			return Results.Ok(new AuthResponse(
				token, user.Email!, user.DisplayName ?? user.Email!, user.Role, expiresAt));
		}).AddEndpointFilter<ValidationFilter<LoginRequest>>();

		// POST /api/auth/register (Owner only — creates Staff accounts)
		group.MapPost("/register", async (RegisterRequest req,
			UserManager<AppUser> userManager, TokenService tokenService) =>
		{
			var existing = await userManager.FindByEmailAsync(req.Email);
			if (existing is not null)
				return Results.Conflict(new { Error = "Email already registered" });

			var user = new AppUser
			{
				UserName = req.Email,
				Email = req.Email,
				DisplayName = req.DisplayName,
				Role = "Staff"
			};

			var result = await userManager.CreateAsync(user, req.Password);
			if (!result.Succeeded)
				return Results.ValidationProblem(
					result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

			await userManager.AddToRoleAsync(user, "Staff");

			var (token, expiresAt) = tokenService.CreateToken(user);
			return Results.Ok(new AuthResponse(
				token, user.Email!, user.DisplayName ?? user.Email!, user.Role, expiresAt));
		}).RequireAuthorization("Owner")
		  .AddEndpointFilter<ValidationFilter<RegisterRequest>>();

		// GET /api/auth/me
		group.MapGet("/me", async (HttpContext httpContext,
			UserManager<AppUser> userManager) =>
		{
			var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId is null) return Results.Unauthorized();

			var user = await userManager.FindByIdAsync(userId);
			if (user is null) return Results.Unauthorized();

			return Results.Ok(new UserDto(user.Id, user.Email!, user.DisplayName, user.Role));
		}).RequireAuthorization();
	}
}
