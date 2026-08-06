using TaskManager.Domain.Enums;

namespace TaskManager.Application.DTOs.Users;

public class UpdateUserRoleRequest
{
    public UserRole Role { get; set; }
}