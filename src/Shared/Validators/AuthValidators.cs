using FluentValidation;
using MannaHp.Shared.DTOs;

namespace MannaHp.Shared.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
	public LoginRequestValidator()
	{
		RuleFor(x => x.Email).NotEmpty().EmailAddress();
		RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
	}
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
	public RegisterRequestValidator()
	{
		RuleFor(x => x.Email).NotEmpty().EmailAddress();
		RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
			.Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
			.Matches("[0-9]").WithMessage("Password must contain at least one digit");
		RuleFor(x => x.DisplayName).MaximumLength(100);
	}
}
