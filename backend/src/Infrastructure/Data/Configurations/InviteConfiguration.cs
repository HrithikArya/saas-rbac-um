using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class InviteConfiguration : IEntityTypeConfiguration<Invite>
{
    public void Configure(EntityTypeBuilder<Invite> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(i => i.Token)
            .IsUnique();

        builder.Property(i => i.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(i => i.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
    }
}
