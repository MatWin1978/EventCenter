using EventCenter.Tests.Helpers;
using System.Security.Claims;
using Xunit;

namespace EventCenter.Tests;

public class AuthenticationTests
{
    [Fact]
    public void TestAuthenticationStateProvider_CreatesAdminUser()
    {
        var provider = TestAuthenticationStateProvider.CreateAdmin();
        var state = provider.GetAuthenticationStateAsync().Result;

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.True(state.User.IsInRole("Admin"));
    }

    [Fact]
    public void TestAuthenticationStateProvider_CreatesMaklerUser()
    {
        var provider = TestAuthenticationStateProvider.CreateMakler();
        var state = provider.GetAuthenticationStateAsync().Result;

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.True(state.User.IsInRole("Makler"));
    }

    [Fact]
    public void TestAuthenticationStateProvider_CreatesUnauthenticatedUser()
    {
        var provider = TestAuthenticationStateProvider.CreateUnauthenticated();
        var state = provider.GetAuthenticationStateAsync().Result;

        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
    }
}
