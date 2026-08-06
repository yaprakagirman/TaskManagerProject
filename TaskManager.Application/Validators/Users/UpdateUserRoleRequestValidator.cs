using FluentValidation;
using TaskManager.Application.DTOs.Users;

namespace TaskManager.Application.Validators.Users;

public class UpdateUserRoleRequestValidator
    : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("A valid user role must be selected.");
    }
}