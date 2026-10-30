using BEL.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BEL.Api.Data.Configurations;

public sealed class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.ToTable("Schools");

        builder.HasKey(school => school.Id);

        builder.Property(school => school.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(school => school.Name)
            .IsUnique();

        builder.Property(school => school.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(school => school.Address)
            .HasMaxLength(300);

        builder.Property(school => school.IsActive)
            .IsRequired();

        builder.Property(school => school.CreatedAtUtc)
            .IsRequired();

        builder.Property(school => school.UpdatedAtUtc)
            .IsRequired();
    }
}
