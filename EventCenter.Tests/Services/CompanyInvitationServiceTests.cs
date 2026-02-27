using EventCenter.Web.Domain;
using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Domain.Enums;
using EventCenter.Web.Infrastructure.Email;
using EventCenter.Web.Models;
using EventCenter.Web.Services;
using EventCenter.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventCenter.Tests.Services;

public class CompanyInvitationServiceTests : IDisposable
{
    private readonly EventCenterDbContext _context;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<ILogger<CompanyInvitationService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly CompanyInvitationService _service;
    private readonly Event _testEvent;

    public CompanyInvitationServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemory();
        _emailSenderMock = new Mock<IEmailSender>();
        _loggerMock = new Mock<ILogger<CompanyInvitationService>>();
        _configurationMock = new Mock<IConfiguration>();

        // Setup configuration for base URL
        _configurationMock.Setup(c => c["BaseUrl"]).Returns("https://eventcenter.test");

        _service = new CompanyInvitationService(
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
            MaxParticipants = 100,
            IsPublished = true
        };

        var agendaItem1 = new EventAgendaItem
        {
            EventId = 1,
            Title = "Session 1",
            Description = "First session",
            StartDateTimeUtc = DateTime.UtcNow.AddDays(30),
            EndDateTimeUtc = DateTime.UtcNow.AddDays(30).AddHours(2),
            CostForMakler = 100m,
            CostForGuest = 150m,
            IsMandatory = false
        };

        var agendaItem2 = new EventAgendaItem
        {
            EventId = 1,
            Title = "Session 2",
            Description = "Second session",
            StartDateTimeUtc = DateTime.UtcNow.AddDays(30).AddHours(2),
            EndDateTimeUtc = DateTime.UtcNow.AddDays(30).AddHours(4),
            CostForMakler = 200m,
            CostForGuest = 250m,
            IsMandatory = false
        };

        _testEvent.AgendaItems.Add(agendaItem1);
        _testEvent.AgendaItems.Add(agendaItem2);

        _context.Events.Add(_testEvent);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public void GenerateSecureInvitationCode_ReturnsUniqueGuidStrings()
    {
        // Act
        var code1 = CompanyInvitationService.GenerateSecureInvitationCode();
        var code2 = CompanyInvitationService.GenerateSecureInvitationCode();

        // Assert
        Assert.NotNull(code1);
        Assert.NotNull(code2);
        Assert.NotEqual(code1, code2);
        Assert.Equal(32, code1.Length); // GUID without dashes = 32 chars
        Assert.Equal(32, code2.Length);
        Assert.Matches("^[0-9a-f]{32}$", code1);

        // Verify it's a valid GUID
        var guidString = code1.Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
        Assert.True(Guid.TryParse(guidString, out _));
    }

