using TaskManager.Domain.Common;
namespace TaskManager.Domain.Entities;


public class TaskTag : BaseEntity
{
    public int TaskItemId { get; set; }

    public TaskItem TaskItem { get; set; } = null!;

    public int TagId { get; set; }

    public Tag Tag { get; set; } = null!;
}