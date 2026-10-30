using System.Net;
using System.Net.Http.Headers;
using BEL.Api.Contracts.Auth;
using BEL.Api.Contracts.Users;
using BEL.Api.Models;

namespace BEL.Api.Tests;

public sealed class AuthenticationTests(BelApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task StudentRegistration_ReturnsStudentProfileAndUsableToken()
    {
        var client = CreateClient();
        var email = UniqueEmail("student");

        var response = await RegisterStudentAsync(client, email);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var registration = await ReadAsync<RegistrationResponse>(response);
        Assert.NotNull(registration);
        Assert.Equal(email, registration.User.Email);
        Assert.Equal(UserRole.Student, registration.User.Role);
        Assert.True(registration.User.IsApproved);
        Assert.Null(registration.User.SchoolId);
        Assert.False(string.IsNullOrWhiteSpace(registration.AccessToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", registration.AccessToken);

        var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var me = await ReadAsync<UserResponse>(meResponse);
        Assert.NotNull(me);
        Assert.Equal(registration.User.Id, me.Id);
        Assert.Equal(UserRole.Student, me.Role);
    }

    [Fact]
    public async Task StudentRegistration_WithInvalidPayload_ReturnsValidationErrors()
    {
        var client = CreateClient();

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/auth/register/student", new
        {
            email = "not-an-email",
            firstName = "",
            lastName = "",
            password = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemAsync(response);
        var errorFields = problem.GetProperty("errors")
            .EnumerateObject()
            .Select(property => property.Name.ToLowerInvariant())
            .ToList();

        Assert.Contains("email", errorFields);
        Assert.Contains("firstname", errorFields);
        Assert.Contains("lastname", errorFields);
        Assert.Contains("password", errorFields);
    }

    [Fact]
    public async Task StudentRegistration_WithDuplicateEmail_ReturnsConflict()
    {
        var client = CreateClient();
        var email = UniqueEmail("duplicate");

        var firstResponse = await RegisterStudentAsync(client, email);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await RegisterStudentAsync(client, email.ToUpperInvariant());
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var problem = await ReadProblemAsync(secondResponse);
        Assert.Contains("already exists", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = CreateClient();
        var email = UniqueEmail("login");
        var registrationResponse = await RegisterStudentAsync(client, email);
        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/auth/login", new
        {
            email,
            password = "WrongPass123"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await ReadProblemAsync(response);
        Assert.Equal("Invalid email or password.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task TeacherRegistration_RequiresExistingActiveSchool()
    {
        var client = CreateClient();

        var missingSchoolResponse = await RegisterTeacherAsync(client, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.BadRequest, missingSchoolResponse.StatusCode);

        var adminClient = await CreateAdminClientAsync();
        var school = await CreateSchoolAsync(adminClient);

        var deactivateResponse = await SendJsonAsync(
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
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        var inactiveSchoolResponse = await RegisterTeacherAsync(client, school.Id);
        Assert.Equal(HttpStatusCode.BadRequest, inactiveSchoolResponse.StatusCode);
    }

    [Fact]
    public async Task TeacherRegistration_IsPendingUntilAdminApproval()
    {
        var adminClient = await CreateAdminClientAsync();
        var school = await CreateSchoolAsync(adminClient);
        var teacherEmail = UniqueEmail("teacher");
        var teacherPassword = "Teacher123";

        var registrationResponse = await RegisterTeacherAsync(
            CreateClient(),
            school.Id,
            teacherEmail,
            teacherPassword);

        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        var registration = await ReadAsync<RegistrationResponse>(registrationResponse);
        Assert.NotNull(registration);
        Assert.Equal(UserRole.Teacher, registration.User.Role);
        Assert.Equal(school.Id, registration.User.SchoolId);
        Assert.Equal(school.Name, registration.User.SchoolName);
        Assert.False(registration.User.IsApproved);
        Assert.Null(registration.AccessToken);

        var anonymousClient = CreateClient();
        var pendingLoginResponse = await SendJsonAsync(anonymousClient, HttpMethod.Post, "/api/auth/login", new
        {
            email = teacherEmail,
            password = teacherPassword
        });

        Assert.Equal(HttpStatusCode.Forbidden, pendingLoginResponse.StatusCode);
        var pendingProblem = await ReadProblemAsync(pendingLoginResponse);
        Assert.Contains("approved", pendingProblem.GetProperty("detail").GetString());

        var pendingTeachersResponse = await adminClient.GetAsync(
            $"/api/users?role=Teacher&schoolId={school.Id}&isApproved=false");
        Assert.Equal(HttpStatusCode.OK, pendingTeachersResponse.StatusCode);

        var pendingTeachers = await ReadAsync<PagedResponse<UserResponse>>(pendingTeachersResponse);
        Assert.NotNull(pendingTeachers);
        Assert.Contains(pendingTeachers.Items, user => user.Id == registration.User.Id);

        var approveResponse = await adminClient.PostAsync(
            $"/api/users/{registration.User.Id}/approve",
            content: null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var approvedUser = await ReadAsync<UserResponse>(approveResponse);
        Assert.NotNull(approvedUser);
        Assert.True(approvedUser.IsApproved);

        var teacherClient = await CreateAuthenticatedClientAsync(teacherEmail, teacherPassword);
        var meResponse = await teacherClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var me = await ReadAsync<UserResponse>(meResponse);
        Assert.NotNull(me);
        Assert.Equal(UserRole.Teacher, me.Role);
        Assert.Equal(school.Id, me.SchoolId);

        var usersResponse = await teacherClient.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, usersResponse.StatusCode);
    }

    [Fact]
    public async Task StudentAccount_CannotBeApprovedAsTeacher()
    {
        var client = CreateClient();
        var registrationResponse = await RegisterStudentAsync(client, UniqueEmail("notteacher"));
        var registration = await ReadAsync<RegistrationResponse>(registrationResponse);
        Assert.NotNull(registration);

        var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PostAsync(
            $"/api/users/{registration.User.Id}/approve",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
