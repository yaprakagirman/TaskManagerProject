using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Auth;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ThrowsConflictException()
    {
        // Arrange: AuthService bağımlılıklarını taklit ediyoruz.
        var userRepository = new Mock<IRepository<User>>();
        var passwordHasherService = new Mock<IPasswordHasherService>();
        var jwtTokenService = new Mock<IJwtTokenService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<AuthService>>();

        var existingUser = new User
        {
            Id = 10,
            FirstName = "Mehmet",
            LastName = "Yilmaz",
            Email = "mehmet@example.com",
            PasswordHash = "existing-hash"
        };

        userRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User>
            {
                existingUser
            });

        var authService = new AuthService(
            userRepository.Object,
            passwordHasherService.Object,
            jwtTokenService.Object,
            unitOfWork.Object,
            logger.Object);

        var request = new RegisterRequest
        {
            FirstName = "Mehmet",
            LastName = "Yilmaz",
            Email = "mehmet@example.com",
            Password = "123456"
        };

        // Act: Aynı email ile kayıt olmayı deniyoruz.
        var exception =
            await Assert.ThrowsAsync<ConflictException>(
                () => authService.RegisterAsync(request));

        // Assert: Doğru hatanın oluştuğunu kontrol ediyoruz.
        Assert.Equal(
            "This email address is already used.",
            exception.Message);

        // Hatalı durumda kullanıcı kaydedilmemeli.
        userRepository.Verify(
            repository => repository.AddAsync(It.IsAny<User>()),
            Times.Never);

        unitOfWork.Verify(
            currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(),
            Times.Never);

        // Parola hash'leme ve token üretme de yapılmamalı.
        passwordHasherService.Verify(
            service => service.HashPassword(It.IsAny<string>()),
            Times.Never);

        jwtTokenService.Verify(
            service => service.GenerateToken(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_EmailIsAvailable_ReturnsAuthResponse()
    {
        // Arrange
        var userRepository = new Mock<IRepository<User>>();
        var passwordHasherService = new Mock<IPasswordHasherService>();
        var jwtTokenService = new Mock<IJwtTokenService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<AuthService>>();

        // Email daha önce kullanılmamış.
        userRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User>());

        // Parola hash'lendiğinde test değeri dönüyoruz.
        passwordHasherService
            .Setup(service => service.HashPassword("123456"))
            .Returns("hashed-password");

        User? addedUser = null;

        // Repository'ye gönderilen kullanıcıyı yakalıyoruz.
        userRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<User>()))
            .Callback<User>(user =>
            {
                addedUser = user;

                // Gerçek database'in oluşturacağı ID'yi taklit ediyoruz.
                user.Id = 25;
            })
            .Returns(Task.CompletedTask);

        unitOfWork
            .Setup(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);

        // Gerçek JWT yerine sabit test token'ı dönüyoruz.
        jwtTokenService
            .Setup(service => service.GenerateToken(
                It.IsAny<User>()))
            .Returns("test-jwt-token");

        var authService = new AuthService(
            userRepository.Object,
            passwordHasherService.Object,
            jwtTokenService.Object,
            unitOfWork.Object,
            logger.Object);

        var request = new RegisterRequest
        {
            FirstName = "Ayse",
            LastName = "Yilmaz",
            Email = "ayse@example.com",
            Password = "123456"
        };

        // Act
        var response = await authService.RegisterAsync(request);

        // Assert: Dönen cevabı kontrol ediyoruz.
        Assert.Equal(25, response.UserId);
        Assert.Equal("Ayse", response.FirstName);
        Assert.Equal("Yilmaz", response.LastName);
        Assert.Equal("ayse@example.com", response.Email);
        Assert.Equal("test-jwt-token", response.Token);

        // Repository'ye gönderilen kullanıcıyı kontrol ediyoruz.
        Assert.NotNull(addedUser);
        Assert.Equal("hashed-password", addedUser.PasswordHash);

        // Gerekli işlemler birer kez yapılmış olmalı.
        passwordHasherService.Verify(
            service => service.HashPassword("123456"),
            Times.Once);

        userRepository.Verify(
            repository => repository.AddAsync(It.IsAny<User>()),
            Times.Once);

        unitOfWork.Verify(
            currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(),
            Times.Once);

        jwtTokenService.Verify(
            service => service.GenerateToken(It.IsAny<User>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UserDoesNotExist_ThrowsUnauthorizedException()
    {
        // Arrange
        var userRepository = new Mock<IRepository<User>>();
        var passwordHasherService = new Mock<IPasswordHasherService>();
        var jwtTokenService = new Mock<IJwtTokenService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<AuthService>>();

        // Repository'de bu email ile kullanıcı bulunmuyor.
        userRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User>());

        var authService = new AuthService(
            userRepository.Object,
            passwordHasherService.Object,
            jwtTokenService.Object,
            unitOfWork.Object,
            logger.Object);

        var request = new LoginRequest
        {
            Email = "unknown@example.com",
            Password = "123456"
        };

        // Act
        var exception =
            await Assert.ThrowsAsync<UnauthorizedException>(
                () => authService.LoginAsync(request));

        // Assert
        Assert.Equal(
            "Email or password is incorrect.",
            exception.Message);

        // Kullanıcı bulunamadığı için parola kontrol edilmemeli.
        passwordHasherService.Verify(
            service => service.VerifyPassword(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        // Kullanıcı bulunamadığı için JWT üretilmemeli.
        jwtTokenService.Verify(
            service => service.GenerateToken(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_PasswordIsInvalid_ThrowsUnauthorizedException()
    {
        // Arrange
        var userRepository = new Mock<IRepository<User>>();
        var passwordHasherService = new Mock<IPasswordHasherService>();
        var jwtTokenService = new Mock<IJwtTokenService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<AuthService>>();

        var existingUser = new User
        {
            Id = 15,
            FirstName = "Ayse",
            LastName = "Yilmaz",
            Email = "ayse@example.com",
            PasswordHash = "stored-password-hash"
        };

        // Kullanıcı repository'de bulunuyor.
        userRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User>
            {
            existingUser
            });

        // Girilen parola, kayıtlı hash ile eşleşmiyor.
        passwordHasherService
            .Setup(service => service.VerifyPassword(
                "wrong-password",
                "stored-password-hash"))
            .Returns(false);

        var authService = new AuthService(
            userRepository.Object,
            passwordHasherService.Object,
            jwtTokenService.Object,
            unitOfWork.Object,
            logger.Object);

        var request = new LoginRequest
        {
            Email = "ayse@example.com",
            Password = "wrong-password"
        };

        // Act
        var exception =
            await Assert.ThrowsAsync<UnauthorizedException>(
                () => authService.LoginAsync(request));

        // Assert
        Assert.Equal(
            "Email or password is incorrect.",
            exception.Message);

        // Kullanıcı bulunduğu için parola bir kez kontrol edilmeli.
        passwordHasherService.Verify(
            service => service.VerifyPassword(
                "wrong-password",
                "stored-password-hash"),
            Times.Once);

        // Parola yanlış olduğu için JWT üretilmemeli.
        jwtTokenService.Verify(
            service => service.GenerateToken(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_CredentialsAreValid_ReturnsAuthResponse()
    {
        // Arrange
        var userRepository = new Mock<IRepository<User>>();
        var passwordHasherService = new Mock<IPasswordHasherService>();
        var jwtTokenService = new Mock<IJwtTokenService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<AuthService>>();

        var existingUser = new User
        {
            Id = 15,
            FirstName = "Ayse",
            LastName = "Yilmaz",
            Email = "ayse@example.com",
            PasswordHash = "stored-password-hash"
        };

        // Kullanıcı repository'de bulunuyor.
        userRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User>
            {
            existingUser
            });

        // Girilen parola doğru.
        passwordHasherService
            .Setup(service => service.VerifyPassword(
                "correct-password",
                "stored-password-hash"))
            .Returns(true);

        // Başarılı login sonrasında üretilecek test token'ı.
        jwtTokenService
            .Setup(service => service.GenerateToken(existingUser))
            .Returns("test-jwt-token");

        var authService = new AuthService(
            userRepository.Object,
            passwordHasherService.Object,
            jwtTokenService.Object,
            unitOfWork.Object,
            logger.Object);

        var request = new LoginRequest
        {
            Email = "ayse@example.com",
            Password = "correct-password"
        };

        // Act
        var response = await authService.LoginAsync(request);

        // Assert: Doğru kullanıcı bilgileri dönmeli.
        Assert.Equal(15, response.UserId);
        Assert.Equal("Ayse", response.FirstName);
        Assert.Equal("Yilmaz", response.LastName);
        Assert.Equal("ayse@example.com", response.Email);
        Assert.Equal("test-jwt-token", response.Token);

        // Parola tam bir kez doğrulanmalı.
        passwordHasherService.Verify(
            service => service.VerifyPassword(
                "correct-password",
                "stored-password-hash"),
            Times.Once);

        // JWT tam bir kez ve doğru kullanıcı için üretilmeli.
        jwtTokenService.Verify(
            service => service.GenerateToken(existingUser),
            Times.Once);

        // Login işlemi kullanıcı kaydı oluşturmamalı.
        userRepository.Verify(
            repository => repository.AddAsync(It.IsAny<User>()),
            Times.Never);

        unitOfWork.Verify(
            currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(),
            Times.Never);
    }
}
