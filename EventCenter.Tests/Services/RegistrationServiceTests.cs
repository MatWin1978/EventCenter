using EventCenter.Web.Domain;
using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Domain.Enums;
using EventCenter.Web.Domain.Extensions;
using EventCenter.Web.Infrastructure.Email;
using EventCenter.Web.Services;
using EventCenter.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task RegisterGuestAsync_ValidGuest_ReturnsSuccess()
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
            CostForGuest = 30.00m,
            MaklerCanParticipate = true,
            GuestsCanParticipate = true
        };
        _context.AgendaItems.Add(agendaItem);
        await _context.SaveChangesAsync();

        // Create broker registration
        var brokerResult = await _service.RegisterMaklerAsync(
            evt.Id,
            "broker@example.com",
            "Max",
            "Broker",
            null,
            null,
            new List<int> { agendaItem.Id }
        );

        // Act
        var guestResult = await _service.RegisterGuestAsync(
            brokerResult.RegistrationId!.Value,
            "Frau",
            "Anna",
            "Guest",
            "guest@example.com",
            "Ehepartner",
            new List<int> { agendaItem.Id }
        );

        // Assert
        Assert.True(guestResult.Success, $"Guest registration failed: {guestResult.ErrorMessage}");
        Assert.NotNull(guestResult.GuestRegistrationId);
        Assert.Null(guestResult.ErrorMessage);

        var guestRegistration = await _context.Registrations.FindAsync(guestResult.GuestRegistrationId);
        Assert.NotNull(guestRegistration);
        Assert.Equal(RegistrationType.Guest, guestRegistration.RegistrationType);
        Assert.Equal(brokerResult.RegistrationId, guestRegistration.ParentRegistrationId);
        Assert.Equal("Frau", guestRegistration.Salutation);
        Assert.Equal("Ehepartner", guestRegistration.RelationshipType);
    }

    [Fact]
    public async Task RegisterGuestAsync_LimitReached_ReturnsError()
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
            MaxCompanions = 1,
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
            CostForGuest = 30.00m,
            MaklerCanParticipate = true,
            GuestsCanParticipate = true
        };
        _context.AgendaItems.Add(agendaItem);
        await _context.SaveChangesAsync();

        // Create broker registration
        var brokerResult = await _service.RegisterMaklerAsync(
            evt.Id,
            "broker@example.com",
            "Max",
            "Broker",
            null,
            null,
            new List<int> { agendaItem.Id }
        );

        // Register first guest (should succeed)
        var firstGuestResult = await _service.RegisterGuestAsync(
            brokerResult.RegistrationId!.Value,
            "Frau",
            "Anna",
            "Guest1",
            "guest1@example.com",
            "Ehepartner",
            new List<int> { agendaItem.Id }
        );
        Assert.True(firstGuestResult.Success);

        // Act - Register second guest (should fail due to limit)
        var secondGuestResult = await _service.RegisterGuestAsync(
            brokerResult.RegistrationId!.Value,
            "Herr",
            "Peter",
            "Guest2",
            "guest2@example.com",
            "Kollege",
            new List<int> { agendaItem.Id }
        );

        // Assert
        Assert.False(secondGuestResult.Success);
        Assert.Null(secondGuestResult.GuestRegistrationId);
        Assert.Contains("Maximale Anzahl", secondGuestResult.ErrorMessage);
    }

    [Fact]
    public async Task RegisterGuestAsync_BrokerNotFound_ReturnsError()
    {
        // Act
        var result = await _service.RegisterGuestAsync(
            999,
            "Frau",
            "Anna",
            "Guest",
            "guest@example.com",
            "Ehepartner",
            new List<int> { 1 }
        );

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.GuestRegistrationId);
        Assert.Contains("nicht gefunden", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterGuestAsync_NonMaklerParent_ReturnsError()
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

        // Create a company participant registration
        var companyReg = new Registration
        {
            EventId = evt.Id,
            RegistrationType = RegistrationType.CompanyParticipant,
            FirstName = "Company",
            LastName = "User",
            Email = "company@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true
        };
        _context.Registrations.Add(companyReg);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RegisterGuestAsync(
            companyReg.Id,
            "Frau",
            "Anna",
            "Guest",
            "guest@example.com",
            "Ehepartner",
            new List<int> { 1 }
        );

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Nur Makler", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterGuestAsync_DeadlinePassed_ReturnsError()
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

        // Create broker registration
        var brokerReg = new Registration
        {
            EventId = evt.Id,
            RegistrationType = RegistrationType.Makler,
            FirstName = "Max",
            LastName = "Broker",
            Email = "broker@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true
        };
        _context.Registrations.Add(brokerReg);
        await _context.SaveChangesAsync();

        // Modify event to have past deadline
        evt.RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1), DateTimeKind.Utc);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RegisterGuestAsync(
            brokerReg.Id,
            "Frau",
            "Anna",
            "Guest",
            "guest@example.com",
            "Ehepartner",
            new List<int> { 1 }
        );

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Anmeldung nicht möglich", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterGuestAsync_InvalidAgendaItems_ReturnsError()
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
            CostForGuest = 30.00m,
            MaklerCanParticipate = true,
            GuestsCanParticipate = false  // Guests cannot participate
        };
        _context.AgendaItems.Add(agendaItem);
        await _context.SaveChangesAsync();

        // Create broker registration
        var brokerResult = await _service.RegisterMaklerAsync(
            evt.Id,
            "broker@example.com",
            "Max",
            "Broker",
            null,
            null,
            new List<int> { agendaItem.Id }
        );

        // Act
        var result = await _service.RegisterGuestAsync(
            brokerResult.RegistrationId!.Value,
            "Frau",
            "Anna",
            "Guest",
            "guest@example.com",
            "Ehepartner",
            new List<int> { agendaItem.Id }  // Try to select item where GuestsCanParticipate = false
        );

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Ungültige Agendapunkt-Auswahl", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterGuestAsync_CreatesRegistrationAgendaItems()
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
            CostForGuest = 30.00m,
            MaklerCanParticipate = true,
            GuestsCanParticipate = true
        };
        var agendaItem2 = new EventAgendaItem
        {
            EventId = evt.Id,
            Title = "Agenda 2",
            StartDateTimeUtc = evt.StartDateUtc.AddHours(2),
            EndDateTimeUtc = evt.StartDateUtc.AddHours(3),
            CostForMakler = 60.00m,
            CostForGuest = 40.00m,
            MaklerCanParticipate = true,
            GuestsCanParticipate = true
        };
        _context.AgendaItems.AddRange(agendaItem1, agendaItem2);
        await _context.SaveChangesAsync();

        // Create broker registration
        var brokerResult = await _service.RegisterMaklerAsync(
            evt.Id,
            "broker@example.com",
            "Max",
            "Broker",
            null,
            null,
            new List<int> { agendaItem1.Id }
        );

        // Act
        var guestResult = await _service.RegisterGuestAsync(
            brokerResult.RegistrationId!.Value,
            "Frau",
            "Anna",
            "Guest",
            "guest@example.com",
            "Ehepartner",
            new List<int> { agendaItem1.Id, agendaItem2.Id }
        );

        // Assert
        Assert.True(guestResult.Success, $"Guest registration failed: {guestResult.ErrorMessage}");

        // Small delay to allow fire-and-forget email to complete
        await Task.Delay(100);

        var registrationAgendaItems = await _context.Set<RegistrationAgendaItem>()
            .Where(rai => rai.RegistrationId == guestResult.GuestRegistrationId!.Value)
            .ToListAsync();

        Assert.Equal(2, registrationAgendaItems.Count);
        Assert.Contains(registrationAgendaItems, rai => rai.AgendaItemId == agendaItem1.Id);
        Assert.Contains(registrationAgendaItems, rai => rai.AgendaItemId == agendaItem2.Id);
    }

    [Fact]
    public async Task RegisterGuestAsync_SetsCorrectFields()
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
            CostForGuest = 30.00m,
            MaklerCanParticipate = true,
            GuestsCanParticipate = true
        };
        _context.AgendaItems.Add(agendaItem);
        await _context.SaveChangesAsync();

        // Create broker registration
        var brokerResult = await _service.RegisterMaklerAsync(
            evt.Id,
            "broker@example.com",
            "Max",
            "Broker",
            null,
            null,
            new List<int> { agendaItem.Id }
        );

        // Act
        var guestResult = await _service.RegisterGuestAsync(
            brokerResult.RegistrationId!.Value,
            "Divers",
            "Alex",
            "Guest",
            "alex@example.com",
            "Freund",
            new List<int> { agendaItem.Id }
        );

        // Assert
        Assert.True(guestResult.Success, $"Guest registration failed: {guestResult.ErrorMessage}");

        var guestRegistration = await _context.Registrations.FindAsync(guestResult.GuestRegistrationId);
        Assert.NotNull(guestRegistration);
        Assert.Equal("Divers", guestRegistration.Salutation);
        Assert.Equal("Alex", guestRegistration.FirstName);
        Assert.Equal("Guest", guestRegistration.LastName);
        Assert.Equal("alex@example.com", guestRegistration.Email);
        Assert.Equal("Freund", guestRegistration.RelationshipType);
        Assert.Equal(brokerResult.RegistrationId, guestRegistration.ParentRegistrationId);
        Assert.Equal(RegistrationType.Guest, guestRegistration.RegistrationType);
        Assert.True(guestRegistration.IsConfirmed);
    }

    [Fact]
    public async Task RegisterGuestAsync_Success_SendsEmail()
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
            CostForGuest = 30.00m,
            MaklerCanParticipate = true,
            GuestsCanParticipate = true
        };
        _context.AgendaItems.Add(agendaItem);
        await _context.SaveChangesAsync();

        // Create broker registration
        var brokerResult = await _service.RegisterMaklerAsync(
            evt.Id,
            "broker@example.com",
            "Max",
            "Broker",
            null,
            null,
            new List<int> { agendaItem.Id }
        );

        // Act
        var guestResult = await _service.RegisterGuestAsync(
            brokerResult.RegistrationId!.Value,
            "Frau",
            "Anna",
            "Guest",
            "guest@example.com",
            "Ehepartner",
            new List<int> { agendaItem.Id }
        );

        // Assert
        Assert.True(guestResult.Success, $"Guest registration failed: {guestResult.ErrorMessage}");

        // Verify email sender was called (with small delay for fire-and-forget)
        await Task.Delay(500);
        _mockEmailSender.Verify(
            x => x.SendGuestRegistrationConfirmationAsync(It.IsAny<Registration>(), It.IsAny<Registration>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetGuestCountAsync_ReturnsCorrectCount()
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

        // Create broker registration
        var brokerReg = new Registration
        {
            EventId = evt.Id,
            RegistrationType = RegistrationType.Makler,
            FirstName = "Max",
            LastName = "Broker",
            Email = "broker@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true
        };
        _context.Registrations.Add(brokerReg);
        await _context.SaveChangesAsync();

        // Create active guest
        var activeGuest = new Registration
        {
            EventId = evt.Id,
            ParentRegistrationId = brokerReg.Id,
            RegistrationType = RegistrationType.Guest,
            FirstName = "Anna",
            LastName = "Guest1",
            Email = "guest1@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true,
            IsCancelled = false
        };

        // Create cancelled guest
        var cancelledGuest = new Registration
        {
            EventId = evt.Id,
            ParentRegistrationId = brokerReg.Id,
            RegistrationType = RegistrationType.Guest,
            FirstName = "Peter",
            LastName = "Guest2",
            Email = "guest2@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true,
            IsCancelled = true
        };

        _context.Registrations.AddRange(activeGuest, cancelledGuest);
        await _context.SaveChangesAsync();

        // Act
        var count = await _service.GetGuestCountAsync(brokerReg.Id);

        // Assert
        Assert.Equal(1, count); // Only active guest counted
    }

    [Fact]
    public async Task CancelRegistration_OwnRegistration_ReturnsSuccess()
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

        var registration = new Registration
        {
            EventId = evt.Id,
            RegistrationType = RegistrationType.Makler,
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "makler@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true,
            IsCancelled = false
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelRegistrationAsync(registration.Id, "makler@example.com", "Termin kollidiert");

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);

        var updated = await _context.Registrations.FindAsync(registration.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsCancelled);
        Assert.NotNull(updated.CancellationDateUtc);
        Assert.Equal("Termin kollidiert", updated.CancellationReason);
    }

    [Fact]
    public async Task CancelRegistration_GuestRegistration_ByBroker_ReturnsSuccess()
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

        var brokerRegistration = new Registration
        {
            EventId = evt.Id,
            RegistrationType = RegistrationType.Makler,
            FirstName = "Max",
            LastName = "Broker",
            Email = "broker@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true,
            IsCancelled = false
        };
        _context.Registrations.Add(brokerRegistration);
        await _context.SaveChangesAsync();

        var guestRegistration = new Registration
        {
            EventId = evt.Id,
            ParentRegistrationId = brokerRegistration.Id,
            RegistrationType = RegistrationType.Guest,
            FirstName = "Anna",
            LastName = "Guest",
            Email = "guest@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true,
            IsCancelled = false
        };
        _context.Registrations.Add(guestRegistration);
        await _context.SaveChangesAsync();

        // Act - cancel guest using broker's email
        var result = await _service.CancelRegistrationAsync(guestRegistration.Id, "broker@example.com", null);

        // Assert
        Assert.True(result.Success, $"Expected success but got error: {result.ErrorMessage}");
        Assert.Null(result.ErrorMessage);

        var updated = await _context.Registrations.FindAsync(guestRegistration.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsCancelled);
    }

    [Fact]
    public async Task CancelRegistration_NotOwner_ReturnsError()
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

        var registration = new Registration
        {
            EventId = evt.Id,
            RegistrationType = RegistrationType.Makler,
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "owner@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true,
            IsCancelled = false
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act - try to cancel with different email
        var result = await _service.CancelRegistrationAsync(registration.Id, "other@example.com", null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Keine Berechtigung zum Stornieren dieser Anmeldung.", result.ErrorMessage);
    }

    [Fact]
    public async Task CancelRegistration_DeadlinePassed_ReturnsError()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(8), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-3), DateTimeKind.Utc),
            MaxCapacity = 10,
            MaxCompanions = 2,
            IsPublished = true
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        var registration = new Registration
        {
            EventId = evt.Id,
            RegistrationType = RegistrationType.Makler,
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "makler@example.com",
            RegistrationDateUtc = DateTime.UtcNow.AddDays(-5),
            IsConfirmed = true,
            IsCancelled = false
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelRegistrationAsync(registration.Id, "makler@example.com", null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Stornierung nach Anmeldefrist nicht möglich.", result.ErrorMessage);
    }

    [Fact]
    public async Task CancelRegistration_AlreadyCancelled_ReturnsError()
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

        var registration = new Registration
        {
            EventId = evt.Id,
            RegistrationType = RegistrationType.Makler,
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "makler@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true,
            IsCancelled = true,
            CancellationDateUtc = DateTime.UtcNow.AddDays(-1)
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelRegistrationAsync(registration.Id, "makler@example.com", null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Anmeldung ist bereits storniert.", result.ErrorMessage);
    }

    [Fact]
    public async Task CancelRegistration_NotFound_ReturnsError()
    {
        // Act
        var result = await _service.CancelRegistrationAsync(99999, "any@example.com", null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Anmeldung nicht gefunden.", result.ErrorMessage);
    }

    [Fact]
    public async Task CancelRegistration_DoesNotCancelGuestRegistrations()
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

        var brokerRegistration = new Registration
        {
            EventId = evt.Id,
            RegistrationType = RegistrationType.Makler,
            FirstName = "Max",
            LastName = "Broker",
            Email = "broker@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true,
            IsCancelled = false
        };
        _context.Registrations.Add(brokerRegistration);
        await _context.SaveChangesAsync();

        var guest1 = new Registration
        {
            EventId = evt.Id,
            ParentRegistrationId = brokerRegistration.Id,
            RegistrationType = RegistrationType.Guest,
            FirstName = "Anna",
            LastName = "Guest1",
            Email = "guest1@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true,
            IsCancelled = false
        };
        var guest2 = new Registration
        {
            EventId = evt.Id,
            ParentRegistrationId = brokerRegistration.Id,
            RegistrationType = RegistrationType.Guest,
            FirstName = "Peter",
            LastName = "Guest2",
            Email = "guest2@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true,
            IsCancelled = false
        };
        _context.Registrations.AddRange(guest1, guest2);
        await _context.SaveChangesAsync();

        // Act - cancel broker registration
        var result = await _service.CancelRegistrationAsync(brokerRegistration.Id, "broker@example.com", null);

        // Assert
        Assert.True(result.Success);

        var updatedBroker = await _context.Registrations.FindAsync(brokerRegistration.Id);
        Assert.NotNull(updatedBroker);
        Assert.True(updatedBroker.IsCancelled);

        var updatedGuest1 = await _context.Registrations.FindAsync(guest1.Id);
        Assert.NotNull(updatedGuest1);
        Assert.False(updatedGuest1.IsCancelled, "Guest 1 should NOT be cancelled when broker cancels");

        var updatedGuest2 = await _context.Registrations.FindAsync(guest2.Id);
        Assert.NotNull(updatedGuest2);
        Assert.False(updatedGuest2.IsCancelled, "Guest 2 should NOT be cancelled when broker cancels");
    }

    [Fact]
    public async Task CancelRegistration_UpdatesRegistrationCount()
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

        var reg1 = new Registration
        {
            EventId = evt.Id,
            RegistrationType = RegistrationType.Makler,
            FirstName = "Max",
            LastName = "One",
            Email = "reg1@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true,
            IsCancelled = false
        };
        var reg2 = new Registration
        {
            EventId = evt.Id,
            RegistrationType = RegistrationType.Makler,
            FirstName = "Peter",
            LastName = "Two",
            Email = "reg2@example.com",
            RegistrationDateUtc = DateTime.UtcNow,
            IsConfirmed = true,
            IsCancelled = false
        };
        _context.Registrations.AddRange(reg1, reg2);
        await _context.SaveChangesAsync();

        // Cancel one registration
        var cancelResult = await _service.CancelRegistrationAsync(reg1.Id, "reg1@example.com", null);
        Assert.True(cancelResult.Success);

        // Load event with registrations to check count
        var evtWithRegs = await _context.Events
            .Include(e => e.Registrations)
            .FirstAsync(e => e.Id == evt.Id);

        // Assert count is 1 (not 2)
        Assert.Equal(1, evtWithRegs.GetCurrentRegistrationCount());
    }

    [Fact]
    public async Task GetGuestRegistrationsAsync_ReturnsGuestsWithDetails()
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
            CostForGuest = 30.00m,
            MaklerCanParticipate = true,
            GuestsCanParticipate = true
        };
        _context.AgendaItems.Add(agendaItem);
        await _context.SaveChangesAsync();

        // Create broker registration
        var brokerResult = await _service.RegisterMaklerAsync(
            evt.Id,
            "broker@example.com",
            "Max",
            "Broker",
            null,
            null,
            new List<int> { agendaItem.Id }
        );

        // Create two guests
        var guest1Result = await _service.RegisterGuestAsync(
            brokerResult.RegistrationId!.Value,
            "Frau",
            "Anna",
            "Guest1",
            "guest1@example.com",
            "Ehepartner",
            new List<int> { agendaItem.Id }
        );

        var guest2Result = await _service.RegisterGuestAsync(
            brokerResult.RegistrationId!.Value,
            "Herr",
            "Peter",
            "Guest2",
            "guest2@example.com",
            "Kollege",
            new List<int> { agendaItem.Id }
        );

        Assert.True(guest1Result.Success, $"Guest 1 registration failed: {guest1Result.ErrorMessage}");
        Assert.True(guest2Result.Success, $"Guest 2 registration failed: {guest2Result.ErrorMessage}");

        // Act
        var guests = await _service.GetGuestRegistrationsAsync(brokerResult.RegistrationId!.Value);

        // Assert
        Assert.Equal(2, guests.Count);
        Assert.All(guests, g => Assert.NotEmpty(g.RegistrationAgendaItems));
    }
}
