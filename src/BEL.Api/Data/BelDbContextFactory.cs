using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BEL.Api.Data;

public sealed class BelDbContextFactory : IDesignTimeDbContextFactory<BelDbContext>
{
    private const string FallbackConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=BelPrep;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public BelDbContext CreateDbContext(string[] args)
    {
        var basePath = ResolveBasePath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? FallbackConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<BelDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new BelDbContext(optionsBuilder.Options);
    }

    private static string ResolveBasePath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
        {
            return currentDirectory;
        }

        var projectDirectory = Path.Combine(currentDirectory, "src", "BEL.Api");
        return Directory.Exists(projectDirectory) ? projectDirectory : currentDirectory;
    }
}
