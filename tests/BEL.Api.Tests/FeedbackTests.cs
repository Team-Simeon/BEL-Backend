using System.Net;
using BEL.Api.Contracts.Feedback;
using BEL.Api.Data;
using BEL.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BEL.Api.Tests;

public sealed class FeedbackTests(BelApiFactory factory) : IntegrationTestBase(factory)
{
    [Theory]
    [InlineData(FeedbackType.Problem)]
    [InlineData(FeedbackType.Suggestion)]
    public async Task CreateFeedback_WithValidType_SavesItAndReturnsTheStoredEntry(FeedbackType type)
    {
        var client = await CreateStudentClientAsync();
        const string message = "  Бутонът за изход не работи на телефона.  ";

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/feedback", new
        {
            type,
            message
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var feedback = await ReadAsync<FeedbackResponse>(response);
        Assert.NotNull(feedback);
        Assert.NotEqual(Guid.Empty, feedback.Id);
        Assert.Equal(type, feedback.Type);
        Assert.Equal(message.Trim(), feedback.Message);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BelDbContext>();
        var stored = await db.Feedback.AsNoTracking().SingleAsync(f => f.Id == feedback.Id);
        Assert.Equal(type, stored.Type);
        Assert.Equal(message.Trim(), stored.Message);
    }

    [Theory]
    [InlineData(Feedback.MinMessageLength)]
    [InlineData(Feedback.MaxMessageLength)]
    public async Task CreateFeedback_WithBoundaryMessageLength_IsAccepted(int length)
    {
        var client = await CreateStudentClientAsync();

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/feedback", new
        {
            type = nameof(FeedbackType.Problem),
            message = new string('а', length)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var feedback = await ReadAsync<FeedbackResponse>(response);
        Assert.NotNull(feedback);
        Assert.Equal(length, feedback.Message.Length);
    }

    [Fact]
    public async Task CreateFeedback_WithNineCharacterMessage_ReturnsBadRequest()
    {
        var client = await CreateStudentClientAsync();

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/feedback", new
        {
            type = nameof(FeedbackType.Problem),
            message = "123456789"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await ReadValidationErrorsAsync(response);
        Assert.True(errors.ContainsKey(nameof(CreateFeedbackRequest.Message)));
        Assert.False(errors.ContainsKey(nameof(CreateFeedbackRequest.Type)));
    }

    [Fact]
    public async Task CreateFeedback_WithTooLongMessage_ReturnsBadRequest()
    {
        var client = await CreateStudentClientAsync();

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/feedback", new
        {
            type = nameof(FeedbackType.Suggestion),
            message = new string('а', Feedback.MaxMessageLength + 1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await ReadValidationErrorsAsync(response);
        Assert.True(errors.ContainsKey(nameof(CreateFeedbackRequest.Message)));
        Assert.False(errors.ContainsKey(nameof(CreateFeedbackRequest.Type)));
    }

    [Fact]
    public async Task CreateFeedback_WithMessageThatIsTooShortAfterTrimming_ReturnsBadRequest()
    {
        var client = await CreateStudentClientAsync();

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/feedback", new
        {
            type = nameof(FeedbackType.Problem),
            message = "  123456789  "
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await ReadValidationErrorsAsync(response);
        Assert.True(errors.ContainsKey(nameof(CreateFeedbackRequest.Message)));
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("3")]
    [InlineData("")]
    public async Task CreateFeedback_WithInvalidType_ReturnsBadRequest(string type)
    {
        var client = await CreateStudentClientAsync();

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/feedback", new
        {
            type,
            message = "Това е достатъчно дълго съобщение."
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await ReadValidationErrorsAsync(response);
        Assert.True(errors.ContainsKey(nameof(CreateFeedbackRequest.Type)));
        Assert.False(errors.ContainsKey(nameof(CreateFeedbackRequest.Message)));
    }

    [Fact]
    public async Task CreateFeedback_WithMissingType_ReturnsBadRequest()
    {
        var client = await CreateStudentClientAsync();

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/feedback", new
        {
            message = "Това е достатъчно дълго съобщение."
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await ReadValidationErrorsAsync(response);
        Assert.True(errors.ContainsKey(nameof(CreateFeedbackRequest.Type)));
    }

    [Fact]
    public async Task CreateFeedback_WithMissingMessage_ReturnsBadRequest()
    {
        var client = await CreateStudentClientAsync();

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/feedback", new
        {
            type = nameof(FeedbackType.Problem)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await ReadValidationErrorsAsync(response);
        Assert.True(errors.ContainsKey(nameof(CreateFeedbackRequest.Message)));
    }

    [Fact]
    public async Task CreateFeedback_WithBothFieldsInvalid_ReturnsAnErrorForEachField()
    {
        var client = await CreateStudentClientAsync();

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/feedback", new
        {
            type = "Invalid",
            message = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await ReadValidationErrorsAsync(response);
        Assert.True(errors.ContainsKey(nameof(CreateFeedbackRequest.Type)));
        Assert.True(errors.ContainsKey(nameof(CreateFeedbackRequest.Message)));
    }

    [Fact]
    public async Task CreateFeedback_WithoutToken_ReturnsUnauthorized()
    {
        var response = await SendJsonAsync(CreateClient(), HttpMethod.Post, "/api/feedback", new
        {
            type = nameof(FeedbackType.Problem),
            message = "Съобщение без вход в системата."
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFeedback_AsAdmin_ReturnsSubmittedEntries()
    {
        var studentClient = await CreateStudentClientAsync();
        var createResponse = await SendJsonAsync(studentClient, HttpMethod.Post, "/api/feedback", new
        {
            type = nameof(FeedbackType.Suggestion),
            message = "Предложение, което администраторът трябва да види."
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await ReadAsync<FeedbackResponse>(createResponse);

        var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.GetAsync("/api/feedback");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entries = await ReadAsync<List<FeedbackResponse>>(response);
        Assert.NotNull(entries);
        Assert.Contains(entries, entry => entry.Id == created!.Id);
    }

    [Fact]
    public async Task GetFeedback_AsStudent_ReturnsForbidden()
    {
        var client = await CreateStudentClientAsync();

        var response = await client.GetAsync("/api/feedback");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetFeedback_WithoutToken_ReturnsUnauthorized()
    {
        var response = await CreateClient().GetAsync("/api/feedback");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> CreateStudentClientAsync()
    {
        const string password = "Student123";
        var email = UniqueEmail("feedback");

        var registration = await RegisterStudentAsync(CreateClient(), email, password);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        return await CreateAuthenticatedClientAsync(email, password);
    }

    private static async Task<Dictionary<string, string[]>> ReadValidationErrorsAsync(
        HttpResponseMessage response)
    {
        var json = await ReadJsonAsync(response);
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        Assert.True(json.RootElement.TryGetProperty("errors", out var errorProperty));

        foreach (var error in errorProperty.EnumerateObject())
        {
            errors[error.Name] = error.Value
                .EnumerateArray()
                .Select(message => message.GetString() ?? string.Empty)
                .ToArray();
        }

        return errors;
    }
}
