namespace EventCenter.Web.Domain.Entities;

public class EventOption
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int? MaxQuantity { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
