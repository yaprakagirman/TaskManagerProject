using FluentValidation;
using TaskManager.Application.Common;
using TaskManager.Application.DTOs.Tags;

namespace TaskManager.Application.Validators.Tags;

public class TagRequestValidator : AbstractValidator<TagRequest>
{
    public TagRequestValidator()
    {
        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage(ValidationMessages.Tag.NameRequired)
            .MaximumLength(100)
                .WithMessage(ValidationMessages.Tag.NameMaxLength);
    }
}