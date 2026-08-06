using TaskManager.Domain.Common;

namespace TaskManager.Domain.Entities;

public class Project : FullAuditedEntityBase
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<TaskItem> Tasks { get; set; }
        = new List<TaskItem>();
}
