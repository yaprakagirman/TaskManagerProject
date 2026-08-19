using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users { get; set; }

    public DbSet<TaskItem> Tasks { get; set; }

    public DbSet<Project> Projects { get; set; }

    public DbSet<TaskAssignment> TaskAssignments { get; set; }

    public DbSet<Tag> Tags { get; set; }

    public DbSet<TaskTag> TaskTags { get; set; }

    private void UpdateAuditFields()
    {
        var currentDate = DateTime.UtcNow;
        var currentUserId = _currentUserService?.UserId;

        foreach (var entry in ChangeTracker
                     .Entries<FullAuditedEntityBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedDate == default)
                    {
                        entry.Entity.CreatedDate = currentDate;
                    }

                    entry.Entity.CreatorId ??= currentUserId;
                    break;

                case EntityState.Modified:
                    PreserveCreationAudit(entry);
                    entry.Entity.LastModifiedDate = currentDate;

                    if (currentUserId.HasValue)
                    {
                        entry.Entity.LastModifierId = currentUserId;
                    }

                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    PreserveCreationAudit(entry);
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedDate = currentDate;
                    entry.Entity.DeleterId = currentUserId;
                    break;
            }
        }
    }

    private static void PreserveCreationAudit(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<FullAuditedEntityBase> entry)
    {
        entry.Property(entity => entity.CreatedDate).IsModified = false;
        entry.Property(entity => entity.CreatorId).IsModified = false;
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();

        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();

        return await base.SaveChangesAsync(
            cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
