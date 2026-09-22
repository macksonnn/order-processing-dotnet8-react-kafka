using System.Text;
using System.Text.Json;
using OrderProcessing.Application.Abstractions.Identity;
using OrderProcessing.Application.Exceptions;

namespace OrderProcessing.Application.Auth;

public sealed class AuthService
{
    private readonly IIdentityProvider _identity;

    public AuthService(IIdentityProvider identity)
    {
        _identity = identity;
    }

    public async Task<LoginResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrWhiteSpace(command.Password))
        {
            throw new ValidationAppException("login", "Informe usuário e senha.");
        }

        var tokens = await _identity.LoginAsync(command.Username.Trim(), command.Password, cancellationToken);
        return ToResult(tokens);
    }

    public async Task<LoginResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAppException("Sessão expirada.");
        }

        var tokens = await _identity.RefreshAsync(refreshToken, cancellationToken);
        return ToResult(tokens);
    }

    public Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Task.CompletedTask;
        }

        return _identity.LogoutAsync(refreshToken, cancellationToken);
    }

    private static LoginResult ToResult(IdentityTokens tokens)
    {
        return new LoginResult(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresIn,
            ReadUsername(tokens.AccessToken));
    }

    private static string ReadUsername(string accessToken)
    {
        try
        {
            var parts = accessToken.Split('.');
            if (parts.Length < 2)
            {
                return string.Empty;
            }

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2:
                    payload += "==";
                    break;
                case 3:
                    payload += "=";
                    break;
            }

            using var document = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            if (document.RootElement.TryGetProperty("preferred_username", out var username))
            {
                return username.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}

public sealed record LoginCommand(string Username, string Password);

public sealed record LoginResult(string AccessToken, string RefreshToken, int ExpiresIn, string Username);
