namespace EventCenter.Web.Models;

public class RegistrationFormModel
{
    public int EventId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public List<int> SelectedAgendaItemIds { get; set; } = new();
}
