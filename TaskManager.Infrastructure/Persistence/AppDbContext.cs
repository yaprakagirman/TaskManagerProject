using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
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

        foreach (var entry in ChangeTracker
                     .Entries<FullAuditedEntityBase>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = currentDate;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedDate = currentDate;
            }
        }
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
