using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Application.Auth;

namespace OrderProcessing.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.LoginAsync(
            new LoginCommand(request.Username ?? string.Empty, request.Password ?? string.Empty),
            cancellationToken);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.RefreshAsync(request.RefreshToken ?? string.Empty, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        await _auth.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new MeResponse(User.Identity?.Name ?? string.Empty));
    }
}

public sealed record LoginRequest(string? Username, string? Password);

public sealed record RefreshRequest(string? RefreshToken);

public sealed record MeResponse(string Username);
