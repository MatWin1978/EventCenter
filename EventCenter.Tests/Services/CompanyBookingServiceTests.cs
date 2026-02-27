using EventCenter.Web.Domain;
using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Domain.Enums;
using EventCenter.Web.Infrastructure.Email;
using EventCenter.Web.Models;
using EventCenter.Web.Services;
using EventCenter.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventCenter.Tests.Services;

public class CompanyBookingServiceTests : IDisposable
{
    private readonly EventCenterDbContext _context;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<ILogger<CompanyBookingService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly CompanyBookingService _service;
    private readonly Event _testEvent;
    private readonly EventCompany _testInvitation;

    public CompanyBookingServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemory();
        _emailSenderMock = new Mock<IEmailSender>();
        _loggerMock = new Mock<ILogger<CompanyBookingService>>();
        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.Setup(c => c["BaseUrl"]).Returns("https://eventcenter.test");

        _service = new CompanyBookingService(
            _context,
            _emailSenderMock.Object,
            _loggerMock.Object,
            _configurationMock.Object);

        // Create test event with agenda items
        _testEvent = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            Location = "Test Location",
            StartDateUtc = DateTime.UtcNow.AddDays(30),
            EndDateUtc = DateTime.UtcNow.AddDays(30).AddHours(4),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(15),
            MaxCapacity = 100,
            MaxCompanions = 2,
            IsPublished = true
        };

        var agendaItem1 = new EventAgendaItem
        {
            Event = _testEvent,
            Title = "Workshop A",
            StartDateTimeUtc = DateTime.UtcNow.AddDays(30),
            EndDateTimeUtc = DateTime.UtcNow.AddDays(30).AddHours(2),
            CostForMakler = 100m,
            CostForGuest = 120m,
            IsMandatory = false
        };

        var agendaItem2 = new EventAgendaItem
        {
            Event = _testEvent,
            Title = "Workshop B",
            StartDateTimeUtc = DateTime.UtcNow.AddDays(30).AddHours(2),
            EndDateTimeUtc = DateTime.UtcNow.AddDays(30).AddHours(4),
            CostForMakler = 150m,
            CostForGuest = 180m,
            IsMandatory = false
        };

        _testEvent.AgendaItems.Add(agendaItem1);
        _testEvent.AgendaItems.Add(agendaItem2);

        var extraOption = new EventOption
        {
            Event = _testEvent,
            Name = "Lunch Package",
            Price = 25m
        };

        _testEvent.EventOptions.Add(extraOption);

        _context.Events.Add(_testEvent);
        _context.SaveChanges();

        // Create test invitation
        _testInvitation = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Test Company",
            ContactEmail = "test@company.com",
            ContactPhone = "123456789",
            InvitationCode = "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",
            Status = InvitationStatus.Sent,
            InvitationSentUtc = DateTime.UtcNow.AddDays(-5),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(10)
        };

        _context.EventCompanies.Add(_testInvitation);
        _context.SaveChanges();

        // Add custom pricing for test invitation
        _context.EventCompanyAgendaItemPrices.Add(new EventCompanyAgendaItemPrice
        {
            EventCompanyId = _testInvitation.Id,
            AgendaItemId = agendaItem1.Id,
            CustomPrice = 80m
        });

        _context.EventCompanyAgendaItemPrices.Add(new EventCompanyAgendaItemPrice
        {
            EventCompanyId = _testInvitation.Id,
            AgendaItemId = agendaItem2.Id,
            CustomPrice = 120m
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region ValidateInvitationCodeAsync Tests

    [Fact]
    public async Task ValidateInvitationCodeAsync_ValidCodeNotExpired_ReturnsSuccessWithCompany()
    {
        // Arrange
        var code = _testInvitation.InvitationCode!;

        // Act
        var (isValid, company, errorMessage) = await _service.ValidateInvitationCodeAsync(code);

        // Assert
        Assert.True(isValid);
        Assert.NotNull(company);
        Assert.Equal(_testInvitation.Id, company.Id);
        Assert.Null(errorMessage);
        Assert.NotNull(company.Event);
        Assert.NotEmpty(company.Event.AgendaItems);
        Assert.NotEmpty(company.Event.EventOptions);
    }

    [Fact]
    public async Task ValidateInvitationCodeAsync_InvalidCode_ReturnsFalseWithError()
    {
        // Arrange
        var invalidCode = "invalidcodeinvalidcodeinvalidcode";

        // Act
        var (isValid, company, errorMessage) = await _service.ValidateInvitationCodeAsync(invalidCode);

        // Assert
        Assert.False(isValid);
        Assert.Null(company);
        Assert.Equal("Dieser Link ist ungültig oder abgelaufen.", errorMessage);
    }

    [Fact]
    public async Task ValidateInvitationCodeAsync_ExpiredCode_ReturnsFalseWithExpirationError()
    {
        // Arrange
        _testInvitation.ExpiresAtUtc = DateTime.UtcNow.AddDays(-1);
        _context.SaveChanges();

        // Act
        var (isValid, company, errorMessage) = await _service.ValidateInvitationCodeAsync(_testInvitation.InvitationCode!);

        // Assert
        Assert.False(isValid);
        Assert.Null(company);
        Assert.Equal("Dieser Link ist abgelaufen. Bitte kontaktieren Sie uns für eine neue Einladung.", errorMessage);
    }

    [Fact]
    public async Task ValidateInvitationCodeAsync_DraftStatus_ReturnsFalseWithError()
    {
        // Arrange
        _testInvitation.Status = InvitationStatus.Draft;
        _context.SaveChanges();

        // Act
        var (isValid, company, errorMessage) = await _service.ValidateInvitationCodeAsync(_testInvitation.InvitationCode!);

        // Assert
        Assert.False(isValid);
        Assert.Null(company);
        Assert.Equal("Diese Einladung wurde noch nicht versendet.", errorMessage);
    }

    [Fact]
    public async Task ValidateInvitationCodeAsync_BookedStatus_ReturnsSuccessForStatusCheck()
    {
        // Arrange
        _testInvitation.Status = InvitationStatus.Booked;
        _context.SaveChanges();

        // Act
        var (isValid, company, errorMessage) = await _service.ValidateInvitationCodeAsync(_testInvitation.InvitationCode!);

        // Assert
        Assert.True(isValid);
        Assert.NotNull(company);
        Assert.Null(errorMessage);
    }

    [Fact]
    public async Task ValidateInvitationCodeAsync_CancelledStatus_ReturnsSuccessForStatusCheck()
    {
        // Arrange
        _testInvitation.Status = InvitationStatus.Cancelled;
        _context.SaveChanges();

        // Act
        var (isValid, company, errorMessage) = await _service.ValidateInvitationCodeAsync(_testInvitation.InvitationCode!);

        // Assert
        Assert.True(isValid);
        Assert.NotNull(company);
        Assert.Null(errorMessage);
    }

    #endregion

    #region SubmitBookingAsync Tests

    [Fact]
    public async Task SubmitBookingAsync_ValidSubmission_CreatesRegistrationsAndUpdatesStatus()
    {
        // Arrange
        var agendaItems = _testEvent.AgendaItems.ToList();
        var formModel = new CompanyBookingFormModel
        {
            EventCompanyId = _testInvitation.Id,
            Participants = new List<ParticipantModel>
            {
                new ParticipantModel
                {
                    Salutation = "Herr",
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@company.com",
                    SelectedAgendaItemIds = new List<int> { agendaItems[0].Id, agendaItems[1].Id }
                },
                new ParticipantModel
                {
                    Salutation = "Frau",
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@company.com",
                    SelectedAgendaItemIds = new List<int> { agendaItems[0].Id }
                }
            },
            SelectedExtraOptionIds = new List<int> { _testEvent.EventOptions.First().Id }
        };

        // Act
        var (success, errorMessage) = await _service.SubmitBookingAsync(_testInvitation.Id, formModel);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);

        // Verify invitation updated
        var updatedInvitation = await _context.EventCompanies.FindAsync(_testInvitation.Id);
        Assert.NotNull(updatedInvitation);
        Assert.Equal(InvitationStatus.Booked, updatedInvitation.Status);
        Assert.NotNull(updatedInvitation.BookingDateUtc);
        Assert.True(updatedInvitation.BookingDateUtc <= DateTime.UtcNow);

        // Verify registrations created
        var registrations = _context.Registrations
            .Where(r => r.EventCompanyId == _testInvitation.Id)
            .ToList();
        Assert.Equal(2, registrations.Count);

        var johnReg = registrations.First(r => r.FirstName == "John");
        Assert.Equal("Doe", johnReg.LastName);
        Assert.Equal("john.doe@company.com", johnReg.Email);
        Assert.Equal(RegistrationType.CompanyParticipant, johnReg.RegistrationType);
        Assert.True(johnReg.IsConfirmed);
        Assert.Equal(_testInvitation.Id, johnReg.EventCompanyId);

        // Verify agenda item links
        var johnAgendaItems = _context.RegistrationAgendaItems
            .Where(rai => rai.RegistrationId == johnReg.Id)
            .ToList();
        Assert.Equal(2, johnAgendaItems.Count);

        var janeReg = registrations.First(r => r.FirstName == "Jane");
        var janeAgendaItems = _context.RegistrationAgendaItems
            .Where(rai => rai.RegistrationId == janeReg.Id)
            .ToList();
        Assert.Single(janeAgendaItems);

        // Fire-and-forget email happens asynchronously, so we need to wait briefly
        await Task.Delay(100);

        // Verify email was called
        _emailSenderMock.Verify(
            e => e.SendAdminBookingNotificationAsync(
                It.IsAny<EventCompany>(),
                It.IsAny<Event>(),
                It.IsAny<List<ParticipantModel>>()),
            Times.Once());
    }

    [Fact]
    public async Task SubmitBookingAsync_InvalidInvitationId_ReturnsError()
    {
        // Arrange
        var formModel = new CompanyBookingFormModel
        {
            EventCompanyId = 99999,
            Participants = new List<ParticipantModel> { new ParticipantModel() }
        };

        // Act
        var (success, errorMessage) = await _service.SubmitBookingAsync(99999, formModel);

        // Assert
        Assert.False(success);
        Assert.NotNull(errorMessage);
    }

    [Fact]
    public async Task SubmitBookingAsync_AlreadyBooked_ReturnsError()
    {
        // Arrange
        _testInvitation.Status = InvitationStatus.Booked;
        _context.SaveChanges();

        var formModel = new CompanyBookingFormModel
        {
            EventCompanyId = _testInvitation.Id,
            Participants = new List<ParticipantModel> { new ParticipantModel() }
        };

        // Act
        var (success, errorMessage) = await _service.SubmitBookingAsync(_testInvitation.Id, formModel);

        // Assert
        Assert.False(success);
        Assert.Equal("Diese Einladung kann nicht mehr gebucht werden.", errorMessage);
    }

    #endregion

    #region CancelBookingAsync Tests

    [Fact]
    public async Task CancelBookingAsync_ValidBooking_CancelsAndMarksRegistrations()
    {
        // Arrange - First create a booking
        _testInvitation.Status = InvitationStatus.Booked;
        _testInvitation.BookingDateUtc = DateTime.UtcNow.AddDays(-1);

        var registration = new Registration
        {
            EventId = _testEvent.Id,
            EventCompanyId = _testInvitation.Id,
            RegistrationType = RegistrationType.CompanyParticipant,
            FirstName = "Test",
            LastName = "User",
            Email = "test@company.com",
            RegistrationDateUtc = DateTime.UtcNow.AddDays(-1),
            IsConfirmed = true
        };

        _context.Registrations.Add(registration);
        _context.SaveChanges();

        // Act
        var (success, errorMessage) = await _service.CancelBookingAsync(_testInvitation.Id, "Changed plans");

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);

        // Verify invitation updated
        var updatedInvitation = await _context.EventCompanies.FindAsync(_testInvitation.Id);
        Assert.NotNull(updatedInvitation);
        Assert.Equal(InvitationStatus.Cancelled, updatedInvitation.Status);
        Assert.Equal("Changed plans", updatedInvitation.CancellationComment);
        Assert.False(updatedInvitation.IsNonParticipation);

        // Verify registration cancelled
        var updatedReg = await _context.Registrations.FindAsync(registration.Id);
        Assert.NotNull(updatedReg);
        Assert.True(updatedReg.IsCancelled);
        Assert.NotNull(updatedReg.CancellationDateUtc);
    }

    [Fact]
    public async Task CancelBookingAsync_NotBooked_ReturnsError()
    {
        // Arrange
        _testInvitation.Status = InvitationStatus.Sent;
        _context.SaveChanges();

        // Act
        var (success, errorMessage) = await _service.CancelBookingAsync(_testInvitation.Id, "Cancel");

        // Assert
        Assert.False(success);
        Assert.Equal("Keine aktive Buchung vorhanden.", errorMessage);
    }

    #endregion

    #region ReportNonParticipationAsync Tests

    [Fact]
    public async Task ReportNonParticipationAsync_ValidBooking_MarksNonParticipation()
    {
        // Arrange - First create a booking
        _testInvitation.Status = InvitationStatus.Booked;
        _testInvitation.BookingDateUtc = DateTime.UtcNow.AddDays(-1);

        var registration = new Registration
        {
            EventId = _testEvent.Id,
            EventCompanyId = _testInvitation.Id,
            RegistrationType = RegistrationType.CompanyParticipant,
            FirstName = "Test",
            LastName = "User",
            Email = "test@company.com",
            RegistrationDateUtc = DateTime.UtcNow.AddDays(-1),
            IsConfirmed = true
        };

        _context.Registrations.Add(registration);
        _context.SaveChanges();

        // Act
        var (success, errorMessage) = await _service.ReportNonParticipationAsync(_testInvitation.Id, "No staff available");

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);

        // Verify invitation updated
        var updatedInvitation = await _context.EventCompanies.FindAsync(_testInvitation.Id);
        Assert.NotNull(updatedInvitation);
        Assert.Equal(InvitationStatus.Cancelled, updatedInvitation.Status);
        Assert.Equal("No staff available", updatedInvitation.CancellationComment);
        Assert.True(updatedInvitation.IsNonParticipation);

        // Verify registration cancelled
        var updatedReg = await _context.Registrations.FindAsync(registration.Id);
        Assert.NotNull(updatedReg);
        Assert.True(updatedReg.IsCancelled);
        Assert.NotNull(updatedReg.CancellationDateUtc);
    }

    [Fact]
    public async Task ReportNonParticipationAsync_NotBooked_ReturnsError()
    {
        // Arrange
        _testInvitation.Status = InvitationStatus.Sent;
        _context.SaveChanges();

        // Act
        var (success, errorMessage) = await _service.ReportNonParticipationAsync(_testInvitation.Id, "Cannot attend");

        // Assert
        Assert.False(success);
        Assert.Equal("Keine aktive Buchung vorhanden.", errorMessage);
    }

    #endregion

    #region GetBookingStatusAsync Tests

    [Fact]
    public async Task GetBookingStatusAsync_ExistingInvitation_ReturnsWithNavigationProperties()
    {
        // Arrange
        _testInvitation.Status = InvitationStatus.Booked;
        _context.SaveChanges();

        // Act
        var result = await _service.GetBookingStatusAsync(_testInvitation.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testInvitation.Id, result.Id);
        Assert.NotNull(result.Event);
    }

    [Fact]
    public async Task GetBookingStatusAsync_NonExistent_ReturnsNull()
    {
        // Act
        var result = await _service.GetBookingStatusAsync(99999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CalculateTotalCost Tests

    [Fact]
    public async Task CalculateTotalCost_WithCustomPricing_ReturnsCorrectTotal()
    {
        // Arrange
        var company = await _context.EventCompanies
            .Include(ec => ec.AgendaItemPrices)
            .FirstAsync(ec => ec.Id == _testInvitation.Id);

        var agendaItems = await _context.AgendaItems
            .Where(ai => ai.EventId == _testEvent.Id)
            .ToListAsync();

        var eventOptions = await _context.EventOptions
            .Where(eo => eo.EventId == _testEvent.Id)
            .ToListAsync();

        var formModel = new CompanyBookingFormModel
        {
            EventCompanyId = _testInvitation.Id,
            Participants = new List<ParticipantModel>
            {
                new ParticipantModel
                {
                    SelectedAgendaItemIds = new List<int> { agendaItems[0].Id, agendaItems[1].Id }
                },
                new ParticipantModel
                {
                    SelectedAgendaItemIds = new List<int> { agendaItems[0].Id }
                }
            },
            SelectedExtraOptionIds = new List<int> { eventOptions[0].Id }
        };

        // Act
        var total = _service.CalculateTotalCost(company, formModel, agendaItems, eventOptions);

        // Assert
        // Participant 1: 80 + 120 = 200
        // Participant 2: 80 = 80
        // Extra option: 25
        // Total: 305
        Assert.Equal(305m, total);
    }

    [Fact]
    public async Task CalculateTotalCost_NoCustomPricing_UsesBasePrices()
    {
        // Arrange - Create invitation without custom pricing
        var newInvitation = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "New Company",
            ContactEmail = "new@company.com",
            Status = InvitationStatus.Sent
        };

        _context.EventCompanies.Add(newInvitation);
        _context.SaveChanges();

        var agendaItems = await _context.AgendaItems
            .Where(ai => ai.EventId == _testEvent.Id)
            .ToListAsync();

        var eventOptions = await _context.EventOptions
            .Where(eo => eo.EventId == _testEvent.Id)
            .ToListAsync();

        var formModel = new CompanyBookingFormModel
        {
            EventCompanyId = newInvitation.Id,
            Participants = new List<ParticipantModel>
            {
                new ParticipantModel
                {
                    SelectedAgendaItemIds = new List<int> { agendaItems[0].Id }
                }
            },
            SelectedExtraOptionIds = new List<int>()
        };

        // Act
        var total = _service.CalculateTotalCost(newInvitation, formModel, agendaItems, eventOptions);

        // Assert
        // Participant 1: 100 (base price)
        // Total: 100
        Assert.Equal(100m, total);
    }

    #endregion
}
