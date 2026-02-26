using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Validators;
using Xunit;

namespace EventCenter.Tests.Validators;

public class EventAgendaItemValidatorTests
{
    private readonly EventAgendaItemValidator _validator = new();

    [Fact]
    public void Title_Required()
    {
        // Arrange
        var item = new EventAgendaItem
        {
            Title = "",
            StartDateTimeUtc = DateTime.UtcNow,
            EndDateTimeUtc = DateTime.UtcNow.AddHours(1),
            CostForMakler = 0,
            CostForGuest = 0
        };

        // Act
        var result = _validator.Validate(item);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void EndDateTime_MustBeAfterStartDateTime()
    {
        // Arrange
        var item = new EventAgendaItem
        {
            Title = "Test",
            StartDateTimeUtc = DateTime.UtcNow.AddHours(2),
            EndDateTimeUtc = DateTime.UtcNow.AddHours(1),
            CostForMakler = 0,
            CostForGuest = 0
        };

        // Act
        var result = _validator.Validate(item);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "EndDateTimeUtc");
    }

    [Fact]
    public void CostForMakler_NotNegative()
    {
        // Arrange
        var item = new EventAgendaItem
        {
            Title = "Test",
            StartDateTimeUtc = DateTime.UtcNow,
            EndDateTimeUtc = DateTime.UtcNow.AddHours(1),
            CostForMakler = -10,
            CostForGuest = 0
        };

        // Act
        var result = _validator.Validate(item);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CostForMakler");
    }

    [Fact]
    public void CostForGuest_NotNegative()
    {
        // Arrange
        var item = new EventAgendaItem
        {
            Title = "Test",
            StartDateTimeUtc = DateTime.UtcNow,
            EndDateTimeUtc = DateTime.UtcNow.AddHours(1),
            CostForMakler = 0,
            CostForGuest = -5
        };

        // Act
        var result = _validator.Validate(item);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CostForGuest");
    }

    [Fact]
    public void ValidItem_Passes()
    {
        // Arrange
        var item = new EventAgendaItem
        {
            Title = "Valid Agenda Item",
            StartDateTimeUtc = DateTime.UtcNow,
            EndDateTimeUtc = DateTime.UtcNow.AddHours(1),
            CostForMakler = 10,
            CostForGuest = 15
        };

        // Act
        var result = _validator.Validate(item);

        // Assert
        Assert.True(result.IsValid);
    }
}
