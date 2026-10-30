using BEL.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BEL.Api.Tests;

public sealed class BelApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@belprep.test";
    public const string AdminPassword = "AdminPass123";

    private const string TestJwtKey =
        "bel-prep-integration-tests-signing-key-please-change-in-production-123456";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:DefaultConnection", "Data Source=:memory:");
        builder.UseSetting("Jwt:Issuer", "BEL.Api.Tests");
        builder.UseSetting("Jwt:Audience", "BEL.Api.Tests");
        builder.UseSetting("Jwt:Key", TestJwtKey);
        builder.UseSetting("Jwt:ExpiryMinutes", "60");
        builder.UseSetting("Seed:AdminEmail", AdminEmail);
        builder.UseSetting("Seed:AdminPassword", AdminPassword);
        builder.UseSetting("Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command", "Warning");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BelDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<BelDbContext>>();
            services.RemoveAll<BelDbContext>();

            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }

            services.AddDbContext<BelDbContext>(options => options.UseSqlite(_connection));

            using var db = new BelDbContext(
                new DbContextOptionsBuilder<BelDbContext>()
                    .UseSqlite(_connection)
                    .Options);

            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
