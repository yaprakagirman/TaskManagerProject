using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace TaskManager.API.Controllers;

public abstract class BaseApiController : ControllerBase
{
    protected BadRequestObjectResult ValidationError(ValidationResult validationResult)
    {
        return BadRequest(validationResult.Errors.Select(error => new
        {
            field = error.PropertyName,
            message = error.ErrorMessage
        }));
    }

    protected async Task<ActionResult<TResponse>> ValidateAndExecute<TRequest, TResponse>(
        TRequest request,
        IValidator<TRequest> validator,
        Func<Task<ActionResult<TResponse>>> action)
    {
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return ValidationError(validationResult);
        }

        return await action();
    }

    protected async Task<IActionResult> ValidateAndExecute<TRequest>(
        TRequest request,
        IValidator<TRequest> validator,
        Func<Task<IActionResult>> action)
    {
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return ValidationError(validationResult);
        }

        return await action();
    }

    protected async Task<IActionResult>
    ValidateAndExecuteNoContent<TRequest>(
        TRequest request,
        IValidator<TRequest> validator,
        Func<Task> action)
    {
        var validationResult =
            await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return ValidationError(validationResult);
        }

        await action();

        return NoContent();
    }
}
