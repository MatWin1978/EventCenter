using EventCenter.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventCenter.Web.Data.Configurations;

public class RegistrationAgendaItemConfiguration : IEntityTypeConfiguration<RegistrationAgendaItem>
{
    public void Configure(EntityTypeBuilder<RegistrationAgendaItem> builder)
    {
        builder.ToTable("RegistrationAgendaItems");

        // Composite primary key
        builder.HasKey(ra => new { ra.RegistrationId, ra.AgendaItemId });

        // Configure relationships
        builder.HasOne(ra => ra.Registration)
            .WithMany(r => r.RegistrationAgendaItems)
            .HasForeignKey(ra => ra.RegistrationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ra => ra.AgendaItem)
            .WithMany()
            .HasForeignKey(ra => ra.AgendaItemId)
            .OnDelete(DeleteBehavior.NoAction); // Prevent cascade cycle
    }
}
