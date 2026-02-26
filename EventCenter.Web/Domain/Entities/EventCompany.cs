namespace EventCenter.Web.Domain.Entities;

public class EventCompany
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public decimal? PricePerPerson { get; set; }
    public int? MaxParticipants { get; set; }
    public string? InvitationCode { get; set; }
    public DateTime? InvitationSentUtc { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
