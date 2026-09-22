namespace OrderProcessing.Application.Abstractions.Identity;

public interface IIdentityProvider
{
    Task<IdentityTokens> LoginAsync(string username, string password, CancellationToken cancellationToken);

    Task<IdentityTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
}

public sealed record IdentityTokens(string AccessToken, string RefreshToken, int ExpiresIn);
