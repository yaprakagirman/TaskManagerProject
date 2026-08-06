using FluentValidation;
using TaskManager.Application.Common;
using TaskManager.Application.DTOs.Auth;

namespace TaskManager.Application.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.User.EmailRequired)
            .EmailAddress().WithMessage(ValidationMessages.User.EmailInvalid);

        RuleFor(request => request.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.Auth.PasswordRequired);
    }
}