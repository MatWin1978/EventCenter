using EventCenter.Web.Domain;
using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Domain.Enums;
using EventCenter.Web.Infrastructure.Email;
using EventCenter.Web.Services;
using EventCenter.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventCenter.Tests.Services;

public class RegistrationServiceTests : IDisposable
{
    private readonly EventCenterDbContext _context;
    private readonly Mock<IEmailSender> _mockEmailSender;
    private readonly Mock<ILogger<RegistrationService>> _mockLogger;
    private readonly RegistrationService _service;

    public RegistrationServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemory();
        _mockEmailSender = new Mock<IEmailSender>();
        _mockLogger = new Mock<ILogger<RegistrationService>>();
        _service = new RegistrationService(_context, _mockEmailSender.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task RegisterMakler_SuccessfulRegistration_ReturnsSuccessAndId()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(8), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(5), DateTimeKind.Utc),
            MaxCapacity = 10,
            MaxCompanions = 2,
            IsPublished = true
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        var agendaItem1 = new EventAgendaItem
        {
            EventId = evt.Id,
            Title = "Agenda 1",
            StartDateTimeUtc = evt.StartDateUtc,
            EndDateTimeUtc = evt.StartDateUtc.AddHours(1),
            CostForMakler = 50.00m,
            MaklerCanParticipate = true
        };
        var agendaItem2 = new EventAgendaItem
        {
            EventId = evt.Id,
            Title = "Agenda 2",
            StartDateTimeUtc = evt.StartDateUtc.AddHours(2),
            EndDateTimeUtc = evt.StartDateUtc.AddHours(3),
            CostForMakler = 75.00m,
            MaklerCanParticipate = true
        };
        _context.AgendaItems.AddRange(agendaItem1, agendaItem2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RegisterMaklerAsync(
            evt.Id,
            "makler@example.com",
            "Max",
            "Mustermann",
            "+491234567890",
            "Test Immobilien GmbH",
            new List<int> { agendaItem1.Id, agendaItem2.Id }
        );

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.RegistrationId);
        Assert.Null(result.ErrorMessage);

