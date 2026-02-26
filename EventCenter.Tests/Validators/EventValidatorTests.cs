using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Validators;
using Xunit;

namespace EventCenter.Tests.Validators;

public class EventValidatorTests
{
    private readonly EventValidator _validator = new();

    [Fact]
    public void ContactEmail_InvalidFormat_Fails()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.UtcNow.AddDays(1),
            EndDateUtc = DateTime.UtcNow.AddDays(2),
            RegistrationDeadlineUtc = DateTime.UtcNow,
            MaxCapacity = 100,
            MaxCompanions = 2,
            ContactEmail = "invalid-email"
        };

        // Act
        var result = _validator.Validate(evt);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ContactEmail");
    }

    [Fact]
    public void ContactPhone_TooLong_Fails()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.UtcNow.AddDays(1),
            EndDateUtc = DateTime.UtcNow.AddDays(2),
            RegistrationDeadlineUtc = DateTime.UtcNow,
            MaxCapacity = 100,
            MaxCompanions = 2,
            ContactPhone = new string('1', 51) // 51 characters
        };

        // Act
        var result = _validator.Validate(evt);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ContactPhone");
    }

    [Fact]
    public void AgendaItems_NestedValidation_Fails()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.UtcNow.AddDays(1),
            EndDateUtc = DateTime.UtcNow.AddDays(2),
            RegistrationDeadlineUtc = DateTime.UtcNow,
            MaxCapacity = 100,
            MaxCompanions = 2,
            AgendaItems = new List<EventAgendaItem>
            {
                new EventAgendaItem
                {
                    Title = "", // Invalid - empty title
                    StartDateTimeUtc = DateTime.UtcNow,
                    EndDateTimeUtc = DateTime.UtcNow.AddHours(1)
                }
            }
        };

        // Act
        var result = _validator.Validate(evt);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("AgendaItems"));
    }
}
