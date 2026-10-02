using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Services.Security
{
    /// <summary>Hashes and verifies login passwords. Never store or compare plain-text passwords.</summary>
    public interface IPasswordHasherService
    {
        /// <summary>Produces a salted hash of <paramref name="password"/> suitable for storing in User.PasswordHash.</summary>
        string HashPassword(User user, string password);

        /// <summary>
        /// Checks a plain-text password against a stored hash. Returns true if it matches.
        /// <paramref name="rehashNeeded"/> is true when the match succeeded but the stored hash was
        /// produced with an older algorithm/parameters and should be re-saved via <see cref="HashPassword"/>.
        /// </summary>
        bool VerifyPassword(User user, string storedHash, string suppliedPassword, out bool rehashNeeded);
    }
}
