using FluentValidation;
using TaskManager.Application.Common;
using TaskManager.Application.DTOs.Projects;

namespace TaskManager.Application.Validators.Projects;

public class CreateProjectRequestValidator
    : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage(ValidationMessages.Project.NameRequired)
            .MaximumLength(200)
            .WithMessage(ValidationMessages.Project.NameMaxLength);

        RuleFor(request => request.Description)
            .MaximumLength(1000)
            .WithMessage(
                ValidationMessages.Project.DescriptionMaxLength);
    }
}
