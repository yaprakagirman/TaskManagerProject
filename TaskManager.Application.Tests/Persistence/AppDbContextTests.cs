using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Application.Tests.Persistence;

public class AppDbContextTests
{
    [Fact]
    public async Task SaveChangesAsync_AddedEntity_SetsCreationAuditFromCurrentUser()
    {
        await using var context = CreateContext(42);
        var project = new Project { Name = "Project" };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        Assert.Equal(DateTimeKind.Utc, project.CreatedDate.Kind);
        Assert.Equal(42, project.CreatorId);
    }

    [Fact]
    public async Task SaveChangesAsync_AddedEntity_PreservesExplicitCreationAudit()
    {
        await using var context = CreateContext(42);
        var project = new Project
        {
            Name = "Imported project",
            CreatedDate = DateTime.UnixEpoch,
            CreatorId = 7
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        Assert.Equal(DateTime.UnixEpoch, project.CreatedDate);
        Assert.Equal(7, project.CreatorId);
    }

    [Fact]
    public async Task SaveChangesAsync_ModifiedEntity_SetsModificationAuditAndPreservesCreationAudit()
    {
        await using var context = CreateContext(42);
        var project = new Project { Name = "Project" };
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        var originalCreatedDate = project.CreatedDate;
        var originalCreatorId = project.CreatorId;
        var before = DateTime.UtcNow;

        project.Name = "Updated project";
        await context.SaveChangesAsync();

        var after = DateTime.UtcNow;
        Assert.NotNull(project.LastModifiedDate);
        Assert.InRange(project.LastModifiedDate.Value, before, after);
        Assert.Equal(42, project.LastModifierId);
        Assert.Equal(originalCreatedDate, project.CreatedDate);
        Assert.Equal(originalCreatorId, project.CreatorId);
    }

    [Fact]
    public async Task SaveChangesAsync_DeletedAuditedEntity_SoftDeletesAndFiltersRow()
    {
        await using var context = CreateContext(42);
        var tag = new Tag { Name = "backend" };
        context.Tags.Add(tag);
        await context.SaveChangesAsync();
        var originalCreatedDate = tag.CreatedDate;

        context.Tags.Remove(tag);
        var before = DateTime.UtcNow;
        await context.SaveChangesAsync();
        var after = DateTime.UtcNow;

        Assert.True(tag.IsDeleted);
        Assert.NotNull(tag.DeletedDate);
        Assert.InRange(tag.DeletedDate.Value, before, after);
        Assert.Equal(42, tag.DeleterId);
        Assert.Equal(originalCreatedDate, tag.CreatedDate);
        Assert.Empty(await context.Tags.ToListAsync());

        var deletedRow = await context.Tags
            .IgnoreQueryFilters()
            .SingleAsync(candidate => candidate.Id == tag.Id);
        Assert.Same(tag, deletedRow);
    }

    [Fact]
    public async Task SaveChangesAsync_DeletedJoinEntity_PhysicallyRemovesRow()
    {
        await using var context = CreateContext(42);
        var taskTag = new TaskTag { Id = 20, TaskItemId = 3, TagId = 10 };
        context.TaskTags.Add(taskTag);
        await context.SaveChangesAsync();

        context.TaskTags.Remove(taskTag);
        await context.SaveChangesAsync();

        Assert.Null(await context.TaskTags
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(candidate => candidate.Id == taskTag.Id));
    }

    [Fact]
    public async Task SaveChangesAsync_UnauthenticatedCreate_DoesNotCrash()
    {
        await using var context = CreateContext();
        var user = new User
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.test"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        Assert.Null(user.CreatorId);
        Assert.NotEqual(default, user.CreatedDate);
    }

    [Fact]
    public void SaveChanges_SynchronousPath_AppliesAuditFields()
    {
        using var context = CreateContext(11);
        var project = new Project { Name = "Synchronous" };

        context.Projects.Add(project);
        context.SaveChanges();

        Assert.Equal(11, project.CreatorId);
    }

    [Fact]
    public void Model_AllAuditedAggregates_HaveGlobalQueryFilters()
    {
        using var context = CreateContext();
        Type[] auditedAggregateTypes =
        [
            typeof(User),
            typeof(Project),
            typeof(TaskItem),
            typeof(Tag)
        ];

        foreach (var entityType in auditedAggregateTypes)
        {
            Assert.NotEmpty(context.Model
                .FindEntityType(entityType)?
                .GetDeclaredQueryFilters() ?? []);
        }
    }

    private static AppDbContext CreateContext(int? userId = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(service => service.UserId).Returns(userId);
        currentUserService.Setup(service => service.IsAuthenticated).Returns(userId.HasValue);

        return new AppDbContext(options, currentUserService.Object);
    }
}
