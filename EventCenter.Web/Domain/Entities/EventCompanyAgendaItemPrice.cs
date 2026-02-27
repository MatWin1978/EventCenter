namespace EventCenter.Web.Domain.Entities;

public class EventCompanyAgendaItemPrice
{
    public int EventCompanyId { get; set; }
    public int AgendaItemId { get; set; }
    public decimal? CustomPrice { get; set; }  // null = use default price

    // Navigation properties
    public EventCompany EventCompany { get; set; } = null!;
    public EventAgendaItem AgendaItem { get; set; } = null!;
}
