namespace TaskManager.Application.DTOs.Projects;

public class ProjectResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; }
}