    [Theory]
    [InlineData(100, null, null, 100.00)]
    [InlineData(100, 10, null, 90.00)]
    [InlineData(100, 10, 85, 85.00)]
    [InlineData(99.99, 33.33, null, 66.66)]
    [InlineData(100, 50, 45, 45.00)]
    [InlineData(200, 25, null, 150.00)]
    public void CalculateCustomPrice_AppliesCorrectLogic(decimal basePrice, decimal? percentageDiscount, decimal? manualOverride, decimal expected)
    {
        // Act
        var result = CompanyInvitationService.CalculateCustomPrice(basePrice, percentageDiscount, manualOverride);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task CreateInvitationAsync_ValidInput_CreatesInvitationAndAgendaItemPrices()
    {
        // Arrange
        var formModel = new CompanyInvitationFormModel
        {
            EventId = _testEvent.Id,
            CompanyName = "Test Company",
            ContactEmail = "test@company.com",
            ContactPhone = "+49 123 456789",
            PercentageDiscount = 10,
            SendImmediately = false,
            AgendaItemPrices = new List<CompanyAgendaItemPriceModel>
            {
                new() { AgendaItemId = 1, BasePrice = 100m, ManualOverride = null },
                new() { AgendaItemId = 2, BasePrice = 200m, ManualOverride = 170m }
            }
        };

        // Act
        var (success, invitationId, errorMessage) = await _service.CreateInvitationAsync(formModel);

        // Assert
        Assert.True(success);
        Assert.NotNull(invitationId);
        Assert.Null(errorMessage);

        var invitation = await _context.EventCompanies.FindAsync(invitationId);
        Assert.NotNull(invitation);
        Assert.Equal("Test Company", invitation.CompanyName);
        Assert.Equal("test@company.com", invitation.ContactEmail);
        Assert.Equal(InvitationStatus.Draft, invitation.Status);
        Assert.NotNull(invitation.InvitationCode);
        Assert.Equal(32, invitation.InvitationCode.Length);
        Assert.Null(invitation.InvitationSentUtc);

        var agendaPrices = _context.EventCompanyAgendaItemPrices
            .Where(x => x.EventCompanyId == invitationId)
            .ToList();
        Assert.Equal(2, agendaPrices.Count);
        Assert.Equal(90m, agendaPrices.First(x => x.AgendaItemId == 1).CustomPrice); // 10% discount
        Assert.Equal(170m, agendaPrices.First(x => x.AgendaItemId == 2).CustomPrice); // manual override
    }

    [Fact]
    public async Task CreateInvitationAsync_SendImmediately_TransitionsToSentAndTriggersEmail()
    {
        // Arrange
        var formModel = new CompanyInvitationFormModel
        {
            EventId = _testEvent.Id,
            CompanyName = "Test Company",
            ContactEmail = "test@company.com",
            SendImmediately = true,
            PersonalMessage = "Looking forward to seeing you!",
            AgendaItemPrices = new List<CompanyAgendaItemPriceModel>()
        };

        // Act
        var (success, invitationId, errorMessage) = await _service.CreateInvitationAsync(formModel);

        // Assert
        Assert.True(success);
        Assert.NotNull(invitationId);

        var invitation = await _context.EventCompanies.FindAsync(invitationId);
        Assert.NotNull(invitation);
        Assert.Equal(InvitationStatus.Sent, invitation.Status);
        Assert.NotNull(invitation.InvitationSentUtc);

        // Email should be triggered (fire-and-forget, so we wait a bit)
        await Task.Delay(100);
        _emailSenderMock.Verify(
            x => x.SendCompanyInvitationAsync(
                It.IsAny<EventCompany>(),
                It.IsAny<Event>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateInvitationAsync_EventNotFound_ReturnsError()
    {
        // Arrange
        var formModel = new CompanyInvitationFormModel
        {
            EventId = 999,
            CompanyName = "Test Company",
            ContactEmail = "test@company.com"
        };

        // Act
        var (success, invitationId, errorMessage) = await _service.CreateInvitationAsync(formModel);

        // Assert
        Assert.False(success);
        Assert.Null(invitationId);
        Assert.Equal("Veranstaltung nicht gefunden.", errorMessage);
    }

    [Fact]
    public async Task CreateInvitationAsync_DuplicateEmail_ReturnsError()
    {
        // Arrange
        var firstInvitation = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Existing Company",
            ContactEmail = "duplicate@company.com",
            InvitationCode = "existing123",
            Status = InvitationStatus.Sent
        };
        _context.EventCompanies.Add(firstInvitation);
        await _context.SaveChangesAsync();

        var formModel = new CompanyInvitationFormModel
        {
            EventId = _testEvent.Id,
            CompanyName = "New Company",
            ContactEmail = "duplicate@company.com"
        };

        // Act
        var (success, invitationId, errorMessage) = await _service.CreateInvitationAsync(formModel);

        // Assert
        Assert.False(success);
        Assert.Null(invitationId);
        Assert.Equal("Diese Firma wurde bereits eingeladen.", errorMessage);
    }

    [Fact]
    public async Task SendInvitationAsync_DraftInvitation_TransitionsToSentAndSendsEmail()
    {
        // Arrange
        var invitation = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Test Company",
            ContactEmail = "test@company.com",
            InvitationCode = CompanyInvitationService.GenerateSecureInvitationCode(),
            Status = InvitationStatus.Draft,
            PersonalMessage = "Welcome!"
        };
        _context.EventCompanies.Add(invitation);
        await _context.SaveChangesAsync();

        // Act
        var (success, errorMessage) = await _service.SendInvitationAsync(invitation.Id);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);

        var updated = await _context.EventCompanies.FindAsync(invitation.Id);
        Assert.Equal(InvitationStatus.Sent, updated!.Status);
        Assert.NotNull(updated.InvitationSentUtc);

        await Task.Delay(100);
        _emailSenderMock.Verify(
            x => x.SendCompanyInvitationAsync(
                It.IsAny<EventCompany>(),
                It.IsAny<Event>(),
                "Welcome!",
                It.Is<string>(link => link.Contains(invitation.InvitationCode))),
            Times.Once);
    }

    [Fact]
    public async Task ResendInvitationAsync_SentInvitation_UpdatesTimestampAndSendsEmail()
    {
        // Arrange
        var invitation = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Test Company",
            ContactEmail = "test@company.com",
            InvitationCode = CompanyInvitationService.GenerateSecureInvitationCode(),
            Status = InvitationStatus.Sent,
            InvitationSentUtc = DateTime.UtcNow.AddDays(-5)
        };
        _context.EventCompanies.Add(invitation);
        await _context.SaveChangesAsync();

        var originalSentDate = invitation.InvitationSentUtc;

        // Act
        var (success, errorMessage) = await _service.ResendInvitationAsync(invitation.Id);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);

