using FluentValidation;
using TaskManager.Application.Common;
using TaskManager.Application.DTOs.Tasks;

namespace TaskManager.Application.Validators.Tasks;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(request => request.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.Task.TitleRequired)
            .MaximumLength(200).WithMessage(ValidationMessages.Task.TitleMaxLength);

        RuleFor(request => request.Description)
            .MaximumLength(1000).WithMessage(ValidationMessages.Task.DescriptionMaxLength);

        RuleFor(request => request.ProjectId)
            .GreaterThan(0)
            .When(request => request.ProjectId.HasValue)
            .WithMessage(
                ValidationMessages.Task.ProjectIdGreaterThanZero);

        RuleFor(request => request.ParentTaskId)
            .GreaterThan(0)
            .When(request => request.ParentTaskId.HasValue)
            .WithMessage(
                ValidationMessages.Task.ParentTaskIdGreaterThanZero);

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
