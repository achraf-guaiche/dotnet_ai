using Formations.Domain.Entities;

namespace Formations.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    string? GetUserIdFromExpiredToken(string token);
}
