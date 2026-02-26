using EventCenter.Web.Domain.Enums;

namespace EventCenter.Web.Domain.Entities;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public DateTime RegistrationDeadlineUtc { get; set; }
    public int MaxCapacity { get; set; }
    public int MaxCompanions { get; set; }
    public bool IsPublished { get; set; }

    // Navigation properties
    public ICollection<EventAgendaItem> AgendaItems { get; set; } = new List<EventAgendaItem>();
    public ICollection<EventCompany> Companies { get; set; } = new List<EventCompany>();
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
