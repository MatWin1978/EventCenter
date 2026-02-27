using EventCenter.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventCenter.Web.Data.Configurations;

public class EventCompanyAgendaItemPriceConfiguration : IEntityTypeConfiguration<EventCompanyAgendaItemPrice>
{
    public void Configure(EntityTypeBuilder<EventCompanyAgendaItemPrice> builder)
    {
        builder.ToTable("EventCompanyAgendaItemPrices");

        // Composite primary key
        builder.HasKey(p => new { p.EventCompanyId, p.AgendaItemId });

        builder.Property(p => p.CustomPrice)
            .HasPrecision(18, 2);

        // Relationships
        builder.HasOne(p => p.EventCompany)
            .WithMany(ec => ec.AgendaItemPrices)
            .HasForeignKey(p => p.EventCompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.AgendaItem)
            .WithMany()
            .HasForeignKey(p => p.AgendaItemId)
            .OnDelete(DeleteBehavior.NoAction);  // NoAction to prevent cascade cycles
    }
}
