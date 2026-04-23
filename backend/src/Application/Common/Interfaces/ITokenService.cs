namespace Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, bool isSuperAdmin = false);
    string GenerateRefreshToken();
}
