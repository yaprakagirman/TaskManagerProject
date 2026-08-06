namespace TaskManager.Domain.Common;

public abstract class FullAuditedEntityBase : BaseEntity
{
    public int? CreatorId { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public int? LastModifierId { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedDate { get; set; }

    public int? DeleterId { get; set; }
}