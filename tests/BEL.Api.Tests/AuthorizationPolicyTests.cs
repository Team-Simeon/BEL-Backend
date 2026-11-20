using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using BEL.Api.Models;
using BEL.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BEL.Api.Tests;

public sealed class AuthorizationPolicyTests(BelApiFactory factory) : IntegrationTestBase(factory)
{
    [Theory]
    [InlineData(UserRole.Admin, true, true)]
    [InlineData(UserRole.Teacher, true, true)]
    [InlineData(UserRole.Teacher, false, false)]
    [InlineData(UserRole.Student, true, false)]
    public async Task CanPublishContent_AllowsOnlyApprovedTeachersAndAdmins(
        UserRole role,
        bool isApproved,
        bool expected)
    {
        var result = await AuthorizeAsync(CreatePrincipal(role, isApproved), AuthorizationPolicies.CanPublishContent);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UserRole.Student, true)]
    [InlineData(UserRole.Teacher, false)]
    [InlineData(UserRole.Admin, false)]
    public async Task StudentOnly_AllowsOnlyStudents(UserRole role, bool expected)
    {
        var result = await AuthorizeAsync(CreatePrincipal(role, isApproved: true), AuthorizationPolicies.StudentOnly);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.Teacher, false)]
    [InlineData(UserRole.Student, false)]
    public async Task AdminOnly_AllowsOnlyAdmins(UserRole role, bool expected)
    {
        var result = await AuthorizeAsync(CreatePrincipal(role, isApproved: true), AuthorizationPolicies.AdminOnly);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task CanPublishContent_RejectsTeacherTokenWithoutApprovedClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, nameof(UserRole.Teacher))],
            authenticationType: "Test"));

        Assert.False(await AuthorizeAsync(principal, AuthorizationPolicies.CanPublishContent));
    }

    [Fact]
    public async Task IssuedTokens_CarryApprovalClaim_ThatSatisfiesThePublishPolicy()
    {
        var adminClient = await CreateAdminClientAsync();
        const string teacherPassword = "Teacher123";
        var teacherEmail = UniqueEmail("teacher");
        await RegisterApprovedTeacherAsync(adminClient, teacherEmail, teacherPassword);

        var studentEmail = UniqueEmail("student");
        Assert.Equal(HttpStatusCode.Created, (await RegisterStudentAsync(CreateClient(), studentEmail)).StatusCode);

        var teacherToken = (await LoginAsync(CreateClient(), teacherEmail, teacherPassword)).AccessToken;
        var studentToken = (await LoginAsync(CreateClient(), studentEmail, "Student123")).AccessToken;

        Assert.True(await AuthorizeAsync(ReadPrincipal(teacherToken), AuthorizationPolicies.CanPublishContent));
        Assert.False(await AuthorizeAsync(ReadPrincipal(studentToken), AuthorizationPolicies.CanPublishContent));
    }

    [Fact]
    public void FallbackPolicy_RequiresAuthenticatedUser()
    {
        var options = Factory.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.FallbackPolicy);
        Assert.Contains(
            options.FallbackPolicy.Requirements,
            requirement => requirement is Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task HealthEndpoint_RemainsPublic()
    {
        var response = await CreateClient().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminAccount_IsSeededAndCanUseAdminRoutes()
    {
        var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<bool> AuthorizeAsync(ClaimsPrincipal principal, string policy)
    {
        using var scope = Factory.Services.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var result = await authorizationService.AuthorizeAsync(principal, resource: null, policy);
        return result.Succeeded;
    }

    private static ClaimsPrincipal CreatePrincipal(UserRole role, bool isApproved) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role.ToString()),
                new Claim(BelClaimTypes.Approved, isApproved.ToString())
            ],
            authenticationType: "Test"));

    private static ClaimsPrincipal ReadPrincipal(string accessToken)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        return new ClaimsPrincipal(new ClaimsIdentity(token.Claims, "Bearer", ClaimTypes.Name, ClaimTypes.Role));
    }
}
