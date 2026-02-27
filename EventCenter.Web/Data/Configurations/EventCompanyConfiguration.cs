using EventCenter.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventCenter.Web.Data.Configurations;

public class EventCompanyConfiguration : IEntityTypeConfiguration<EventCompany>
{
    public void Configure(EntityTypeBuilder<EventCompany> builder)
    {
        builder.ToTable("EventCompanies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.ContactEmail)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.ContactPhone)
            .HasMaxLength(50);

        builder.Property(c => c.PricePerPerson)
            .HasPrecision(18, 2);

        builder.Property(c => c.MaxParticipants)
            .IsRequired(false);

        builder.Property(c => c.InvitationCode)
            .HasMaxLength(100);

        builder.Property(c => c.InvitationSentUtc);

        // Phase 04 fields
        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.PercentageDiscount)
            .HasPrecision(5, 2);

        builder.Property(c => c.PersonalMessage)
            .HasMaxLength(2000);

        builder.Property(c => c.ExpiresAtUtc);

        // Index on EventId for faster lookup
        builder.HasIndex(c => c.EventId);

        // Unique index on InvitationCode (when not null)
        builder.HasIndex(c => c.InvitationCode)
            .IsUnique()
            .HasFilter("[InvitationCode] IS NOT NULL");

        // Relationships
        builder.HasMany(c => c.Registrations)
            .WithOne(r => r.EventCompany)
            .HasForeignKey(r => r.EventCompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
