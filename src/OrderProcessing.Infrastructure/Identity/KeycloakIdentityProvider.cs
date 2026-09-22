using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OrderProcessing.Application.Abstractions.Identity;
using OrderProcessing.Application.Exceptions;

namespace OrderProcessing.Infrastructure.Identity;

public sealed class KeycloakIdentityProvider : IIdentityProvider
{
    private readonly HttpClient _http;
    private readonly KeycloakOptions _options;

    public KeycloakIdentityProvider(HttpClient http, IOptions<KeycloakOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public Task<IdentityTokens> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        var form = BaseForm();
        form.Add(new KeyValuePair<string, string>("grant_type", "password"));
        form.Add(new KeyValuePair<string, string>("username", username));
        form.Add(new KeyValuePair<string, string>("password", password));
        return RequestTokensAsync(form, cancellationToken);
    }

    public Task<IdentityTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var form = BaseForm();
        form.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));
        form.Add(new KeyValuePair<string, string>("refresh_token", refreshToken));
        return RequestTokensAsync(form, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var form = BaseForm();
        form.Add(new KeyValuePair<string, string>("refresh_token", refreshToken));

        using var response = await _http.PostAsync(
            "protocol/openid-connect/logout",
            new FormUrlEncodedContent(form),
            cancellationToken);

        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
        {
            throw new UnauthorizedAppException("Não foi possível encerrar a sessão.");
        }
    }

    private List<KeyValuePair<string, string>> BaseForm()
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("client_id", _options.ClientId)
        };

        if (!string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            form.Add(new KeyValuePair<string, string>("client_secret", _options.ClientSecret));
        }

        return form;
    }

    private async Task<IdentityTokens> RequestTokensAsync(
        List<KeyValuePair<string, string>> form,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new UnauthorizedAppException("Usuário ou senha inválidos.");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var accessToken = root.GetProperty("access_token").GetString();
        var refreshToken = root.GetProperty("refresh_token").GetString();
        var expiresIn = root.TryGetProperty("expires_in", out var expires)
            ? expires.GetInt32()
            : 300;

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAppException("Resposta de autenticação incompleta.");
        }

        return new IdentityTokens(accessToken, refreshToken, expiresIn);
    }
}
