using FluentValidation;
using TaskManager.Application.Common;
using TaskManager.Application.DTOs.Auth;

namespace TaskManager.Application.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.User.FirstNameRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.User.FirstNameMaxLength);

        RuleFor(request => request.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.User.LastNameRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.User.LastNameMaxLength);

        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.User.EmailRequired)
            .EmailAddress().WithMessage(ValidationMessages.User.EmailInvalid)
            .MaximumLength(200).WithMessage(ValidationMessages.User.EmailMaxLength);

        RuleFor(request => request.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.Auth.PasswordRequired)
            .MinimumLength(6).WithMessage(ValidationMessages.Auth.PasswordMinLength);
    }
}