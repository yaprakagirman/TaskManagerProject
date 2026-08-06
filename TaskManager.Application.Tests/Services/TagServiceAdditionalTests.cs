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

public class TagServiceAdditionalTests
{
    [Fact]
    public async Task GetByIdAsync_TagDoesNotExist_ThrowsNotFoundException()
    {
        var repository = new Mock<IRepository<Tag>>();
        repository.Setup(currentRepository => currentRepository.GetByIdAsync(3)).ReturnsAsync((Tag?)null);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(3));
    }

    [Fact]
    public async Task UpdateAsync_NameBelongsToAnotherTag_ThrowsConflictExceptionWithoutSaving()
    {
        var repository = new Mock<IRepository<Tag>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(currentRepository => currentRepository.GetByIdAsync(3))
            .ReturnsAsync(new Tag { Id = 3, Name = "old" });
        repository
            .Setup(currentRepository => currentRepository.FindAsync(It.IsAny<Expression<Func<Tag, bool>>>() ))
            .ReturnsAsync([new Tag { Id = 4, Name = "backend" }]);
        var service = CreateService(repository, unitOfWork);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(3, new TagRequest { Name = " BACKEND " }));

        repository.Verify(currentRepository => currentRepository.Update(It.IsAny<Tag>()), Times.Never);
        unitOfWork.Verify(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(), Times.Never);
    }

    private static TagService CreateService(
        Mock<IRepository<Tag>> repository,
        Mock<IUnitOfWork>? unitOfWork = null)
    {
        return new TagService(
            repository.Object,
            (unitOfWork ?? new Mock<IUnitOfWork>()).Object,
            Mock.Of<IMapper>(),
            Mock.Of<ILogger<TagService>>());
    }
}
