using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Domain.Enums;
using EventCenter.Web.Domain.Extensions;
using Xunit;

namespace EventCenter.Tests;

public class EventStateCalculationTests
{
    [Fact]
    public void NotPublished_WhenIsPublishedFalse()
    {
        // Arrange
        var evt = new Event
        {
            IsPublished = false,
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(8), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(5), DateTimeKind.Utc)
        };

        // Act
        var state = evt.GetCurrentState();

        // Assert
        Assert.Equal(EventState.NotPublished, state);
    }

    [Fact]
    public void Finished_WhenEndDateInPast()
    {
        // Arrange
        var evt = new Event
        {
            IsPublished = true,
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-3), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-5), DateTimeKind.Utc)
        };

        // Act
        var state = evt.GetCurrentState();

        // Assert
        Assert.Equal(EventState.Finished, state);
    }

    [Fact]
    public void DeadlineReached_WhenDeadlineEndOfDayPassed()
    {
        // Arrange - event with deadline yesterday (end-of-day has passed)
        var evt = new Event
        {
            IsPublished = true,
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(2), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(3), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1), DateTimeKind.Utc)
        };

        // Act
        var state = evt.GetCurrentState();

        // Assert
        Assert.Equal(EventState.DeadlineReached, state);
    }

    [Fact]
    public void Public_WhenPublishedAndDeadlineNotPassed()
    {
        // Arrange
        var evt = new Event
        {
            IsPublished = true,
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(8), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(5), DateTimeKind.Utc)
        };

        // Act
        var state = evt.GetCurrentState();

        // Assert
        Assert.Equal(EventState.Public, state);
    }

    [Fact]
    public void GetCurrentRegistrationCount_ReturnsCount()
    {
        // Arrange
        var evt = new Event
        {
            Registrations = new List<Registration>
            {
                new Registration { Id = 1 },
                new Registration { Id = 2 },
                new Registration { Id = 3 }
            }
        };

        // Act
        var count = evt.GetCurrentRegistrationCount();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void GetCurrentRegistrationCount_ReturnsZero_WhenNull()
    {
        // Arrange
        var evt = new Event
        {
            Registrations = null!
        };

        // Act
        var count = evt.GetCurrentRegistrationCount();

        // Assert
        Assert.Equal(0, count);
    }
}
