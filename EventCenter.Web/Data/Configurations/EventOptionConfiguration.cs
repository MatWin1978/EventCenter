using EventCenter.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventCenter.Web.Data.Configurations;

public class EventOptionConfiguration : IEntityTypeConfiguration<EventOption>
{
    public void Configure(EntityTypeBuilder<EventOption> builder)
    {
        builder.ToTable("EventOptions");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Description)
            .HasMaxLength(1000);

        builder.Property(o => o.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(o => o.MaxQuantity)
            .IsRequired(false);

        // Index on EventId for faster lookup
        builder.HasIndex(o => o.EventId);

        // Relationship with Event (EventOption belongs to Event)
        builder.HasOne(o => o.Event)
            .WithMany(e => e.EventOptions)
            .HasForeignKey(o => o.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-many relationship is configured from Registration side
    }
}
