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
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public List<string> DocumentPaths { get; set; } = new();

    public EventType EventType { get; set; } = EventType.InPerson;
    public string? ExternalRegistrationUrl { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "decimal(5,2)")]
    public decimal? WeiterbildungsstundenWebinar { get; set; }

    public string? Anmeldehinweis { get; set; }
    public string? StornohinweisMakler { get; set; }
    public string? StornohinweisUnternehmen { get; set; }

    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    public ICollection<EventAgendaItem> AgendaItems { get; set; } = new List<EventAgendaItem>();
    public ICollection<EventCompany> Companies { get; set; } = new List<EventCompany>();
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    public ICollection<EventOption> EventOptions { get; set; } = new List<EventOption>();
}
