using BEL.Api.Models;
using BEL.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace BEL.Api.Data;

public static class DataSeeder
{
    public static async Task SeedAdminAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Administrator seeding skipped: both Seed:AdminEmail and Seed:AdminPassword must be configured.");
            return;
        }

        if (!IsPasswordValid(password))
        {
            throw new InvalidOperationException(
                "Seed:AdminPassword must be 8-128 characters long and contain at least one letter and one digit.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BelDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin, cancellationToken))
        {
            return;
        }

        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            logger.LogWarning(
                "Administrator seeding skipped: a non-admin user with email {Email} already exists.",
                normalizedEmail);
            return;
        }

        db.Users.Add(new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(password),
            FirstName = "System",
            LastName = "Administrator",
            Role = UserRole.Admin,
            IsApproved = true,
            IsActive = true
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded the administrator account {Email}.", normalizedEmail);
    }

    private static bool IsPasswordValid(string password) =>
        password.Length is >= 8 and <= 128 &&
        password.Any(char.IsLetter) &&
        password.Any(char.IsDigit);
}
