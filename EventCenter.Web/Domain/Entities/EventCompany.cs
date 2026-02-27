using EventCenter.Web.Domain.Enums;

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

    // Phase 04 fields
    public InvitationStatus Status { get; set; } = InvitationStatus.Draft;
    public decimal? PercentageDiscount { get; set; }
    public string? PersonalMessage { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    public ICollection<EventCompanyAgendaItemPrice> AgendaItemPrices { get; set; } = new List<EventCompanyAgendaItemPrice>();
}