        var updated = await _context.EventCompanies.FindAsync(invitation.Id);
        Assert.NotEqual(originalSentDate, updated!.InvitationSentUtc);
        Assert.True(updated.InvitationSentUtc > originalSentDate);

        await Task.Delay(100);
        _emailSenderMock.Verify(
            x => x.SendCompanyInvitationAsync(
                It.IsAny<EventCompany>(),
                It.IsAny<Event>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateInvitationAsync_UpdatesPricingAndContactDetails()
    {
        // Arrange
        var invitation = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Original Company",
            ContactEmail = "original@company.com",
            InvitationCode = CompanyInvitationService.GenerateSecureInvitationCode(),
            Status = InvitationStatus.Sent,
            PercentageDiscount = 10
        };
        _context.EventCompanies.Add(invitation);

        var oldPrice = new EventCompanyAgendaItemPrice
        {
            EventCompanyId = invitation.Id,
            AgendaItemId = 1,
            CustomPrice = 90m
        };
        _context.EventCompanyAgendaItemPrices.Add(oldPrice);
        await _context.SaveChangesAsync();

        var updateModel = new CompanyInvitationFormModel
        {
            EventId = _testEvent.Id,
            CompanyName = "Updated Company",
            ContactEmail = "updated@company.com",
            ContactPhone = "+49 987 654321",
            PercentageDiscount = 20,
            PersonalMessage = "Updated message",
            AgendaItemPrices = new List<CompanyAgendaItemPriceModel>
            {
                new() { AgendaItemId = 1, BasePrice = 100m, ManualOverride = 75m },
                new() { AgendaItemId = 2, BasePrice = 200m, ManualOverride = null }
            }
        };

        // Act
        var (success, errorMessage) = await _service.UpdateInvitationAsync(invitation.Id, updateModel);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);

        var updated = await _context.EventCompanies.FindAsync(invitation.Id);
        Assert.Equal("Updated Company", updated!.CompanyName);
        Assert.Equal("updated@company.com", updated.ContactEmail);
        Assert.Equal(20, updated.PercentageDiscount);

        var prices = _context.EventCompanyAgendaItemPrices
            .Where(x => x.EventCompanyId == invitation.Id)
            .ToList();
        Assert.Equal(2, prices.Count);
        Assert.Equal(75m, prices.First(x => x.AgendaItemId == 1).CustomPrice);
        Assert.Equal(160m, prices.First(x => x.AgendaItemId == 2).CustomPrice); // 20% discount
    }

    [Fact]
    public async Task DeleteInvitationAsync_DraftStatus_AllowsDeletion()
    {
        // Arrange
        var invitation = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Test Company",
            ContactEmail = "test@company.com",
            InvitationCode = CompanyInvitationService.GenerateSecureInvitationCode(),
            Status = InvitationStatus.Draft
        };
        _context.EventCompanies.Add(invitation);
        await _context.SaveChangesAsync();

