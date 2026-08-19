using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskManager.Application.DTOs.Users;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Services;

public class UserService : IUserService
{
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<UserResponse>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();

        _logger.LogInformation("All users listed. Count: {UserCount}", users.Count);

        return _mapper.Map<List<UserResponse>>(users);
    }

    public async Task<UserResponse?> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user is null)
        {
            _logger.LogWarning("User not found. UserId: {UserId}", id);
            return null;
        }

        _logger.LogInformation("User retrieved successfully. UserId: {UserId}", id);

        return _mapper.Map<UserResponse>(user);
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request)
    {
        if (await IsEmailAlreadyUsedAsync(request.Email))
        {
            _logger.LogWarning(
                "User creation failed. Email already exists. Email: {Email}",
                request.Email);

            throw new ConflictException("This email address is already used.");
        }

        var user = _mapper.Map<User>(request);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "User created successfully. UserId: {UserId}, Email: {Email}",
            user.Id,
            user.Email);

        return _mapper.Map<UserResponse>(user);
    }

    public async Task<UserResponse?> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user is null)
        {
            _logger.LogWarning("User update failed. User not found. UserId: {UserId}", id);
            return null;
        }

        if (await IsEmailAlreadyUsedByAnotherUserAsync(request.Email, id))
        {
            _logger.LogWarning(
                "User update failed. Email already exists. UserId: {UserId}, Email: {Email}",
                id,
                request.Email);

            throw new ConflictException("This email address is already used by another user.");
        }

        _mapper.Map(request, user);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User updated successfully. UserId: {UserId}", user.Id);

        return _mapper.Map<UserResponse>(user);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user is null)
        {
            _logger.LogWarning("User delete failed. User not found. UserId: {UserId}", id);
            return false;
        }

        _userRepository.Delete(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User deleted successfully. UserId: {UserId}", id);

        return true;
    }

    private async Task<bool> IsEmailAlreadyUsedAsync(string email)
    {
        var users = await _userRepository.FindAsync(user => user.Email == email);

        return users.Any();
    }

    private async Task<bool> IsEmailAlreadyUsedByAnotherUserAsync(string email, int userId)
    {
        var users = await _userRepository.FindAsync(user =>
            user.Email == email &&
            user.Id != userId);

        return users.Any();
    }

    public async Task<UserResponse> UpdateUserRoleAsync(
    int userId,
    UpdateUserRoleRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user is null)
        {
            _logger.LogWarning(
                "User role update failed. User not found. UserId: {UserId}",
                userId);

            throw new NotFoundException("User was not found.");
        }

        user.Role = request.Role;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "User role updated successfully. UserId: {UserId}, NewRole: {NewRole}",
            userId,
            request.Role);

        return _mapper.Map<UserResponse>(user);
    }

    public async Task<UserResponse> UpdateUserExpertisesAsync(
        int userId,
        UpdateUserExpertisesRequest request)
    {
        if (userId <= 0)
        {
            throw new BadRequestException(
                "User ID must be greater than zero.");
        }

        var user = await _userRepository.GetByIdAsync(userId);

        if (user is null)
        {
            _logger.LogWarning(
                "User expertise update failed. User not found. UserId: {UserId}",
                userId);

            throw new NotFoundException("User was not found.");
        }

        var expertises = request.Expertises
            ?? throw new BadRequestException(
                "Expertises list cannot be null.");

        user.Expertises = expertises.Aggregate(
            UserExpertise.None,
            (combined, expertise) => combined | expertise);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "User expertises updated successfully. UserId: {UserId}, Expertises: {Expertises}",
            userId,
            user.Expertises);

        return _mapper.Map<UserResponse>(user);
    }
}
