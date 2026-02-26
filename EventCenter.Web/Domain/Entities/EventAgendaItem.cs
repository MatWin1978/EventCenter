namespace EventCenter.Web.Domain.Entities;

public class EventAgendaItem
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDateTimeUtc { get; set; }
    public DateTime EndDateTimeUtc { get; set; }
    public decimal CostForMakler { get; set; }
    public decimal CostForGuest { get; set; }
    public bool IsMandatory { get; set; }
    public int? MaxParticipants { get; set; }
    public bool MaklerCanParticipate { get; set; } = true;
    public bool GuestsCanParticipate { get; set; } = true;

    // Navigation properties
    public Event Event { get; set; } = null!;
}
