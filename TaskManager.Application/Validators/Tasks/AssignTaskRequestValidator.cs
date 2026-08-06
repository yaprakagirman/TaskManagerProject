using FluentValidation;
using TaskManager.Application.Common;
using TaskManager.Application.DTOs.Tasks;

namespace TaskManager.Application.Validators.Tasks;

public class AssignTaskRequestValidator : AbstractValidator<AssignTaskRequest>
{
    public AssignTaskRequestValidator()
    {
        RuleFor(request => request.AssignedUserId)
            .GreaterThan(0).WithMessage(ValidationMessages.Task.AssignedUserIdGreaterThanZero);
    }
}