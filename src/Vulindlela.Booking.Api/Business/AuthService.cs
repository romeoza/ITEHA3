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
    private readonly ICitizenRepository _citizens;
    public AuthService(ICitizenRepository citizens) => _citizens = citizens;

    public async Task<Citizen?> VerifyAsync(string username, string password, CancellationToken ct)
    {
        var citizen = await _citizens.ByUsernameAsync(username, ct);
        if (citizen is null) return null;
        return PasswordHasher.Verify(password, citizen.PasswordHash, citizen.PasswordSalt) ? citizen : null;
    }

    public (string Hash, string Salt) CreateHash(string password) => PasswordHasher.Create(password);
}