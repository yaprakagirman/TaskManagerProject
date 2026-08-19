using TaskManager.Domain.Enums;

namespace TaskManager.Application.DTOs.Users;

public class EligibleUserResponse
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public UserExpertise Expertises { get; set; }
}