        // Act
        var (success, errorMessage) = await _service.DeleteInvitationAsync(invitation.Id);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);

        var deleted = await _context.EventCompanies.FindAsync(invitation.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteInvitationAsync_BookedStatus_PreventsDeletion()
    {
        // Arrange
        var invitation = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Test Company",
            ContactEmail = "test@company.com",
            InvitationCode = CompanyInvitationService.GenerateSecureInvitationCode(),
            Status = InvitationStatus.Booked
        };
        _context.EventCompanies.Add(invitation);
        await _context.SaveChangesAsync();

        // Act
        var (success, errorMessage) = await _service.DeleteInvitationAsync(invitation.Id);

        // Assert
        Assert.False(success);
        Assert.Equal("Diese Einladung kann nicht gelöscht werden, da bereits eine Buchung vorliegt.", errorMessage);

        var stillExists = await _context.EventCompanies.FindAsync(invitation.Id);
        Assert.NotNull(stillExists);
    }

    [Fact]
    public async Task GetInvitationsForEventAsync_ReturnsAllInvitationsWithNavigation()
    {
        // Arrange
        var invitation1 = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Company A",
            ContactEmail = "a@company.com",
            InvitationCode = "code1",
            Status = InvitationStatus.Draft
        };
        var invitation2 = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Company B",
            ContactEmail = "b@company.com",
            InvitationCode = "code2",
            Status = InvitationStatus.Sent
        };
        _context.EventCompanies.AddRange(invitation1, invitation2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetInvitationsForEventAsync(_testEvent.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, i => i.CompanyName == "Company A");
        Assert.Contains(result, i => i.CompanyName == "Company B");
        Assert.All(result, i => Assert.NotNull(i.Event));
    }

    [Fact]
    public async Task GetInvitationByIdAsync_ReturnsInvitationWithAgendaItemPrices()
    {
        // Arrange
        var invitation = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Test Company",
            ContactEmail = "test@company.com",
            InvitationCode = "testcode",
            Status = InvitationStatus.Draft
        };
        _context.EventCompanies.Add(invitation);

        var agendaPrice = new EventCompanyAgendaItemPrice
        {
            EventCompanyId = invitation.Id,
            AgendaItemId = 1,
            CustomPrice = 85m
        };
        _context.EventCompanyAgendaItemPrices.Add(agendaPrice);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetInvitationByIdAsync(invitation.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Company", result.CompanyName);
        Assert.Single(result.AgendaItemPrices);
        Assert.NotNull(result.AgendaItemPrices.First().AgendaItem);
    }

    [Fact]
    public async Task GetInvitationStatusSummaryAsync_ReturnsCorrectCounts()
    {
        // Arrange
        _context.EventCompanies.AddRange(
            new EventCompany { EventId = _testEvent.Id, CompanyName = "C1", ContactEmail = "c1@test.com", InvitationCode = "c1", Status = InvitationStatus.Draft },
            new EventCompany { EventId = _testEvent.Id, CompanyName = "C2", ContactEmail = "c2@test.com", InvitationCode = "c2", Status = InvitationStatus.Draft },
            new EventCompany { EventId = _testEvent.Id, CompanyName = "C3", ContactEmail = "c3@test.com", InvitationCode = "c3", Status = InvitationStatus.Sent },
            new EventCompany { EventId = _testEvent.Id, CompanyName = "C4", ContactEmail = "c4@test.com", InvitationCode = "c4", Status = InvitationStatus.Booked }
        );
        await _context.SaveChangesAsync();

        // Act
        var summary = await _service.GetInvitationStatusSummaryAsync(_testEvent.Id);

        // Assert
        Assert.Equal(2, summary[InvitationStatus.Draft]);
        Assert.Equal(1, summary[InvitationStatus.Sent]);
        Assert.Equal(1, summary[InvitationStatus.Booked]);
        Assert.Equal(0, summary[InvitationStatus.Cancelled]);
    }
}
