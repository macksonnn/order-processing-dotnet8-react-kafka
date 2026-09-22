using System.Security.Claims;
using OrderProcessing.Application.Abstractions;

namespace OrderProcessing.Api.Authentication;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User
                ?? throw new UnauthorizedAccessException("There is no authenticated user.");

            return user.FindFirstValue("sub")
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("The JWT does not contain a 'sub' claim.");
        }
    }
}
