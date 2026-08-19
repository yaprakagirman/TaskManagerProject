using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs.Users;
using TaskManager.Application.Interfaces;

namespace TaskManager.API.Controllers;

/// <summary>
/// Kullanıcı yönetimi, rol ve uzmanlık güncellemeleri için API uç noktaları.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateUserRequest> _createUserValidator;
    private readonly IValidator<UpdateUserRequest> _updateUserValidator;
    private readonly IValidator<UpdateUserRoleRequest> _updateUserRoleValidator;
    private readonly IValidator<UpdateUserExpertisesRequest> _updateUserExpertisesValidator;

    public UsersController(
        IUserService userService,
        ICurrentUserService currentUserService,
        IValidator<CreateUserRequest> createUserValidator,
        IValidator<UpdateUserRequest> updateUserValidator,
        IValidator<UpdateUserRoleRequest> updateUserRoleValidator,
        IValidator<UpdateUserExpertisesRequest> updateUserExpertisesValidator)
    {
        _userService = userService;
        _currentUserService = currentUserService;
        _createUserValidator = createUserValidator;
        _updateUserValidator = updateUserValidator;
        _updateUserRoleValidator = updateUserRoleValidator;
        _updateUserExpertisesValidator = updateUserExpertisesValidator;
    }

    /// <summary>
    /// Oturum açmış kullanıcının kendi profil bilgilerini getirir.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetMyProfile()
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var user = await _userService.GetUserByIdAsync(userId.Value);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Tüm kullanıcıları listeler (Yalnızca Admin yetkisi ile).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// ID değerine göre kullanıcı detayını getirir.
    /// </summary>
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

    /// <summary>
    /// Yeni bir kullanıcı oluşturur.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
    {
        return ValidateAndExecute<CreateUserRequest, UserResponse>(
            request,
            _createUserValidator,
            async () =>
            {
                var response = await _userService.CreateUserAsync(request);
                return Ok(response);
            });
    }

    /// <summary>
    /// Mevcut bir kullanıcının temel bilgilerini günceller.
    /// </summary>
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

    /// <summary>
    /// Bir kullanıcıyı siler (Soft Delete).
    /// </summary>
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

    /// <summary>
    /// Kullanıcının rolünü günceller (Yalnızca Admin yetkisi ile).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/role")]
    public Task<ActionResult<UserResponse>> UpdateRole(
        int id,
        [FromBody] UpdateUserRoleRequest request)
    {
        return ValidateAndExecute<UpdateUserRoleRequest, UserResponse>(
            request,
            _updateUserRoleValidator,
            async () =>
            {
                var response = await _userService.UpdateUserRoleAsync(id, request);
                return Ok(response);
            });
    }

    /// <summary>
    /// Kullanıcının yetkinliklerini günceller (Yalnızca Admin yetkisi ile).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/expertises")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<UserResponse>> UpdateExpertises(
        int id,
        [FromBody] UpdateUserExpertisesRequest request)
    {
        return ValidateAndExecute<UpdateUserExpertisesRequest, UserResponse>(
            request,
            _updateUserExpertisesValidator,
            async () =>
            {
                var response = await _userService.UpdateUserExpertisesAsync(id, request);
                return Ok(response);
            });
    }
}
