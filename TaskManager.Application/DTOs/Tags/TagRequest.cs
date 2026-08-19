namespace TaskManager.Application.DTOs.Tags;

public class TagRequest
{
    public string Name { get; set; } = string.Empty;

    public TaskManager.Domain.Enums.UserExpertise? RequiredExpertise { get; set; }
}
