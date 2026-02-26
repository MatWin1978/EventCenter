using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Security.Claims;

namespace EventCenter.Web.Infrastructure.Authentication;

public class IdentityRevalidatingAuthStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IdentityRevalidatingAuthStateProvider> _logger;

    public IdentityRevalidatingAuthStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<IdentityRevalidatingAuthStateProvider> logger)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        var user = authenticationState.User;

        if (user?.Identity?.IsAuthenticated != true)
            return false;

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User authenticated but NameIdentifier claim missing");
            return false;
        }

        // In Keycloak scenario with token-based auth, the token expiration
        // handles most validation. Return true unless implementing custom
        // user state tracking (e.g., checking if user still exists in DB).
        return true;
    }
}
