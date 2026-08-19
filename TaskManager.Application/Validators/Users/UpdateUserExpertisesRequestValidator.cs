using FluentValidation;
using TaskManager.Application.Common;
using TaskManager.Application.DTOs.Users;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Validators.Users;

public class UpdateUserExpertisesRequestValidator
    : AbstractValidator<UpdateUserExpertisesRequest>
{
    public UpdateUserExpertisesRequestValidator()
    {
        RuleFor(request => request.Expertises)
            .NotNull()
            .WithMessage(ValidationMessages.User.ExpertisesRequired);

        RuleFor(request => request.Expertises)
            .Must(HaveUniqueExpertises)
            .WithMessage(ValidationMessages.User.ExpertisesUnique);

        RuleFor(request => request.Expertises)
            .Must(HaveValidNoneCombination)
            .WithMessage(ValidationMessages.User.NoneExpertiseCannotBeCombined);

        RuleFor(request => request.Expertises)
            .Must(HaveOnlySupportedExpertises)
            .WithMessage(ValidationMessages.User.ExpertiseInvalid);
    }

    private static bool HaveUniqueExpertises(
        List<UserExpertise>? expertises)
    {
        return expertises is null ||
            expertises.Distinct().Count() == expertises.Count;
    }

    private static bool HaveValidNoneCombination(
        List<UserExpertise>? expertises)
    {
        return expertises is null ||
            expertises.Count <= 1 ||
            !expertises.Contains(UserExpertise.None);
    }

    private static bool HaveOnlySupportedExpertises(
        List<UserExpertise>? expertises)
    {
        return expertises is null ||
            expertises.All(IsSupportedExpertise);
    }

    private static bool IsSupportedExpertise(UserExpertise expertise)
    {
        return expertise is
            UserExpertise.None or
            UserExpertise.Backend or
            UserExpertise.Frontend or
            UserExpertise.QA or
            UserExpertise.DevOps;
    }
}
