using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs.Auth;
using TaskManager.Application.Interfaces;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    [HttpPost("register")]
    public Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        return ValidateAndExecute<RegisterRequest, AuthResponse>(
            request,
            _registerValidator,
            async () =>
            {
                var response = await _authService.RegisterAsync(request);
                return Ok(response);
            });
    }

    [HttpPost("login")]
    public Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        return ValidateAndExecute<LoginRequest, AuthResponse>(
            request,
            _loginValidator,
            async () =>
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            });
    }
}