using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskManager.API.Services;

namespace TaskManager.Application.Tests.Services;

public class CurrentUserServiceTests
{
    [Fact]
    public void AuthenticatedPrincipal_ValidNameIdentifier_ReturnsUserId()
    {
        var service = CreateService("42");

        Assert.True(service.IsAuthenticated);
        Assert.Equal(42, service.UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-integer")]
    [InlineData("0")]
    [InlineData("-1")]
    public void AuthenticatedPrincipal_MissingOrInvalidNameIdentifier_ReturnsNull(
        string? claimValue)
    {
        var service = CreateService(claimValue);

        Assert.True(service.IsAuthenticated);
        Assert.Null(service.UserId);
    }

    [Fact]
    public void MissingHttpContext_IsHandledSafely()
    {
        var service = new CurrentUserService(new HttpContextAccessor());

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
    }

    [Fact]
    public void UnauthenticatedPrincipal_DoesNotExposeClaimValue()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "42")]))
        };
        var service = new CurrentUserService(new HttpContextAccessor
        {
            HttpContext = context
        });

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
    }

    private static CurrentUserService CreateService(string? claimValue)
    {
        Claim[] claims = claimValue is null
            ? []
            : [new Claim(ClaimTypes.NameIdentifier, claimValue)];
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };

        return new CurrentUserService(new HttpContextAccessor
        {
            HttpContext = context
        });
    }
}
