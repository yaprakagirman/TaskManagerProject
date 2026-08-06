using Microsoft.Extensions.Logging;
using TaskManager.Application.DTOs.Auth;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IRepository<User> userRepository,
        IPasswordHasherService passwordHasherService,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasherService = passwordHasherService;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await GetUserByEmailAsync(request.Email);

        if (existingUser is not null)
        {
            _logger.LogWarning(
                "Register failed. Email already exists. Email: {Email}",
                request.Email);

            throw new ConflictException("This email address is already used.");
        }

        var user = CreateUser(request);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "User registered successfully. UserId: {UserId}, Email: {Email}",
            user.Id,
            user.Email);

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await GetUserByEmailAsync(request.Email);

        if (user is null)
        {
            _logger.LogWarning(
                "Login failed. User not found. Email: {Email}",
                request.Email);

            throw new UnauthorizedException("Email or password is incorrect.");
        }

        var isPasswordValid = _passwordHasherService.VerifyPassword(
            request.Password,
            user.PasswordHash);

        if (!isPasswordValid)
        {
            _logger.LogWarning(
                "Login failed. Invalid password. UserId: {UserId}, Email: {Email}",
                user.Id,
                user.Email);

            throw new UnauthorizedException("Email or password is incorrect.");
        }

        _logger.LogInformation(
            "User logged in successfully. UserId: {UserId}, Email: {Email}",
            user.Id,
            user.Email);

        return CreateAuthResponse(user);
    }

    private async Task<User?> GetUserByEmailAsync(string email)
    {
        var users = await _userRepository.FindAsync(user => user.Email == email);

        return users.FirstOrDefault();
    }

    private User CreateUser(RegisterRequest request)
    {
        return new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = _passwordHasherService.HashPassword(request.Password),
            CreatedDate = DateTime.UtcNow
        };
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = _jwtTokenService.GenerateToken(user);

        return new AuthResponse
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Token = token
        };
    }
}
