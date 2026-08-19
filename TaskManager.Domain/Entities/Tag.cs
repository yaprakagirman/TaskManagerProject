using TaskManager.Domain.Common;

namespace TaskManager.Domain.Entities;

public class Tag : FullAuditedEntityBase
{
    public string Name { get; set; } = string.Empty;

    public TaskManager.Domain.Enums.UserExpertise? RequiredExpertise { get; set; }

    public ICollection<TaskTag> TaskTags { get; set; }
        = new List<TaskTag>();
} 
