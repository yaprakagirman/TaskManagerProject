using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tests.Services;

public class ProjectServiceTests
{
    [Fact]
    public async Task DeleteAsync_ProjectHasTasks_ThrowsConflictException()
    {
        // Arrange
        var projectRepository =
            new Mock<IRepository<Project>>();

        var taskRepository =
            new Mock<IRepository<TaskItem>>();

        var unitOfWork = new Mock<IUnitOfWork>();

        var mapper = new Mock<IMapper>();
        var logger = new Mock<ILogger<ProjectService>>();

        var project = new Project
        {
            Id = 5,
            Name = "TaskManager"
        };

        var projectTask = new TaskItem
        {
            Id = 10,
            Title = "Unit test yaz",
            ProjectId = 5,
            CreatedByUserId = 1
        };

        // Silinmek istenen proje repository'de bulunuyor.
        projectRepository
            .Setup(repository => repository.GetByIdAsync(5))
            .ReturnsAsync(project);

        // Projeye bağlı en az bir görev bulunuyor.
        taskRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<TaskItem, bool>>>()))
            .ReturnsAsync(new List<TaskItem>
            {
                projectTask
            });

        var projectService = new ProjectService(
            projectRepository.Object,
            taskRepository.Object,
            unitOfWork.Object,
            mapper.Object,
            logger.Object);

        // Act
        var exception =
            await Assert.ThrowsAsync<ConflictException>(
                () => projectService.DeleteAsync(5));

        // Assert
        Assert.Equal(
            "Project cannot be deleted because it has assigned tasks.",
            exception.Message);

        // İş kuralı nedeniyle proje silinmemeli.
        projectRepository.Verify(
            repository => repository.Delete(It.IsAny<Project>()),
            Times.Never);

        // Değişiklik kaydedilmemeli.
        unitOfWork.Verify(
            currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ProjectHasNoTasks_DeletesProject()
    {
        // Arrange
        var projectRepository =
            new Mock<IRepository<Project>>();

        var taskRepository =
            new Mock<IRepository<TaskItem>>();

        var unitOfWork = new Mock<IUnitOfWork>();

        var mapper = new Mock<IMapper>();
        var logger = new Mock<ILogger<ProjectService>>();

        var project = new Project
        {
            Id = 5,
            Name = "TaskManager"
        };

        // Silinmek istenen proje bulunuyor.
        projectRepository
            .Setup(repository => repository.GetByIdAsync(5))
            .ReturnsAsync(project);

        // Projeye bağlı görev bulunmuyor.
        taskRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<TaskItem, bool>>>()))
            .ReturnsAsync(new List<TaskItem>());

        unitOfWork
            .Setup(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);

        var projectService = new ProjectService(
            projectRepository.Object,
            taskRepository.Object,
            unitOfWork.Object,
            mapper.Object,
            logger.Object);

        // Act
        await projectService.DeleteAsync(5);

        // Assert
        projectRepository.Verify(
            repository => repository.Delete(project),
            Times.Once);

        unitOfWork.Verify(
            currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ProjectDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var projectRepository =
            new Mock<IRepository<Project>>();

        var taskRepository =
            new Mock<IRepository<TaskItem>>();

        var unitOfWork = new Mock<IUnitOfWork>();

        var mapper = new Mock<IMapper>();
        var logger = new Mock<ILogger<ProjectService>>();

        // ID 99 olan proje bulunmuyor.
        projectRepository
            .Setup(repository => repository.GetByIdAsync(99))
            .ReturnsAsync((Project?)null);

        var projectService = new ProjectService(
            projectRepository.Object,
            taskRepository.Object,
            unitOfWork.Object,
            mapper.Object,
            logger.Object);

        // Act
        var exception =
            await Assert.ThrowsAsync<NotFoundException>(
                () => projectService.DeleteAsync(99));

        // Assert
        Assert.Equal(
            "Project not found.",
            exception.Message);

        // Project bulunamadığı için task sorgusu yapılmamalı.
        taskRepository.Verify(
            repository => repository.FindAsync(
                It.IsAny<Expression<Func<TaskItem, bool>>>()),
            Times.Never);

        // Silme ve kaydetme işlemleri yapılmamalı.
        projectRepository.Verify(
            repository => repository.Delete(It.IsAny<Project>()),
            Times.Never);

        unitOfWork.Verify(
            currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(),
            Times.Never);
    }
}
