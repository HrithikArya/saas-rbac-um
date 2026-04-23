using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.HasOne(p => p.Organization)
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Plan)
            .WithMany()
            .HasForeignKey(p => p.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.OrganizationId);
        builder.HasIndex(p => p.CreatedAt);
    }
}
