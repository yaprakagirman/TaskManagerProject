using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Mappings;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Repositories;

namespace TaskManager.Application.Tests.Services;

public class TagDeletionIntegrationTests
{
    [Fact]
    public async Task TagReturnedByGet_CanBeSoftDeletedWithTagId()
    {
        await using var context = CreateContext(42);
        var repository = new Repository<Tag>(context);
        var service = new TagService(
            repository,
            new UnitOfWork(context),
            CreateMapper(),
            NullLogger<TagService>.Instance);

        var created = await service.CreateAsync(new() { Name = " Backend " });
        var listed = await service.GetAllAsync();
        Assert.Contains(listed, tag => tag.Id == created.Id);

        await service.DeleteAsync(created.Id);

        Assert.Empty(await service.GetAllAsync());
        var deletedRow = await context.Tags
            .IgnoreQueryFilters()
            .SingleAsync(tag => tag.Id == created.Id);
        Assert.True(deletedRow.IsDeleted);
        Assert.NotNull(deletedRow.DeletedDate);
        Assert.Equal(42, deletedRow.DeleterId);
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteAsync(created.Id));
    }

    [Fact]
    public async Task RemoveTaskTag_WithTaskTagId_PhysicallyDeletesRelationship()
    {
        await using var context = CreateContext(42);
        var taskTag = new TaskTag { Id = 20, TaskItemId = 3, TagId = 10 };
        context.TaskTags.Add(taskTag);
        await context.SaveChangesAsync();
        var service = new TaskTagService(
            Mock.Of<IRepository<TaskItem>>(),
            Mock.Of<IRepository<Tag>>(),
            new Repository<TaskTag>(context),
            new UnitOfWork(context),
            NullLogger<TaskTagService>.Instance);

        await service.RemoveAsync(taskTag.Id);

        Assert.Null(await context.TaskTags
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(candidate => candidate.Id == taskTag.Id));
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(
            config => config.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return configuration.CreateMapper();
    }

    private static AppDbContext CreateContext(int userId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(service => service.UserId).Returns(userId);
        currentUserService.Setup(service => service.IsAuthenticated).Returns(true);

        return new AppDbContext(options, currentUserService.Object);
    }
}
