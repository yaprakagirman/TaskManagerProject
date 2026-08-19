using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskManager.Application.DTOs.Tags;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Mappings;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tests.Services;

public class TagExpertiseTests
{
    [Fact]
    public async Task CreateAsync_BackendRequirement_PersistsAndReturnsRequirement()
    {
        var repository = CreateRepository();
        var service = CreateService(repository);

        var response = await service.CreateAsync(new TagRequest
        {
            Name = "BE",
            RequiredExpertise = UserExpertise.Backend
        });

        Assert.Equal(UserExpertise.Backend, response.RequiredExpertise);
        repository.Verify(repository => repository.AddAsync(
            It.Is<Tag>(tag =>
                tag.RequiredExpertise == UserExpertise.Backend)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NoRequirement_PersistsNullRequirement()
    {
        var repository = CreateRepository();
        var service = CreateService(repository);

        var response = await service.CreateAsync(new TagRequest
        {
            Name = "urgent",
            RequiredExpertise = null
        });

        Assert.Null(response.RequiredExpertise);
        repository.Verify(repository => repository.AddAsync(
            It.Is<Tag>(tag => tag.RequiredExpertise == null)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CanChangeAndRemoveRequiredExpertise()
    {
        var tag = new Tag { Id = 3, Name = "tag" };
        var repository = CreateRepository();
        repository
            .Setup(current => current.GetByIdAsync(3))
            .ReturnsAsync(tag);
        var service = CreateService(repository);

        await service.UpdateAsync(3, new TagRequest
        {
            Name = "tag",
            RequiredExpertise = UserExpertise.QA
        });
        Assert.Equal(UserExpertise.QA, tag.RequiredExpertise);

        await service.UpdateAsync(3, new TagRequest
        {
            Name = "tag",
            RequiredExpertise = null
        });
        Assert.Null(tag.RequiredExpertise);
    }

    private static Mock<IRepository<Tag>> CreateRepository()
    {
        var repository = new Mock<IRepository<Tag>>();
        repository
            .Setup(current => current.FindAsync(
                It.IsAny<Expression<Func<Tag, bool>>>() ))
            .ReturnsAsync([]);
        repository
            .Setup(current => current.AddAsync(It.IsAny<Tag>()))
            .Callback<Tag>(tag => tag.Id = 1)
            .Returns(Task.CompletedTask);

        return repository;
    }

    private static TagService CreateService(Mock<IRepository<Tag>> repository)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(current => current.SaveChangesAsync())
            .ReturnsAsync(1);
        var mapperConfiguration = new MapperConfiguration(
            configuration => configuration.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return new TagService(
            repository.Object,
            unitOfWork.Object,
            mapperConfiguration.CreateMapper(),
            NullLogger<TagService>.Instance);
    }
}
