using FluentValidation;
using TaskManager.Application.Common;
using TaskManager.Application.DTOs.Tags;
using TaskManager.Domain.Enums;

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

        RuleFor(request => request.RequiredExpertise)
            .Must(IsValidRequiredExpertise)
            .WithMessage(ValidationMessages.Tag.RequiredExpertiseInvalid);
    }

    private static bool IsValidRequiredExpertise(UserExpertise? expertise)
    {
        return expertise is null or
            UserExpertise.None or
            UserExpertise.Backend or
            UserExpertise.Frontend or
            UserExpertise.QA or
            UserExpertise.DevOps;
    }
}
