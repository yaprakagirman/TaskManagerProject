using FluentValidation;
using TaskManager.Application.Common;
using TaskManager.Application.DTOs.Users;

namespace TaskManager.Application.Validators.Users;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
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
    }
}