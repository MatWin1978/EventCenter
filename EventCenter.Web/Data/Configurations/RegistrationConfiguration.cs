using EventCenter.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventCenter.Web.Data.Configurations;

public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ToTable("Registrations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RegistrationType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Phone)
            .HasMaxLength(50);

        builder.Property(r => r.Company)
            .HasMaxLength(200);

        builder.Property(r => r.RegistrationDateUtc)
            .IsRequired();

        builder.Property(r => r.IsConfirmed)
            .IsRequired();

        builder.Property(r => r.NumberOfCompanions)
            .IsRequired();

        builder.Property(r => r.SpecialRequirements)
            .HasMaxLength(1000);

        // Index on EventId for faster lookup
        builder.HasIndex(r => r.EventId);

        // Index on Email for duplicate checking
        builder.HasIndex(r => new { r.EventId, r.Email });

        // Many-to-many relationship with EventOption
        builder.HasMany(r => r.SelectedOptions)
            .WithMany(o => o.Registrations)
            .UsingEntity<Dictionary<string, object>>(
                "RegistrationEventOption",
                j => j.HasOne<EventOption>().WithMany().HasForeignKey("EventOptionId"),
                j => j.HasOne<Registration>().WithMany().HasForeignKey("RegistrationId"));
    }
}
