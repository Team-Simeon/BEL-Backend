using System.Net;

namespace BEL.Api.Tests;

public sealed class CorsTests(BelApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Preflight_FromAllowedOrigin_IsAllowed()
    {
        var response = await SendPreflightAsync(BelApiFactory.AllowedFrontendOrigin);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Equal(BelApiFactory.AllowedFrontendOrigin, Assert.Single(origins));
        Assert.True(response.Headers.Contains("Access-Control-Allow-Headers"));
    }

    [Fact]
    public async Task Preflight_FromUnknownOrigin_IsNotAllowed()
    {
        var response = await SendPreflightAsync("http://evil.example");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Request_FromAllowedOrigin_GetsCorsHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", BelApiFactory.AllowedFrontendOrigin);

        var response = await CreateClient().SendAsync(request);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Equal(BelApiFactory.AllowedFrontendOrigin, Assert.Single(origins));
    }

    private Task<HttpResponseMessage> SendPreflightAsync(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/feedback");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");
        return CreateClient().SendAsync(request);
    }
}
