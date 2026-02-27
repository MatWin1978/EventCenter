namespace EventCenter.Web.Models;

public class CompanyAgendaItemPriceModel
{
    public int AgendaItemId { get; set; }
    public string AgendaItemTitle { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }           // Read-only reference (CostForMakler from EventAgendaItem)
    public decimal? ManualOverride { get; set; }     // Admin can override individual item price
}
