using BEL.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BEL.Api.Data;

public class BelDbContext(DbContextOptions<BelDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<School> Schools => Set<School>();

    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    public DbSet<Feedback> Feedback => Set<Feedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BelDbContext).Assembly);
    }
}
