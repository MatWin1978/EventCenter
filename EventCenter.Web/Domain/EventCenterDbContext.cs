using EventCenter.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventCenter.Web.Domain;

public class EventCenterDbContext : DbContext
{
    public EventCenterDbContext(DbContextOptions<EventCenterDbContext> options)
        : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventAgendaItem> AgendaItems => Set<EventAgendaItem>();
    public DbSet<EventCompany> EventCompanies => Set<EventCompany>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<EventOption> EventOptions => Set<EventOption>();
    public DbSet<RegistrationAgendaItem> RegistrationAgendaItems => Set<RegistrationAgendaItem>();
    public DbSet<EventCompanyAgendaItemPrice> EventCompanyAgendaItemPrices => Set<EventCompanyAgendaItemPrice>();
    public DbSet<Company> Companies => Set<Company>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventCenterDbContext).Assembly);
    }
}
