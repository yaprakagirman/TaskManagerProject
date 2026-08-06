using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Application.Tests.Persistence;

public class AppDbContextTests
{
    [Fact]
    public async Task SaveChangesAsync_AddedEntity_SetsCreatedDate()
    {
        await using var context = CreateContext();
        var project = new Project { Name = "Project", CreatedDate = DateTime.UnixEpoch };
        var before = DateTime.UtcNow;

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var after = DateTime.UtcNow;
        Assert.InRange(project.CreatedDate, before, after);
    }

    [Fact]
    public async Task SaveChangesAsync_ModifiedEntity_SetsLastModifiedDateAndPreservesCreatedDate()
    {
        await using var context = CreateContext();
        var project = new Project { Name = "Project" };
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        var originalCreatedDate = project.CreatedDate;
        var before = DateTime.UtcNow;

        project.Name = "Updated project";
        await context.SaveChangesAsync();

        var after = DateTime.UtcNow;
        Assert.NotNull(project.LastModifiedDate);
        Assert.InRange(project.LastModifiedDate.Value, before, after);
        Assert.Equal(originalCreatedDate, project.CreatedDate);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
