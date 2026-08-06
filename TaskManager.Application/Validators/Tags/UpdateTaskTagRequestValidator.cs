using FluentValidation;
using TaskManager.Application.Common;
using TaskManager.Application.DTOs.Tags;

namespace TaskManager.Application.Validators.Tags;

public class UpdateTaskTagRequestValidator
    : AbstractValidator<UpdateTaskTagRequest>
{
    public UpdateTaskTagRequestValidator()
    {
        RuleFor(request => request.TagId)
            .GreaterThan(0)
            .WithMessage(ValidationMessages.Tag.TagIdGreaterThanZero);
    }
}