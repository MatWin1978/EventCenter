using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace EventCenter.Tests.Helpers;

public class TestAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _user;

    public TestAuthenticationStateProvider(ClaimsPrincipal user)
    {
        _user = user;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_user));
    }

    public static TestAuthenticationStateProvider CreateUnauthenticated()
    {
        return new TestAuthenticationStateProvider(new ClaimsPrincipal());
    }

    public static TestAuthenticationStateProvider CreateAdmin(string username = "admin@test.com")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var user = new ClaimsPrincipal(identity);
        return new TestAuthenticationStateProvider(user);
    }

    public static TestAuthenticationStateProvider CreateMakler(string username = "makler@test.com")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Makler")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var user = new ClaimsPrincipal(identity);
        return new TestAuthenticationStateProvider(user);
    }
}
