using BEL.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BEL.Api.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", table =>
            table.HasCheckConstraint(
                "CK_Users_TeacherSchool",
                "([Role] <> 'Teacher' OR [SchoolId] IS NOT NULL) AND ([Role] = 'Teacher' OR [SchoolId] IS NULL)"));

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(user => user.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(user => user.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(user => user.Role)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(user => user.IsApproved)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .IsRequired();

        builder.Property(user => user.SecurityStamp)
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .IsRequired();

        builder.Property(user => user.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(user => user.Role);
        builder.HasIndex(user => user.SchoolId);

        builder.HasOne(user => user.School)
            .WithMany(school => school.Users)
            .HasForeignKey(user => user.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
