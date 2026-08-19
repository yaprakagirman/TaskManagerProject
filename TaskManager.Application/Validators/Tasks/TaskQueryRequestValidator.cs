using FluentValidation;
using TaskManager.Application.DTOs.Tasks;

namespace TaskManager.Application.Validators.Tasks;

public sealed class TaskQueryRequestValidator : AbstractValidator<TaskQueryRequest>
{
    private static readonly string[] SortableFields =
        ["id", "title", "status", "priority", "dueDate", "createdDate"];

    public TaskQueryRequestValidator()
    {
        RuleFor(request => request.Page).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Search).MaximumLength(200);
        RuleFor(request => request.Status)
            .Must(status => !status.HasValue || Enum.IsDefined(status.Value));
        RuleFor(request => request.Priority)
            .Must(priority => !priority.HasValue || Enum.IsDefined(priority.Value));
        RuleFor(request => request.ProjectId).GreaterThan(0).When(request => request.ProjectId.HasValue);
        RuleFor(request => request.AssignedUserId).GreaterThan(0).When(request => request.AssignedUserId.HasValue);
        RuleFor(request => request.TagId).GreaterThan(0).When(request => request.TagId.HasValue);
        RuleFor(request => request.SortBy)
            .Must(value => SortableFields.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", SortableFields)}.");
        RuleFor(request => request.SortDirection)
            .Must(value => string.Equals(value, "asc", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be 'asc' or 'desc'.");
    }
}
