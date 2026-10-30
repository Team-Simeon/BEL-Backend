using System.Net;
using BEL.Api.Contracts.Auth;
using BEL.Api.Contracts.Schools;
using BEL.Api.Contracts.Users;
using BEL.Api.Models;

namespace BEL.Api.Tests;

public sealed class AuthorizationAndOwnershipTests(BelApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task AnonymousRequests_ToProtectedEndpoints_ReturnUnauthorized()
    {
        var client = CreateClient();

        var usersResponse = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Unauthorized, usersResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);

        var createSchoolResponse = await SendJsonAsync(client, HttpMethod.Post, "/api/schools", new
        {
            name = UniqueSchoolName(),
            city = "София"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, createSchoolResponse.StatusCode);
    }

    [Fact]
    public async Task Student_CanViewAndEditOwnProfile_ButNotOtherProfilesOrAdminRoutes()
    {
        var firstClient = CreateClient();
        var firstEmail = UniqueEmail("owner");
        var firstRegistrationResponse = await RegisterStudentAsync(firstClient, firstEmail);
        var firstRegistration = await ReadAsync<RegistrationResponse>(firstRegistrationResponse);
        Assert.NotNull(firstRegistration);

        var secondEmail = UniqueEmail("other");
        var secondRegistrationResponse = await RegisterStudentAsync(CreateClient(), secondEmail);
        var secondRegistration = await ReadAsync<RegistrationResponse>(secondRegistrationResponse);
        Assert.NotNull(secondRegistration);

        SetBearerToken(firstClient, firstRegistration.AccessToken!);

        var ownProfileResponse = await firstClient.GetAsync($"/api/users/{firstRegistration.User.Id}");
        Assert.Equal(HttpStatusCode.OK, ownProfileResponse.StatusCode);

        var otherProfileResponse = await firstClient.GetAsync($"/api/users/{secondRegistration.User.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, otherProfileResponse.StatusCode);

        var updateProfileResponse = await SendJsonAsync(
            firstClient,
            HttpMethod.Put,
            "/api/users/me",
            new
            {
                firstName = "Ново",
                lastName = "Име"
            });
        Assert.Equal(HttpStatusCode.OK, updateProfileResponse.StatusCode);

        var updatedProfile = await ReadAsync<UserResponse>(updateProfileResponse);
        Assert.NotNull(updatedProfile);
        Assert.Equal("Ново", updatedProfile.FirstName);
        Assert.Equal("Име", updatedProfile.LastName);

        var adminUpdateAttempt = await SendJsonAsync(
            firstClient,
            HttpMethod.Put,
            $"/api/users/{firstRegistration.User.Id}",
            new
            {
                firstName = "Хакер",
                lastName = "Опит",
                role = "Admin",
                schoolId = (Guid?)null,
                isApproved = true,
                isActive = true
            });
        Assert.Equal(HttpStatusCode.Forbidden, adminUpdateAttempt.StatusCode);

        var adminClient = await CreateAdminClientAsync();
        var adminReadResponse = await adminClient.GetAsync($"/api/users/{secondRegistration.User.Id}");
        Assert.Equal(HttpStatusCode.OK, adminReadResponse.StatusCode);

        var studentUsersResponse = await firstClient.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, studentUsersResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_CannotRemoveOwnAdminAccessOrDeleteOwnAccount()
    {
        var adminClient = await CreateAdminClientAsync();
        var admin = await GetCurrentUserAsync(adminClient);
        Assert.Equal(UserRole.Admin, admin.Role);

        var demoteResponse = await SendJsonAsync(
            adminClient,
            HttpMethod.Put,
            $"/api/users/{admin.Id}",
            new
            {
                firstName = admin.FirstName,
                lastName = admin.LastName,
                role = "Student",
                schoolId = (Guid?)null,
                isApproved = true,
                isActive = true
            });
        Assert.Equal(HttpStatusCode.BadRequest, demoteResponse.StatusCode);

        var deleteResponse = await adminClient.DeleteAsync($"/api/users/{admin.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_CanDeleteUser_AndDeletedUserCannotLogin()
    {
        var adminClient = await CreateAdminClientAsync();
        var email = UniqueEmail("deletable");
        var password = "Student123";

        var registrationResponse = await RegisterStudentAsync(CreateClient(), email, password);
        var registration = await ReadAsync<RegistrationResponse>(registrationResponse);
        Assert.NotNull(registration);

        var deleteResponse = await adminClient.DeleteAsync($"/api/users/{registration.User.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var loginResponse = await SendJsonAsync(CreateClient(), HttpMethod.Post, "/api/auth/login", new
        {
            email,
            password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task AdminUpdate_ValidatesTeacherSchoolAndRoleTransitions()
    {
        var adminClient = await CreateAdminClientAsync();
        var teacher = await RegisterApprovedTeacherAsync(adminClient);

        var missingSchoolResponse = await SendJsonAsync(
            adminClient,
            HttpMethod.Put,
            $"/api/users/{teacher.User.Id}",
            new
            {
                firstName = teacher.User.FirstName,
                lastName = teacher.User.LastName,
                role = "Teacher",
                schoolId = (Guid?)null,
                isApproved = true,
                isActive = true
            });
        Assert.Equal(HttpStatusCode.BadRequest, missingSchoolResponse.StatusCode);

        var demoteResponse = await SendJsonAsync(
            adminClient,
            HttpMethod.Put,
            $"/api/users/{teacher.User.Id}",
            new
            {
                firstName = teacher.User.FirstName,
                lastName = teacher.User.LastName,
                role = "Student",
                schoolId = (Guid?)null,
                isApproved = true,
                isActive = true
            });
        Assert.Equal(HttpStatusCode.OK, demoteResponse.StatusCode);

        var demoted = await ReadAsync<UserResponse>(demoteResponse);
        Assert.NotNull(demoted);
        Assert.Equal(UserRole.Student, demoted.Role);
        Assert.Null(demoted.SchoolId);
        Assert.Null(demoted.SchoolName);

        var newSchool = await CreateSchoolAsync(adminClient);
        var promoteResponse = await SendJsonAsync(
            adminClient,
            HttpMethod.Put,
            $"/api/users/{teacher.User.Id}",
            new
            {
                firstName = teacher.User.FirstName,
                lastName = teacher.User.LastName,
                role = "Teacher",
                schoolId = newSchool.Id,
                isApproved = true,
                isActive = true
            });
        Assert.Equal(HttpStatusCode.OK, promoteResponse.StatusCode);

        var promoted = await ReadAsync<UserResponse>(promoteResponse);
        Assert.NotNull(promoted);
        Assert.Equal(UserRole.Teacher, promoted.Role);
        Assert.Equal(newSchool.Id, promoted.SchoolId);
        Assert.True(promoted.IsApproved);

        var deactivateResponse = await SendJsonAsync(
            adminClient,
            HttpMethod.Put,
            $"/api/users/{teacher.User.Id}",
            new
            {
                firstName = teacher.User.FirstName,
                lastName = teacher.User.LastName,
                role = "Teacher",
                schoolId = newSchool.Id,
                isApproved = true,
                isActive = false
            });
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        var loginResponse = await SendJsonAsync(CreateClient(), HttpMethod.Post, "/api/auth/login", new
        {
            email = teacher.User.Email,
            password = "Teacher123"
        });
        Assert.Equal(HttpStatusCode.Forbidden, loginResponse.StatusCode);
    }

    [Fact]
    public async Task SchoolEndpoints_EnforceAdminRoleAndValidation()
    {
        var adminClient = await CreateAdminClientAsync();
        var studentClient = CreateClient();
        var studentRegistrationResponse = await RegisterStudentAsync(studentClient, UniqueEmail("schoolstudent"));
        var studentRegistration = await ReadAsync<RegistrationResponse>(studentRegistrationResponse);
        Assert.NotNull(studentRegistration);
        SetBearerToken(studentClient, studentRegistration.AccessToken!);

        var createByStudentResponse = await SendJsonAsync(studentClient, HttpMethod.Post, "/api/schools", new
        {
            name = UniqueSchoolName(),
            city = "Пловдив"
        });
        Assert.Equal(HttpStatusCode.Forbidden, createByStudentResponse.StatusCode);

        var school = await CreateSchoolAsync(adminClient, city: "Пловдив");

        var duplicateResponse = await SendJsonAsync(adminClient, HttpMethod.Post, "/api/schools", new
        {
            name = school.Name,
            city = "Пловдив"
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var publicListBeforeResponse = await CreateClient().GetAsync("/api/schools");
        Assert.Equal(HttpStatusCode.OK, publicListBeforeResponse.StatusCode);
        var publicListBefore = await ReadAsync<List<SchoolResponse>>(publicListBeforeResponse);
        Assert.NotNull(publicListBefore);
        Assert.Contains(publicListBefore, item => item.Id == school.Id);

        var updateResponse = await SendJsonAsync(
            adminClient,
            HttpMethod.Put,
            $"/api/schools/{school.Id}",
            new
            {
                name = school.Name,
                city = school.City,
                address = school.Address,
                isActive = false
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var publicListAfter = await ReadAsync<List<SchoolResponse>>(
            await CreateClient().GetAsync("/api/schools"));
        Assert.NotNull(publicListAfter);
        Assert.DoesNotContain(publicListAfter, item => item.Id == school.Id);

        var publicInactiveDetailResponse = await CreateClient().GetAsync($"/api/schools/{school.Id}");
        Assert.Equal(HttpStatusCode.NotFound, publicInactiveDetailResponse.StatusCode);

        var adminInactiveDetailResponse = await adminClient.GetAsync($"/api/schools/{school.Id}");
        Assert.Equal(HttpStatusCode.OK, adminInactiveDetailResponse.StatusCode);

        var anonymousInactiveResponse = await CreateClient().GetAsync("/api/schools?includeInactive=true");
        Assert.Equal(HttpStatusCode.Forbidden, anonymousInactiveResponse.StatusCode);

        var adminAllResponse = await adminClient.GetAsync("/api/schools?includeInactive=true");
        Assert.Equal(HttpStatusCode.OK, adminAllResponse.StatusCode);

        var adminAll = await ReadAsync<List<SchoolResponse>>(adminAllResponse);
        Assert.NotNull(adminAll);
        Assert.Contains(adminAll, item => item.Id == school.Id && !item.IsActive);
    }

    [Fact]
    public async Task SchoolWithAssignedTeacher_CannotBeDeleted()
    {
        var adminClient = await CreateAdminClientAsync();
        var teacher = await RegisterApprovedTeacherAsync(adminClient);
        Assert.NotNull(teacher.User.SchoolId);

        var deleteAssignedSchoolResponse = await adminClient.DeleteAsync(
            $"/api/schools/{teacher.User.SchoolId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteAssignedSchoolResponse.StatusCode);

        var emptySchool = await CreateSchoolAsync(adminClient);
        var deleteEmptySchoolResponse = await adminClient.DeleteAsync($"/api/schools/{emptySchool.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteEmptySchoolResponse.StatusCode);
    }

    private async Task<UserResponse> GetCurrentUserAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await ReadAsync<UserResponse>(response);
        Assert.NotNull(user);
        return user;
    }

    private static void SetBearerToken(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }
}
