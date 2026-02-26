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

    // Navigation properties
    public Event Event { get; set; } = null!;
    public EventCompany? EventCompany { get; set; }
    public ICollection<EventOption> SelectedOptions { get; set; } = new List<EventOption>();
}
