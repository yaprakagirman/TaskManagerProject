using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Tags;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tests.Services;

public class TagServiceTests
{
    [Fact]
    public async Task CreateAsync_NameContainsSpacesAndUpperCase_NormalizesName()
    {
        // Arrange
        var tagRepository =
            new Mock<IRepository<Tag>>();

        var unitOfWork = new Mock<IUnitOfWork>();

        var mapper = new Mock<IMapper>();
        var logger = new Mock<ILogger<TagService>>();

        var request = new TagRequest
        {
            Name = "  BACKEND  "
        };

        // Aynı isimde başka bir etiket bulunmuyor.
        tagRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Tag, bool>>>()))
            .ReturnsAsync(new List<Tag>());

        // Request'in Tag entity'sine çevrilmesini taklit ediyoruz.
        mapper
            .Setup(currentMapper => currentMapper.Map<Tag>(request))
            .Returns(new Tag());

        Tag? addedTag = null;

        // Repository'ye gönderilen etiketi yakalıyoruz.
        tagRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<Tag>()))
            .Callback<Tag>(tag =>
            {
                addedTag = tag;

                // Database tarafından oluşturulacak ID'yi taklit ediyoruz.
                tag.Id = 7;
            })
            .Returns(Task.CompletedTask);

        unitOfWork
            .Setup(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);

        // Entity'nin response'a çevrilmesini taklit ediyoruz.
        mapper
            .Setup(currentMapper => currentMapper.Map<TagResponse>(
                It.IsAny<Tag>()))
            .Returns((Tag tag) => new TagResponse
            {
                Id = tag.Id,
                Name = tag.Name
            });

        var tagService = new TagService(
            tagRepository.Object,
            unitOfWork.Object,
            mapper.Object,
            logger.Object);

        // Act
        var response = await tagService.CreateAsync(request);

        // Assert
        Assert.NotNull(addedTag);

        // Repository'ye normalize edilmiş isim gönderilmeli.
        Assert.Equal("backend", addedTag.Name);

        // API response'unda da normalize edilmiş isim bulunmalı.
        Assert.Equal(7, response.Id);
        Assert.Equal("backend", response.Name);

        tagRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Tag>()),
            Times.Once);

        unitOfWork.Verify(
            currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NameAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var tagRepository =
            new Mock<IRepository<Tag>>();

        var unitOfWork = new Mock<IUnitOfWork>();

        var mapper = new Mock<IMapper>();
        var logger = new Mock<ILogger<TagService>>();

        var existingTag = new Tag
        {
            Id = 3,
            Name = "backend"
        };

        // Repository aynı isimde bir etiket döndürüyor.
        tagRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Tag, bool>>>()))
            .ReturnsAsync(new List<Tag>
            {
            existingTag
            });

        var tagService = new TagService(
            tagRepository.Object,
            unitOfWork.Object,
            mapper.Object,
            logger.Object);

        var request = new TagRequest
        {
            Name = "backend"
        };

        // Act
        var exception =
            await Assert.ThrowsAsync<ConflictException>(
                () => tagService.CreateAsync(request));

        // Assert
        Assert.Equal(
            "A tag with this name already exists.",
            exception.Message);

        // Duplicate bulunduğu için mapping yapılmamalı.
        mapper.Verify(
            currentMapper => currentMapper.Map<Tag>(
                It.IsAny<TagRequest>()),
            Times.Never);

        // Yeni etiket eklenmemeli.
        tagRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Tag>()),
            Times.Never);

        // Database değişikliği kaydedilmemeli.
        unitOfWork.Verify(
            currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(),
            Times.Never);
    }
}
