namespace TaskManager.Application.DTOs.Tags;

public class TagResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public TaskManager.Domain.Enums.UserExpertise? RequiredExpertise { get; set; }
}
