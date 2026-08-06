using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs.Users;
using TaskManager.Application.Interfaces;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IValidator<CreateUserRequest> _createUserValidator;
    private readonly IValidator<UpdateUserRequest> _updateUserValidator;
    private readonly IValidator<UpdateUserRoleRequest>
       _updateUserRoleValidator;

    public UsersController(
    IUserService userService,
    IValidator<CreateUserRequest> createUserValidator,
    IValidator<UpdateUserRequest> updateUserValidator,
    IValidator<UpdateUserRoleRequest> updateUserRoleValidator)
    {
        _userService = userService;
        _createUserValidator = createUserValidator;
        _updateUserValidator = updateUserValidator;
        _updateUserRoleValidator = updateUserRoleValidator;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    public Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
    {
        return ValidateAndExecute<CreateUserRequest, UserResponse>(
            request,
            _createUserValidator,
            async () =>
            {
                var response = await _userService.CreateUserAsync(request);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = response.Id },
                    response);
            });
    }

    [HttpPut("{id}")]
    public Task<ActionResult<UserResponse>> Update(int id, [FromBody] UpdateUserRequest request)
    {
        return ValidateAndExecute<UpdateUserRequest, UserResponse>(
            request,
            _updateUserValidator,
            async () =>
            {
                var response = await _userService.UpdateUserAsync(id, request);

                if (response is null)
                {
                    return NotFound();
                }

                return Ok(response);
            });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var isDeleted = await _userService.DeleteUserAsync(id);

        if (!isDeleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/role")]
    public Task<ActionResult<UserResponse>> UpdateRole(
    int id,
    [FromBody] UpdateUserRoleRequest request)
    {
        return ValidateAndExecute<
            UpdateUserRoleRequest,
            UserResponse>(
            request,
            _updateUserRoleValidator,
            async () =>
            {
                var response =
                    await _userService.UpdateUserRoleAsync(id, request);

                return Ok(response);
            });
    }
}