using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Users;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IRepository<User>> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(
            _repository.Object,
            _unitOfWork.Object,
            _mapper.Object,
            Mock.Of<ILogger<UserService>>());
    }

    [Fact]
    public async Task GetUserByIdAsync_UserExists_ReturnsMappedUser()
    {
        var user = new User { Id = 4, FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" };
        var response = new UserResponse { Id = 4, FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" };
        _repository.Setup(repository => repository.GetByIdAsync(4)).ReturnsAsync(user);
        _mapper.Setup(mapper => mapper.Map<UserResponse>(user)).Returns(response);

        var result = await _service.GetUserByIdAsync(4);

        Assert.Same(response, result);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserDoesNotExist_ReturnsNull()
    {
        _repository.Setup(repository => repository.GetByIdAsync(4)).ReturnsAsync((User?)null);

        var result = await _service.GetUserByIdAsync(4);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_UserExists_UpdatesRoleAndSaves()
    {
        var user = new User { Id = 4, Role = UserRole.User };
        _repository.Setup(repository => repository.GetByIdAsync(4)).ReturnsAsync(user);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync()).ReturnsAsync(1);
        _mapper.Setup(mapper => mapper.Map<UserResponse>(user))
            .Returns(() => new UserResponse { Id = user.Id });

        var result = await _service.UpdateUserRoleAsync(
            4,
            new UpdateUserRoleRequest { Role = UserRole.Admin });

        Assert.Equal(UserRole.Admin, user.Role);
        Assert.Equal(4, result.Id);
        _repository.Verify(repository => repository.Update(user), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_UserDoesNotExist_ThrowsNotFoundExceptionWithoutSaving()
    {
        _repository.Setup(repository => repository.GetByIdAsync(4)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateUserRoleAsync(4, new UpdateUserRoleRequest { Role = UserRole.Admin }));

        _repository.Verify(repository => repository.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateUserExpertisesAsync_BackendAndQa_StoresCombinedFlags()
    {
        var user = new User { Id = 4 };
        _repository.Setup(repository => repository.GetByIdAsync(4)).ReturnsAsync(user);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync()).ReturnsAsync(1);
        _mapper.Setup(mapper => mapper.Map<UserResponse>(user))
            .Returns(() => new UserResponse
            {
                Id = user.Id,
                Expertises = user.Expertises
            });

        var result = await _service.UpdateUserExpertisesAsync(
            4,
            new UpdateUserExpertisesRequest
            {
                Expertises = [UserExpertise.Backend, UserExpertise.QA]
            });

        Assert.Equal(
            UserExpertise.Backend | UserExpertise.QA,
            user.Expertises);
        Assert.Equal(user.Expertises, result.Expertises);
        _repository.Verify(repository => repository.Update(user), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUserExpertisesAsync_EmptyList_StoresNone()
    {
        var user = new User
        {
            Id = 4,
            Expertises = UserExpertise.Backend
        };
        _repository.Setup(repository => repository.GetByIdAsync(4)).ReturnsAsync(user);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync()).ReturnsAsync(1);
        _mapper.Setup(mapper => mapper.Map<UserResponse>(user))
            .Returns(() => new UserResponse
            {
                Id = user.Id,
                Expertises = user.Expertises
            });

        var result = await _service.UpdateUserExpertisesAsync(
            4,
            new UpdateUserExpertisesRequest { Expertises = [] });

        Assert.Equal(UserExpertise.None, user.Expertises);
        Assert.Equal(UserExpertise.None, result.Expertises);
    }

    [Fact]
    public async Task UpdateUserExpertisesAsync_UserDoesNotExist_ThrowsNotFoundWithoutSaving()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(99))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateUserExpertisesAsync(
                99,
                new UpdateUserExpertisesRequest
                {
                    Expertises = [UserExpertise.Backend]
                }));

        _repository.Verify(repository => repository.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateUserExpertisesAsync_NonPositiveUserId_ThrowsBadRequest()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateUserExpertisesAsync(
                0,
                new UpdateUserExpertisesRequest { Expertises = [] }));

        _repository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<object[]>()),
            Times.Never);
    }
}
