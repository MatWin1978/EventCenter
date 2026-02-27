using ClosedXML.Excel;
using EventCenter.Web.Domain;
using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Domain.Enums;
using EventCenter.Web.Services;
using EventCenter.Tests.Helpers;
using Xunit;

namespace EventCenter.Tests.Services;

public class ParticipantExportServiceTests : IDisposable
{
    private readonly EventCenterDbContext _context;
    private readonly ParticipantExportService _exportService;
    private readonly ParticipantQueryService _queryService;
    private readonly Event _testEvent;

    public ParticipantExportServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemory();
        _exportService = new ParticipantExportService(_context);
        _queryService = new ParticipantQueryService(_context);

        _testEvent = new Event
        {
            Title = "Test Event",
            Location = "Berlin",
            StartDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(30), DateTimeKind.Utc),
            EndDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(30).AddHours(4), DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(15), DateTimeKind.Utc),
            MaxCapacity = 100,
            MaxCompanions = 2,
            IsPublished = true
        };

        _context.Events.Add(_testEvent);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // Helper to create a registration for the test event
    private Registration CreateRegistration(string firstName, string lastName, string email,
        bool isCancelled = false, RegistrationType type = RegistrationType.Makler, string? phone = null)
    {
        return new Registration
        {
            EventId = _testEvent.Id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            RegistrationType = type,
            RegistrationDateUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            IsCancelled = isCancelled,
            CancellationDateUtc = isCancelled ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc) : null,
            IsConfirmed = true
        };
    }

    [Fact]
    public async Task ExportParticipantList_ReturnsValidExcel()
    {
        // Arrange
        _context.Registrations.AddRange(
            CreateRegistration("Anna", "Becker", "anna@test.de"),
            CreateRegistration("Bob", "Meier", "bob@test.de"),
            CreateRegistration("Cancelled", "User", "cancelled@test.de", isCancelled: true)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _exportService.ExportParticipantListAsync(_testEvent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        using var workbook = new XLWorkbook(new MemoryStream(result));
        Assert.True(workbook.TryGetWorksheet("Teilnehmerliste", out var ws));
        Assert.NotNull(ws);

        // Header row + 2 active registrations (cancelled excluded)
        var lastRow = ws!.LastRowUsed()?.RowNumber() ?? 0;
        Assert.Equal(3, lastRow); // 1 header + 2 data rows

        // Verify column headers
        Assert.Equal("Vorname", ws.Cell(1, 1).Value.ToString());
        Assert.Equal("Nachname", ws.Cell(1, 2).Value.ToString());
        Assert.Equal("EMail", ws.Cell(1, 3).Value.ToString());
        Assert.Equal("Firma", ws.Cell(1, 4).Value.ToString());
        Assert.Equal("Typ", ws.Cell(1, 5).Value.ToString());
        Assert.Equal("Anmeldedatum", ws.Cell(1, 6).Value.ToString());
    }

    [Fact]
    public async Task ExportParticipantList_ExcludesCancelled()
    {
        // Arrange
        _context.Registrations.AddRange(
            CreateRegistration("Active", "One", "active1@test.de"),
            CreateRegistration("Active", "Two", "active2@test.de"),
            CreateRegistration("Cancelled", "Three", "cancelled@test.de", isCancelled: true)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _exportService.ExportParticipantListAsync(_testEvent.Id);

        // Assert
        using var workbook = new XLWorkbook(new MemoryStream(result));
        workbook.TryGetWorksheet("Teilnehmerliste", out var ws);

        var lastRow = ws!.LastRowUsed()?.RowNumber() ?? 0;
        // Header + 2 active (not 3 total)
        Assert.Equal(3, lastRow);
    }

    [Fact]
    public async Task ExportContactData_ReturnsValidExcel()
    {
        // Arrange
        _context.Registrations.AddRange(
            CreateRegistration("Anna", "Becker", "anna@test.de", phone: "+49 123 456"),
            CreateRegistration("Bob", "Meier", "bob@test.de", phone: "+49 987 654")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _exportService.ExportContactDataAsync(_testEvent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        using var workbook = new XLWorkbook(new MemoryStream(result));
        Assert.True(workbook.TryGetWorksheet("Kontaktdaten", out var ws));
        Assert.NotNull(ws);

        // Verify headers
        Assert.Equal("Vorname", ws!.Cell(1, 1).Value.ToString());
        Assert.Equal("Nachname", ws.Cell(1, 2).Value.ToString());
        Assert.Equal("EMail", ws.Cell(1, 3).Value.ToString());
        Assert.Equal("Telefon", ws.Cell(1, 4).Value.ToString());
        Assert.Equal("Firma", ws.Cell(1, 5).Value.ToString());

        // Verify phone data is present in the rows
        var phoneValues = new List<string>();
        for (int row = 2; row <= 3; row++)
        {
            phoneValues.Add(ws.Cell(row, 4).Value.ToString());
        }

        Assert.Contains("+49 123 456", phoneValues);
        Assert.Contains("+49 987 654", phoneValues);
    }

    [Fact]
    public async Task ExportNonParticipants_ReturnsCorrectDelta()
    {
        // Arrange: Company invited 5, 2 registered → 3 not registered
        var company = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Test GmbH",
            ContactEmail = "contact@test.de",
            ContactPhone = "+49 100 200",
            MaxParticipants = 5,
            Status = InvitationStatus.Sent
        };
        _context.EventCompanies.Add(company);
        await _context.SaveChangesAsync();

        _context.Registrations.AddRange(
            new Registration
            {
                EventId = _testEvent.Id,
                EventCompanyId = company.Id,
                FirstName = "P1",
                LastName = "Last",
                Email = "p1@company.de",
                RegistrationType = RegistrationType.CompanyParticipant,
                RegistrationDateUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                IsCancelled = false,
                IsConfirmed = true
            },
            new Registration
            {
                EventId = _testEvent.Id,
                EventCompanyId = company.Id,
                FirstName = "P2",
                LastName = "Last",
                Email = "p2@company.de",
                RegistrationType = RegistrationType.CompanyParticipant,
                RegistrationDateUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                IsCancelled = false,
                IsConfirmed = true
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _exportService.ExportNonParticipantsAsync(_testEvent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        using var workbook = new XLWorkbook(new MemoryStream(result));
        Assert.True(workbook.TryGetWorksheet("Nicht-Teilnehmer", out var ws));
        Assert.NotNull(ws);

        // Should have 1 data row (header + 1 company row)
        var lastRow = ws!.LastRowUsed()?.RowNumber() ?? 0;
        Assert.Equal(2, lastRow);

        // Find the "Nicht registriert" column (column 6 based on our data structure)
        // NichtRegistriert = 5 - 2 = 3
        var notRegisteredValue = ws.Cell(2, 6).Value;
        Assert.Equal(3, (int)(double)notRegisteredValue);
    }

    [Fact]
    public async Task ExportNonParticipants_SkipsNullMaxParticipants()
    {
        // Arrange: Company with no max participants defined
        var company = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Unknown Size GmbH",
            ContactEmail = "contact@unknown.de",
            MaxParticipants = null,
            Status = InvitationStatus.Sent
        };
        _context.EventCompanies.Add(company);
        await _context.SaveChangesAsync();

        // Act
        var result = await _exportService.ExportNonParticipantsAsync(_testEvent.Id);

        // Assert
        using var workbook = new XLWorkbook(new MemoryStream(result));
        workbook.TryGetWorksheet("Nicht-Teilnehmer", out var ws);

        // Should have no data rows (only header or empty sheet)
        var lastRow = ws?.LastRowUsed()?.RowNumber() ?? 0;
        Assert.Equal(1, lastRow); // Only header, no data rows
    }

    [Fact]
    public async Task ExportCompanyList_ReturnsValidExcel()
    {
        // Arrange: 2 companies with some registrations
        var company1 = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Alpha GmbH",
            ContactEmail = "alpha@test.de",
            ContactPhone = "+49 111 222",
            Status = InvitationStatus.Booked,
            InvitationSentUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-5), DateTimeKind.Utc)
        };
        var company2 = new EventCompany
        {
            EventId = _testEvent.Id,
            CompanyName = "Beta AG",
            ContactEmail = "beta@test.de",
            Status = InvitationStatus.Sent
        };
        _context.EventCompanies.AddRange(company1, company2);
        await _context.SaveChangesAsync();

        _context.Registrations.Add(new Registration
        {
            EventId = _testEvent.Id,
            EventCompanyId = company1.Id,
            FirstName = "Person",
            LastName = "One",
            Email = "p@alpha.de",
            RegistrationType = RegistrationType.CompanyParticipant,
            RegistrationDateUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            IsCancelled = false,
            IsConfirmed = true
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _exportService.ExportCompanyListAsync(_testEvent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        using var workbook = new XLWorkbook(new MemoryStream(result));
        Assert.True(workbook.TryGetWorksheet("Firmenliste", out var ws));
        Assert.NotNull(ws);

        // Header + 2 companies
        var lastRow = ws!.LastRowUsed()?.RowNumber() ?? 0;
        Assert.Equal(3, lastRow);

        // Verify headers
        Assert.Equal("Firma", ws.Cell(1, 1).Value.ToString());
        Assert.Equal("KontaktEmail", ws.Cell(1, 2).Value.ToString());
    }

    [Fact]
    public async Task GetParticipants_ReturnsAllIncludingCancelled()
    {
        // Arrange
        _context.Registrations.AddRange(
            CreateRegistration("Active", "One", "active1@test.de"),
            CreateRegistration("Active", "Two", "active2@test.de"),
            CreateRegistration("Cancelled", "Three", "cancelled@test.de", isCancelled: true)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _queryService.GetParticipantsAsync(_testEvent.Id);

        // Assert: Admin sees all 3 including cancelled
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetParticipants_OrdersByLastNameThenFirstName()
    {
        // Arrange: Add registrations in scrambled order
        _context.Registrations.AddRange(
            CreateRegistration("Zara", "Zimmermann", "zara@test.de"),
            CreateRegistration("Anna", "Becker", "anna@test.de"),
            CreateRegistration("Bruno", "Becker", "bruno@test.de"),
            CreateRegistration("Klaus", "Aigner", "k@test.de")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _queryService.GetParticipantsAsync(_testEvent.Id);

        // Assert: Ordered by LastName, then FirstName
        Assert.Equal(4, result.Count);
        Assert.Equal("Aigner", result[0].LastName);
        Assert.Equal("Anna", result[1].FirstName);  // Becker Anna before Becker Bruno
        Assert.Equal("Bruno", result[2].FirstName); // Becker Bruno after Becker Anna
        Assert.Equal("Zimmermann", result[3].LastName);
    }
}
