using System.Linq;
using FluentValidation;
using TaskManager.Application.Common;
using TaskManager.Application.DTOs.Tags;

namespace TaskManager.Application.Validators.Tags;

public class TagTasksRequestValidator
    : AbstractValidator<TagTasksRequest>
{
    public TagTasksRequestValidator()
    {
        RuleFor(request => request.TaskIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage(ValidationMessages.Tag.TaskIdsRequired)
            .Must(taskIds =>
                taskIds.Distinct().Count() == taskIds.Count)
                .WithMessage(ValidationMessages.Tag.TaskIdsUnique);

        RuleForEach(request => request.TaskIds)
            .GreaterThan(0)
                .WithMessage(ValidationMessages.Tag.TaskIdGreaterThanZero);
    }
}