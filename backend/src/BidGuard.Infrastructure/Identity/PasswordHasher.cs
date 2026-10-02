using BidGuard.Application.Common.Interfaces;

namespace BidGuard.Infrastructure.Identity;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password) => BCrypt.Net.BCrypt.EnhancedHashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
