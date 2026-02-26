using EventCenter.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventCenter.Web.Data.Configurations;

public class EventAgendaItemConfiguration : IEntityTypeConfiguration<EventAgendaItem>
{
    public void Configure(EntityTypeBuilder<EventAgendaItem> builder)
    {
        builder.ToTable("EventAgendaItems");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(2000);

        builder.Property(a => a.StartDateTimeUtc)
            .IsRequired();

        builder.Property(a => a.EndDateTimeUtc)
            .IsRequired();

        builder.Property(a => a.CostForMakler)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(a => a.CostForGuest)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(a => a.IsMandatory)
            .IsRequired();

        builder.Property(a => a.MaxParticipants)
            .IsRequired(false);

        builder.Property(a => a.MaklerCanParticipate)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.GuestsCanParticipate)
            .IsRequired()
            .HasDefaultValue(true);

        // Index on EventId for faster lookup
        builder.HasIndex(a => a.EventId);

        // Relationship is configured from Event side
    }
}
