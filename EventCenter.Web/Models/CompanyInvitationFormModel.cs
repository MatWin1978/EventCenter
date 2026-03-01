namespace EventCenter.Web.Models;

public class CompanyInvitationFormModel
{
    public int EventId { get; set; }
    public int? CompanyId { get; set; }        // FK to Company address book (required for new invitations)
    public string CompanyName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public decimal? PercentageDiscount { get; set; }  // Applied to all items first
    public string? PersonalMessage { get; set; }
    public bool SendImmediately { get; set; }         // false = save draft, true = create & send
    public List<CompanyAgendaItemPriceModel> AgendaItemPrices { get; set; } = new();
}
