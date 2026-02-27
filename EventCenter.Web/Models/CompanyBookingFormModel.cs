namespace EventCenter.Web.Models;

public class CompanyBookingFormModel
{
    public int EventCompanyId { get; set; }
    public List<ParticipantModel> Participants { get; set; } = new() { new ParticipantModel() };
    public List<int> SelectedExtraOptionIds { get; set; } = new();
}

public class ParticipantModel
{
    public string Salutation { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<int> SelectedAgendaItemIds { get; set; } = new();
}
