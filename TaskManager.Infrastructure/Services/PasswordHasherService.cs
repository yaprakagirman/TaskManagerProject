using Microsoft.AspNetCore.Identity;
using TaskManager.Application.Interfaces;

namespace TaskManager.Infrastructure.Services;

public class PasswordHasherService : IPasswordHasherService
{
    private static readonly object PasswordHasherUser = new();

    private readonly PasswordHasher<object> _passwordHasher = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(PasswordHasherUser, password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            PasswordHasherUser,
            passwordHash,
            password);

        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}