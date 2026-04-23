using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.FeaturesJson)
            .IsRequired()
            .HasDefaultValue("{}");

        builder.HasData(
            new Plan { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Free",    StripePriceId = null,            PriceInCents = 0,    FeaturesJson = "{\"max_members\":3,\"advanced_reports\":false}" },
            new Plan { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Starter", StripePriceId = "price_starter", PriceInCents = 900,  FeaturesJson = "{\"max_members\":10,\"advanced_reports\":false}" },
            new Plan { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "Pro",     StripePriceId = "price_pro",     PriceInCents = 2900, FeaturesJson = "{\"max_members\":100,\"advanced_reports\":true}" }
        );
    }
}
