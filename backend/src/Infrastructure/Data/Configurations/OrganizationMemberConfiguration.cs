using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.UserId, m.OrganizationId })
            .IsUnique();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.JoinedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
    }
}
