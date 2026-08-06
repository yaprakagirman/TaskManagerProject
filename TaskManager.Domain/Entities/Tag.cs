using TaskManager.Domain.Common;

namespace TaskManager.Domain.Entities;

public class Tag : FullAuditedEntityBase
{
    public string Name { get; set; } = string.Empty;

    public ICollection<TaskTag> TaskTags { get; set; }
        = new List<TaskTag>();
} 