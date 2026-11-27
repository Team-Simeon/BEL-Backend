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
    public async Task CreateFeedback_SavesItAndReturnsTheStoredEntry(FeedbackType type)
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

    [Fact]
    public async Task CreateFeedback_WithoutToken_ReturnsUnauthorized()
    {
        var response = await SendJsonAsync(CreateClient(), HttpMethod.Post, "/api/feedback", new
        {
            type = FeedbackType.Problem,
            message = "Съобщение без вход в системата."
        });

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
}
