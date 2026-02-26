using EventCenter.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventCenter.Web.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.StartDateUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(e => e.EndDateUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(e => e.RegistrationDeadlineUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(e => e.MaxCapacity)
            .IsRequired();

        builder.Property(e => e.MaxCompanions)
            .IsRequired();

        builder.Property(e => e.IsPublished)
            .IsRequired();

        // Index on IsPublished for filtering published events
        builder.HasIndex(e => e.IsPublished);

        // CHECK constraint: RegistrationDeadlineUtc must be <= StartDateUtc
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Event_RegistrationDeadlineBeforeStart",
            "[RegistrationDeadlineUtc] <= [StartDateUtc]"));

        // Relationships
        builder.HasMany(e => e.AgendaItems)
            .WithOne(a => a.Event)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Companies)
            .WithOne(c => c.Event)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Registrations)
            .WithOne(r => r.Event)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
