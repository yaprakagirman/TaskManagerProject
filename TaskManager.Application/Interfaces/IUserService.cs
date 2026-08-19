using TaskManager.Application.DTOs.Users;

namespace TaskManager.Application.Interfaces;

public interface IUserService
{
    Task<List<UserResponse>> GetAllUsersAsync();

    Task<UserResponse?> GetUserByIdAsync(int id);

    Task<UserResponse> CreateUserAsync(CreateUserRequest request);

    Task<UserResponse?> UpdateUserAsync(int id, UpdateUserRequest request);

    Task<bool> DeleteUserAsync(int id);

    Task<UserResponse> UpdateUserRoleAsync(
    int userId,
    UpdateUserRoleRequest request);

    Task<UserResponse> UpdateUserExpertisesAsync(
        int userId,
        UpdateUserExpertisesRequest request);
}
