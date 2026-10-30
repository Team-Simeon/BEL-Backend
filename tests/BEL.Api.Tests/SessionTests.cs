using System.Net;
using System.Net.Http.Headers;
using BEL.Api.Contracts.Auth;
using BEL.Api.Models;

namespace BEL.Api.Tests;

public sealed class SessionTests(BelApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = CreateClient();
        var email = UniqueEmail("student");
        Assert.Equal(HttpStatusCode.Created, (await RegisterStudentAsync(client, email)).StatusCode);

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/auth/login", new
        {
            email,
            password = "WrongPassword1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesOnlyTheCurrentToken()
    {
        var (email, password) = await RegisterStudentCredentialsAsync();
        var firstSession = await CreateAuthenticatedClientAsync(email, password);
        var secondSession = await CreateAuthenticatedClientAsync(email, password);

        var logoutResponse = await firstSession.PostAsync("/api/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await firstSession.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await secondSession.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        var response = await CreateClient().PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LogoutEverywhere_RevokesAllTokensButAllowsNewLogin()
    {
        var (email, password) = await RegisterStudentCredentialsAsync();
        var firstSession = await CreateAuthenticatedClientAsync(email, password);
        var secondSession = await CreateAuthenticatedClientAsync(email, password);

        var logoutResponse = await firstSession.PostAsync("/api/auth/logout-all", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await firstSession.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await secondSession.GetAsync("/api/auth/me")).StatusCode);

        var newSession = await CreateAuthenticatedClientAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, (await newSession.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task DeactivatedUser_ExistingTokenIsRejected_AndStaysRejectedAfterReactivation()
    {
        var adminClient = await CreateAdminClientAsync();
        var (email, password) = await RegisterStudentCredentialsAsync();
        var studentClient = await CreateAuthenticatedClientAsync(email, password);
        var studentId = await GetCurrentUserIdAsync(studentClient);

        await AdminUpdateAsync(adminClient, studentId, UserRole.Student, isActive: false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await studentClient.GetAsync("/api/auth/me")).StatusCode);

        await AdminUpdateAsync(adminClient, studentId, UserRole.Student, isActive: true);
        Assert.Equal(HttpStatusCode.Unauthorized, (await studentClient.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task DemotedAdmin_ExistingTokenLosesAdminAccess()
    {
        var adminClient = await CreateAdminClientAsync();
        var (email, password) = await RegisterStudentCredentialsAsync();
        var userClient = await CreateAuthenticatedClientAsync(email, password);
        var userId = await GetCurrentUserIdAsync(userClient);

        await AdminUpdateAsync(adminClient, userId, UserRole.Admin, isActive: true);
        var promotedClient = await CreateAuthenticatedClientAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, (await promotedClient.GetAsync("/api/users")).StatusCode);

        await AdminUpdateAsync(adminClient, userId, UserRole.Student, isActive: true);
        Assert.Equal(HttpStatusCode.Unauthorized, (await promotedClient.GetAsync("/api/users")).StatusCode);
    }

    [Fact]
    public async Task DeletedUser_ExistingTokenIsRejected()
    {
        var adminClient = await CreateAdminClientAsync();
        var (email, password) = await RegisterStudentCredentialsAsync();
        var studentClient = await CreateAuthenticatedClientAsync(email, password);
        var studentId = await GetCurrentUserIdAsync(studentClient);

        var deleteResponse = await adminClient.DeleteAsync($"/api/users/{studentId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await studentClient.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task TamperedToken_IsRejected()
    {
        var (email, password) = await RegisterStudentCredentialsAsync();
        var auth = await LoginAsync(CreateClient(), email, password);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken[..^4] + "AAAA");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    private async Task<(string Email, string Password)> RegisterStudentCredentialsAsync()
    {
        const string password = "Student123";
        var email = UniqueEmail("session");

        var response = await RegisterStudentAsync(CreateClient(), email, password);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (email, password);
    }

    private static async Task<Guid> GetCurrentUserIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var document = await ReadJsonAsync(response);
        return document.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task AdminUpdateAsync(HttpClient adminClient, Guid userId, UserRole role, bool isActive)
    {
        var response = await SendJsonAsync(adminClient, HttpMethod.Put, $"/api/users/{userId}", new
        {
            firstName = "Тест",
            lastName = "Потребител",
            role,
            isActive
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
