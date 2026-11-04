using System.Security.Cryptography;
using Vulindlela.Booking.Api.Data;
using Vulindlela.Booking.Api.Data.Entities;

namespace Vulindlela.Booking.Api.Business;

public interface IAuthService
{
    Task<Citizen?> VerifyAsync(string username, string password, CancellationToken ct);
    (string Hash, string Salt) CreateHash(string password);
}

public class AuthService : IAuthService
{
    private const int Iterations = 100_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private readonly ICitizenRepository _citizens;

    public AuthService(ICitizenRepository citizens) => _citizens = citizens;

    public async Task<Citizen?> VerifyAsync(string username, string password, CancellationToken ct)
    {
        var citizen = await _citizens.ByUsernameAsync(username, ct);
        if (citizen is null) return null;
        var salt = Convert.FromBase64String(citizen.PasswordSalt);
        var expected = Convert.FromBase64String(citizen.PasswordHash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return CryptographicOperations.FixedTimeEquals(expected, actual) ? citizen : null;
    }

    public (string Hash, string Salt) CreateHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }
}