        var registration = await _context.Registrations.FindAsync(result.RegistrationId);
        Assert.NotNull(registration);
        Assert.Equal(RegistrationType.Makler, registration.RegistrationType);
    }

    [Fact]
    public async Task RegisterMakler_DeadlinePassed_ReturnsError()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(8), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-5), DateTimeKind.Utc),
            MaxCapacity = 10,
            MaxCompanions = 2,
            IsPublished = true
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RegisterMaklerAsync(
            evt.Id,
            "makler@example.com",
            "Max",
            "Mustermann",
            null,
            null,
            new List<int>()
        );

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.RegistrationId);
        Assert.Contains("Anmeldung nicht möglich", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterMakler_EventFull_ReturnsError()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(8), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(5), DateTimeKind.Utc),
            MaxCapacity = 1,
            MaxCompanions = 2,
            IsPublished = true
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        // Add existing registration
        var existingRegistration = new Registration
        {
            EventId = evt.Id,
            Email = "other@example.com",
            FirstName = "Other",
            LastName = "User",
            RegistrationDateUtc = DateTime.UtcNow,
            RegistrationType = RegistrationType.Makler,
            IsConfirmed = true
        };
        _context.Registrations.Add(existingRegistration);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RegisterMaklerAsync(
            evt.Id,
            "makler@example.com",
            "Max",
            "Mustermann",
            null,
            null,
            new List<int>()
        );

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.RegistrationId);
        Assert.Contains("ausgebucht", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterMakler_DuplicateRegistration_ReturnsError()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(8), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(5), DateTimeKind.Utc),
            MaxCapacity = 10,
            MaxCompanions = 2,
            IsPublished = true
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        // Add existing registration with same email
        var existingRegistration = new Registration
        {
            EventId = evt.Id,
            Email = "makler@example.com",
            FirstName = "Max",
            LastName = "Mustermann",
            RegistrationDateUtc = DateTime.UtcNow,
            RegistrationType = RegistrationType.Makler,
            IsConfirmed = true
        };
        _context.Registrations.Add(existingRegistration);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RegisterMaklerAsync(
            evt.Id,
            "makler@example.com",
            "Max",
            "Mustermann",
            null,
            null,
            new List<int>()
        );

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.RegistrationId);
        Assert.Contains("bereits", result.ErrorMessage);
        Assert.Contains("angemeldet", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterMakler_InvalidAgendaItems_ReturnsError()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(8), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(5), DateTimeKind.Utc),
            MaxCapacity = 10,
            MaxCompanions = 2,
            IsPublished = true
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        var agendaItem = new EventAgendaItem
        {
            EventId = evt.Id,
            Title = "Agenda 1",
            StartDateTimeUtc = evt.StartDateUtc,
            EndDateTimeUtc = evt.StartDateUtc.AddHours(1),
            CostForMakler = 50.00m,
            MaklerCanParticipate = false // Not allowed for makler
        };
        _context.AgendaItems.Add(agendaItem);
        await _context.SaveChangesAsync();

        // Act - try to register with invalid agenda item
        var result = await _service.RegisterMaklerAsync(
            evt.Id,
            "makler@example.com",
            "Max",
            "Mustermann",
            null,
            null,
            new List<int> { agendaItem.Id }
        );

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.RegistrationId);
        Assert.Contains("Ungültige", result.ErrorMessage);
        Assert.Contains("Agendapunkt", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterMakler_CreatesRegistrationAgendaItems()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(8), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(5), DateTimeKind.Utc),
            MaxCapacity = 10,
            MaxCompanions = 2,
            IsPublished = true
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        var agendaItem1 = new EventAgendaItem
        {
            EventId = evt.Id,
            Title = "Agenda 1",
            StartDateTimeUtc = evt.StartDateUtc,
            EndDateTimeUtc = evt.StartDateUtc.AddHours(1),
            MaklerCanParticipate = true
        };
        var agendaItem2 = new EventAgendaItem
        {
            EventId = evt.Id,
            Title = "Agenda 2",
            StartDateTimeUtc = evt.StartDateUtc.AddHours(2),
            EndDateTimeUtc = evt.StartDateUtc.AddHours(3),
            MaklerCanParticipate = true
        };
        _context.AgendaItems.AddRange(agendaItem1, agendaItem2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RegisterMaklerAsync(
            evt.Id,
            "makler@example.com",
            "Max",
            "Mustermann",
            null,
            null,
            new List<int> { agendaItem1.Id, agendaItem2.Id }
        );

        // Assert
        Assert.True(result.Success);

        var registrationAgendaItems = _context.Set<RegistrationAgendaItem>()
            .Where(rai => rai.RegistrationId == result.RegistrationId)
            .ToList();

        Assert.Equal(2, registrationAgendaItems.Count);
        Assert.Contains(registrationAgendaItems, rai => rai.AgendaItemId == agendaItem1.Id);
        Assert.Contains(registrationAgendaItems, rai => rai.AgendaItemId == agendaItem2.Id);
    }

    [Fact]
    public async Task RegisterMakler_SetsCorrectFields()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(8), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(5), DateTimeKind.Utc),
            MaxCapacity = 10,
            MaxCompanions = 2,
            IsPublished = true
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        // Act
        var beforeRegistration = DateTime.UtcNow;
        var result = await _service.RegisterMaklerAsync(
            evt.Id,
            "makler@example.com",
            "Max",
            "Mustermann",
            "+491234567890",
            "Test Immobilien GmbH",
            new List<int>()
        );
        var afterRegistration = DateTime.UtcNow;

        // Assert
        Assert.True(result.Success);

        var registration = await _context.Registrations.FindAsync(result.RegistrationId);
        Assert.NotNull(registration);
        Assert.Equal(RegistrationType.Makler, registration.RegistrationType);
        Assert.True(registration.IsConfirmed);
        Assert.Equal("Max", registration.FirstName);
        Assert.Equal("Mustermann", registration.LastName);
        Assert.Equal("makler@example.com", registration.Email);
        Assert.Equal("+491234567890", registration.Phone);
        Assert.Equal("Test Immobilien GmbH", registration.Company);
        Assert.InRange(registration.RegistrationDateUtc, beforeRegistration.AddSeconds(-1), afterRegistration.AddSeconds(1));
    }

    [Fact]
    public async Task RegisterMakler_CallsEmailSender()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(8), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(5), DateTimeKind.Utc),
            MaxCapacity = 10,
            MaxCompanions = 2,
            IsPublished = true
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RegisterMaklerAsync(
            evt.Id,
            "makler@example.com",
            "Max",
            "Mustermann",
            null,
            null,
            new List<int>()
        );

        // Assert
        Assert.True(result.Success);

        // Verify email sender was called (with small delay for fire-and-forget)
        await Task.Delay(500);
        _mockEmailSender.Verify(
            x => x.SendRegistrationConfirmationAsync(It.IsAny<Registration>()),
            Times.Once
        );
    }
}
