using EventCenter.Web.Domain.Enums;

namespace EventCenter.Web.Domain.Entities;

public class Registration
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int? EventCompanyId { get; set; }
    public RegistrationType RegistrationType { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public DateTime RegistrationDateUtc { get; set; }
    public bool IsConfirmed { get; set; }
    public int NumberOfCompanions { get; set; }
    public string? SpecialRequirements { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancellationDateUtc { get; set; }

    // Guest-specific fields (Phase 6)
    public int? ParentRegistrationId { get; set; }  // NULL for broker/company, FK to broker's registration for guest
    public string? Salutation { get; set; }         // "Herr", "Frau", "Divers"
    public string? RelationshipType { get; set; }   // Free text: "Ehepartner", "Kollege", etc.

    // Navigation properties
    public Event Event { get; set; } = null!;
    public EventCompany? EventCompany { get; set; }
    public ICollection<EventOption> SelectedOptions { get; set; } = new List<EventOption>();
    public ICollection<RegistrationAgendaItem> RegistrationAgendaItems { get; set; } = new List<RegistrationAgendaItem>();
    public Registration? ParentRegistration { get; set; }
    public ICollection<Registration> GuestRegistrations { get; set; } = new List<Registration>();
}
