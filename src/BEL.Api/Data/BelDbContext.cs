using BEL.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BEL.Api.Data;

public class BelDbContext(DbContextOptions<BelDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<School> Schools => Set<School>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BelDbContext).Assembly);
    }
}
