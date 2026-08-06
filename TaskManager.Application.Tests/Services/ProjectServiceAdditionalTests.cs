using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.DTOs.Projects;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tests.Services;

public class ProjectServiceAdditionalTests
{
    [Fact]
    public async Task CreateAsync_ValidRequest_AddsProjectAndSaves()
    {
        var projectRepository = new Mock<IRepository<Project>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var mapper = new Mock<IMapper>();
        var request = new CreateProjectRequest { Name = "New project", Description = "Description" };
        var project = new Project { Name = request.Name, Description = request.Description };
        mapper.Setup(currentMapper => currentMapper.Map<Project>(request)).Returns(project);
        mapper.Setup(currentMapper => currentMapper.Map<ProjectResponse>(project))
            .Returns(new ProjectResponse { Name = project.Name, Description = project.Description });
        unitOfWork.Setup(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync()).ReturnsAsync(1);
        var service = CreateService(projectRepository, unitOfWork, mapper);

        var result = await service.CreateAsync(request);

        Assert.Equal("New project", result.Name);
        Assert.NotEqual(default, project.CreatedDate);
        projectRepository.Verify(repository => repository.AddAsync(project), Times.Once);
        unitOfWork.Verify(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ProjectExists_MapsUpdatesAndSaves()
    {
        var projectRepository = new Mock<IRepository<Project>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var mapper = new Mock<IMapper>();
        var project = new Project { Id = 6, Name = "Old" };
        var request = new UpdateProjectRequest { Name = "Updated", Description = "New description" };
        projectRepository.Setup(repository => repository.GetByIdAsync(6)).ReturnsAsync(project);
        mapper.Setup(currentMapper => currentMapper.Map(request, project))
            .Callback(() => { project.Name = request.Name; project.Description = request.Description; })
            .Returns(project);
        unitOfWork.Setup(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync()).ReturnsAsync(1);
        var service = CreateService(projectRepository, unitOfWork, mapper);

        await service.UpdateAsync(6, request);

        Assert.Equal("Updated", project.Name);
        projectRepository.Verify(repository => repository.Update(project), Times.Once);
        unitOfWork.Verify(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(), Times.Once);
    }

    private static ProjectService CreateService(
        Mock<IRepository<Project>> projectRepository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<IMapper> mapper)
    {
        return new ProjectService(
            projectRepository.Object,
            Mock.Of<IRepository<TaskItem>>(),
            unitOfWork.Object,
            mapper.Object,
            Mock.Of<ILogger<ProjectService>>());
    }
}
