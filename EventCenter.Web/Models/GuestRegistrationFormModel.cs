namespace EventCenter.Web.Models;

public class GuestRegistrationFormModel
{
    public string? Salutation { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? RelationshipType { get; set; }
    public List<int> SelectedAgendaItemIds { get; set; } = new();
}
