using System.Net;
using VPUploader.Core.Tests.Testing;
using Xunit;

namespace VPUploader.Core.Tests;

public class AuthServiceTests
{
    
    [Fact]
    public async Task RefreshAccessTokenAsync_SendsBasicAuthAndFormBody()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
                                           { "access_token": "new-access-token", "refresh_token": "new-refresh-token", "expires_in": 3600 }
                                           """);

        var tokenStore = new InMemoryTokenStore();
        var auth = new AuthService("client-id-123", "client-secret-456", tokenStore, handler.ToHttpClient());

        await auth.RefreshAccessTokenAsync("old-refresh-token");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        Assert.Equal("Y2xpZW50LWlkLTEyMzpjbGllbnQtc2VjcmV0LTQ1Ng==", request.Headers.Authorization!.Parameter);

        string body = handler.RequestBodies[0];
        Assert.Contains("grant_type=refresh_token", body);
        Assert.Contains("refresh_token=old-refresh-token", body);
    }
    
    [Fact]
    public async Task RefreshAccessTokenAsync_SavesRotatedRefreshToken_WhenPinterestReturnsOne()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
                                           { "access_token": "new-access-token", "refresh_token": "rotated-refresh-token", "expires_in": 3600 }
                                           """);

        var tokenStore = new InMemoryTokenStore(seedRefreshToken: "old-refresh-token");
        var auth = new AuthService("id", "secret", tokenStore, handler.ToHttpClient());

        await auth.RefreshAccessTokenAsync("old-refresh-token");

        Assert.Equal("rotated-refresh-token", tokenStore.SavedRefreshToken);
    }
    
    [Fact]
    public async Task RefreshAccessTokenAsync_KeepsExistingRefreshToken_WhenPinterestDoesNotRotateIt()
    {
        var handler = new FakeHttpMessageHandler();
        // No refresh_token in the response - some grants don't rotate it.
        handler.Enqueue(HttpStatusCode.OK, """
                                           { "access_token": "new-access-token", "expires_in": 3600 }
                                           """);

        var tokenStore = new InMemoryTokenStore(seedRefreshToken: "old-refresh-token");
        var auth = new AuthService("id", "secret", tokenStore, handler.ToHttpClient());

        await auth.RefreshAccessTokenAsync("old-refresh-token");

        Assert.Equal("old-refresh-token", tokenStore.SavedRefreshToken);
    }
    
    [Fact]
    public async Task RefreshAccessTokenAsync_Throws_OnErrorResponse()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Unauthorized, """{ "message": "invalid_grant" }""");

        var auth = new AuthService("id", "secret", new InMemoryTokenStore(), handler.ToHttpClient());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            auth.RefreshAccessTokenAsync("expired-refresh-token"));

        Assert.Contains("invalid_grant", ex.Message);
    }
    
    [Fact]
    public async Task GetValidAccessTokenAsync_UsesSavedRefreshToken_WithoutOpeningBrowser()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
                                           { "access_token": "refreshed-token", "refresh_token": "still-here", "expires_in": 3600 }
                                           """);

        var tokenStore = new InMemoryTokenStore(seedRefreshToken: "saved-refresh-token");
        var auth = new AuthService("id", "secret", tokenStore, handler.ToHttpClient());

        string token = await auth.GetValidAccessTokenAsync();

        Assert.Equal("refreshed-token", token);
        Assert.Single(handler.Requests);
    }
    
    [Fact]
    public async Task GetValidAccessTokenAsync_CachesTokenAndSkipsSecondNetworkCall()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
                                           { "access_token": "refreshed-token", "refresh_token": "still-here", "expires_in": 3600 }
                                           """);

        var tokenStore = new InMemoryTokenStore(seedRefreshToken: "saved-refresh-token");
        var auth = new AuthService("id", "secret", tokenStore, handler.ToHttpClient());

        string first = await auth.GetValidAccessTokenAsync();
        string second = await auth.GetValidAccessTokenAsync();

        Assert.Equal(first, second);
        Assert.Single(handler.Requests);
    }
    
}