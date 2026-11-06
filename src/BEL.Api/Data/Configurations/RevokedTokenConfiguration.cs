using BEL.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BEL.Api.Data.Configurations;

public sealed class RevokedTokenConfiguration : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.ToTable("RevokedTokens");

        builder.HasKey(token => token.TokenId);

        builder.Property(token => token.TokenId)
            .HasMaxLength(64);

        builder.Property(token => token.ExpiresAtUtc)
            .IsRequired();

        builder.Property(token => token.RevokedAtUtc)
            .IsRequired();

        builder.HasIndex(token => token.ExpiresAtUtc);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
