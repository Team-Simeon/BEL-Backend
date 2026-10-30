using Microsoft.EntityFrameworkCore;

namespace BEL.Api.Data;

public class BelDbContext(DbContextOptions<BelDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BelDbContext).Assembly);
    }
}
