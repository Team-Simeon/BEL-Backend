using BEL.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BEL.Api.Data.Configurations;

public sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("Feedback");

        builder.HasKey(feedback => feedback.Id);

        builder.Property(feedback => feedback.Type)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(feedback => feedback.Message)
            .IsRequired()
            .HasMaxLength(Feedback.MaxMessageLength);

        builder.Property(feedback => feedback.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(feedback => feedback.CreatedAtUtc);

        builder.HasOne(feedback => feedback.User)
            .WithMany()
            .HasForeignKey(feedback => feedback.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
