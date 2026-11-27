using System.Net;
using BEL.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BEL.Api.Tests;

public sealed class FeedbackDatabaseFailureTests(BelApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateFeedback_WhenDatabaseFails_ReturnsCleanServiceUnavailableResponse()
    {
        const string password = "Student123";
        var email = UniqueEmail("feedback-db");
        var registration = await RegisterStudentAsync(CreateClient(), email, password);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var client = await CreateAuthenticatedClientAsync(email, password);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BelDbContext>();
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"Feedback\";");
        }

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/feedback", new
        {
            type = "Problem",
            message = "Това съобщение няма да бъде записано, защото базата е недостъпна."
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await ReadProblemAsync(response);
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, problem.GetProperty("status").GetInt32());
        Assert.Equal("Database Unavailable", problem.GetProperty("title").GetString());
    }
}
