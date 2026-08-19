using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.Services;
using TaskManager.Infrastructure.Settings;

namespace TaskManager.Application.Tests.Services;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateToken_IncludesUserIdInNameIdentifierClaim()
    {
        var settings = Options.Create(new JwtSettings
        {
            Key = "test-only-key-with-at-least-32-characters",
            Issuer = "TaskManager.Tests",
            Audience = "TaskManager.Tests",
            ExpirationMinutes = 15
        });
        var service = new JwtTokenService(settings);
        var user = new User
        {
            Id = 42,
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.test",
            Role = UserRole.Admin
        };

        var token = service.GenerateToken(user);
        var parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(parsedToken.Claims, claim =>
            claim.Type == ClaimTypes.NameIdentifier &&
            claim.Value == "42");
    }
}
