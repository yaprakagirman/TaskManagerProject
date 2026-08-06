using TaskManager.Application.DTOs.Auth;
using TaskManager.Application.Validators.Auth;

namespace TaskManager.Application.Tests.Validators;

public class RegisterRequestValidatorTests
{
    [Fact]
    public void Validate_PasswordIsShort_ReturnsPasswordValidationError()
    {
        // Arrange: Test için gerekli nesneleri hazırlıyoruz.
        var validator = new RegisterRequestValidator();

        var request = new RegisterRequest
        {
            FirstName = "Ayse",
            LastName = "Yilmaz",
            Email = "ayse@example.com",
            Password = "123"
        };

        // Act: Test etmek istediğimiz işlemi çalıştırıyoruz.
        var result = validator.Validate(request);

        // Assert: Beklenen sonucun oluştuğunu kontrol ediyoruz.
        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void Validate_RequestIsValid_ReturnsValidResult()
    {
        // Arrange
        var validator = new RegisterRequestValidator();

        var request = new RegisterRequest
        {
            FirstName = "Ayse",
            LastName = "Yilmaz",
            Email = "ayse@example.com",
            Password = "123456"
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}