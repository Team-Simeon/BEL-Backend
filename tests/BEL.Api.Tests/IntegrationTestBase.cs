using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BEL.Api.Contracts.Auth;
using BEL.Api.Contracts.Schools;

namespace BEL.Api.Tests;

public abstract class IntegrationTestBase : IClassFixture<BelApiFactory>
{
    protected IntegrationTestBase(BelApiFactory factory)
    {
        Factory = factory;
    }

    protected BelApiFactory Factory { get; }

    protected static JsonSerializerOptions JsonOptions { get; } = CreateJsonOptions();

    protected HttpClient CreateClient() => Factory.CreateClient();

    protected async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = CreateClient();
        var auth = await LoginAsync(client, BelApiFactory.AdminEmail, BelApiFactory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = CreateClient();
        var auth = await LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    protected async Task<AuthResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ReadAsync<AuthResponse>(response))!;
    }

    protected async Task<HttpResponseMessage> RegisterStudentAsync(
        HttpClient client,
        string? email = null,
        string password = "Student123")
    {
        return await SendJsonAsync(client, HttpMethod.Post, "/api/auth/register/student", new
        {
            email = email ?? UniqueEmail("student"),
            firstName = "Тест",
            lastName = "Ученик",
            password
        });
    }

    protected async Task<HttpResponseMessage> RegisterTeacherAsync(
        HttpClient client,
        Guid schoolId,
        string? email = null,
        string password = "Teacher123")
    {
        return await SendJsonAsync(client, HttpMethod.Post, "/api/auth/register/teacher", new
        {
            email = email ?? UniqueEmail("teacher"),
            firstName = "Тест",
            lastName = "Учител",
            password,
            schoolId
        });
    }

    protected async Task<SchoolResponse> CreateSchoolAsync(
        HttpClient adminClient,
        string? name = null,
        string city = "София",
        string? address = "ул. Тестова 1")
    {
        var response = await SendJsonAsync(adminClient, HttpMethod.Post, "/api/schools", new
        {
            name = name ?? UniqueSchoolName(),
            city,
            address
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync<SchoolResponse>(response))!;
    }

    protected async Task<RegistrationResponse> RegisterApprovedTeacherAsync(
        HttpClient adminClient,
        string? email = null,
        string password = "Teacher123")
    {
        var school = await CreateSchoolAsync(adminClient);
        var response = await RegisterTeacherAsync(adminClient, school.Id, email, password);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var registration = (await ReadAsync<RegistrationResponse>(response))!;

        var approveResponse = await adminClient.PostAsync(
            $"/api/users/{registration.User.Id}/approve",
            content: null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        return registration;
    }

    protected static string UniqueEmail(string prefix) =>
        $"{prefix}.{Guid.NewGuid():N}@belprep.test";

    protected static string UniqueSchoolName() =>
        $"Тестово училище {Guid.NewGuid():N}";

    protected static async Task<HttpResponseMessage> SendJsonAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object body)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json")
        };

        return await client.SendAsync(request);
    }

    protected static async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    protected static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    protected static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        var document = await ReadJsonAsync(response);
        return document.RootElement.Clone();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
