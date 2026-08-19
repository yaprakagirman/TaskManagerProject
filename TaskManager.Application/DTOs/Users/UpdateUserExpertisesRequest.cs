using TaskManager.Domain.Enums;

namespace TaskManager.Application.DTOs.Users;

public class UpdateUserExpertisesRequest
{
    public List<UserExpertise>? Expertises { get; set; } = [];
}
