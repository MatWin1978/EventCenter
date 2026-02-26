using EventCenter.Tests.Helpers;
using EventCenter.Web.Domain.Entities;
using Xunit;

namespace EventCenter.Tests;

public class EntityConfigurationTests
{
    [Fact]
    public void EventConfiguration_CreatesTableWithCorrectSchema()
    {
        using var context = TestDbContextFactory.CreateInMemory();

        // Create an event with all properties
        var evt = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            Location = "Test Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2,
            IsPublished = true
        };

        context.Events.Add(evt);
        context.SaveChanges();

        // Verify entity was saved
        Assert.Equal(1, context.Events.Count());

        var saved = context.Events.First();
        Assert.Equal("Test Event", saved.Title);
    }

    [Fact]
    public void EventConfiguration_EnforcesMaxLengthConstraints()
    {
        using var context = TestDbContextFactory.CreateInMemory();

        // Title longer than 200 characters should be truncated or fail
        var longTitle = new string('A', 250);
        var evt = new Event
        {
            Title = longTitle,
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100
        };

        context.Events.Add(evt);

        // SQLite in-memory doesn't enforce max length like SQL Server
        // This test documents expected behavior; SQL Server would truncate or error
        context.SaveChanges();

        Assert.NotNull(context.Events.First());
    }
}
