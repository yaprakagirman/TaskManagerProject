using FluentValidation;
using TaskManager.Application.Common;
using TaskManager.Application.DTOs.Tasks;

namespace TaskManager.Application.Validators.Tasks;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(request => request.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.Task.TitleRequired)
            .MaximumLength(200).WithMessage(ValidationMessages.Task.TitleMaxLength);

        RuleFor(request => request.Description)
            .MaximumLength(1000).WithMessage(ValidationMessages.Task.DescriptionMaxLength);

        RuleFor(request => request.Priority)
            .IsInEnum()
            .WithMessage(ValidationMessages.Task.PriorityInvalid);

        RuleFor(request => request.DueDate)
            .Must(BeTodayOrFuture)
            .WithMessage(ValidationMessages.Task.DueDateCannotBePast);
    }

    private static bool BeTodayOrFuture(DateTime? dueDate)
    {
        return dueDate is null || dueDate.Value.Date >= DateTime.UtcNow.Date;
    }
}
