using Microsoft.AspNetCore.Identity;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Services.Security
{
    /// <summary>
    /// Wraps ASP.NET Core's built-in <see cref="PasswordHasher{TUser}"/> (PBKDF2, salted, versioned).
    /// This type ships inside the ASP.NET Core shared framework, so no extra NuGet package is needed
    /// beyond the Microsoft.NET.Sdk.Web SDK this project already uses.
    /// </summary>
    public class PasswordHasherService : IPasswordHasherService
    {
        private readonly PasswordHasher<User> _hasher = new();

        public string HashPassword(User user, string password) => _hasher.HashPassword(user, password);

        public bool VerifyPassword(User user, string storedHash, string suppliedPassword, out bool rehashNeeded)
        {
            PasswordVerificationResult result = _hasher.VerifyHashedPassword(user, storedHash, suppliedPassword);
            rehashNeeded = result == PasswordVerificationResult.SuccessRehashNeeded;
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
