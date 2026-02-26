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

        // Index on EventId for faster lookup
        builder.HasIndex(c => c.EventId);

        // Index on InvitationCode for invitation lookups
        builder.HasIndex(c => c.InvitationCode);

        // Relationships
        builder.HasMany(c => c.Registrations)
            .WithOne(r => r.EventCompany)
            .HasForeignKey(r => r.EventCompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
