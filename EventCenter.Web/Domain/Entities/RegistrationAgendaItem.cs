namespace EventCenter.Web.Domain.Entities;

public class RegistrationAgendaItem
{
    public int RegistrationId { get; set; }
    public int AgendaItemId { get; set; }

    // Navigation properties
    public Registration Registration { get; set; } = null!;
    public EventAgendaItem AgendaItem { get; set; } = null!;
}
