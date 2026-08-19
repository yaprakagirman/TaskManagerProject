using FluentValidation;
using TaskManager.Application.DTOs.Tasks;

namespace TaskManager.Application.Validators.Tasks;

public sealed class TransferTaskAssignmentRequestValidator
    : AbstractValidator<TransferTaskAssignmentRequest>
{
    public TransferTaskAssignmentRequestValidator()
    {
        RuleFor(request => request.CurrentAssignedUserId).GreaterThan(0);
        RuleFor(request => request.NewAssignedUserId).GreaterThan(0);
        RuleFor(request => request.NewAssignedUserId)
            .NotEqual(request => request.CurrentAssignedUserId)
            .WithMessage("CurrentAssignedUserId and NewAssignedUserId must be different.");
    }
}
