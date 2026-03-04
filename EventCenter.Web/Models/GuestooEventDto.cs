namespace EventCenter.Web.Models;

public record GuestooEventLocation(
    string? Street,
    string? StreetNumber,
    string? PostCode,
    string? City,
    string? Country);

public record GuestooEventDto(
    string? Title,
    string? Subtitle,
    GuestooEventLocation? Location,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    long? AvailableSeats,
    string? ImageUrl,
    string? ShortDescription,
    string? EventLink);
