using Application.Dtos;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(string email, string password, string deviceId);
    Task<AuthResultDto> RegisterAsync(string email, string password, string fullName);
    Task<AuthResultDto> RefreshTokenAsync(string clientRefreshToken);
    Task RevokeAllUserTokensAsync(Guid userId);
    Task RevokeSingleTokenAsync(string clientRefreshToken, Guid userId);
}