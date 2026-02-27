using EventCenter.Web.Domain;
using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Services;
using EventCenter.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;

namespace EventCenter.Tests.Services;

public class EventServiceTests : IDisposable
{
    private readonly EventCenterDbContext _context;
    private readonly EventService _service;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly string _tempDirectory;

    public EventServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemory();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _mockEnvironment.Setup(e => e.WebRootPath).Returns(_tempDirectory);
        _service = new EventService(_context, _mockEnvironment.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public async Task CreateEvent_PersistsToDatabase()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2
        };

        // Act
        var result = await _service.CreateEventAsync(evt);

        // Assert
        Assert.True(result.Id > 0);

        // Verify persistence in same context (SQLite in-memory DB is connection-scoped)
        var persisted = await _context.Events.FindAsync(result.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Test Event", persisted.Title);
    }

    [Fact]
    public async Task GetEventById_IncludesRelatedEntities()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Test Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        _context.AgendaItems.Add(new EventAgendaItem
        {
            EventId = evt.Id,
            Title = "Agenda Item 1",
            StartDateTimeUtc = evt.StartDateUtc,
            EndDateTimeUtc = evt.StartDateUtc.AddHours(1)
        });

        _context.EventOptions.Add(new EventOption
        {
            EventId = evt.Id,
            Name = "Option 1",
            Price = 10.00m
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEventByIdAsync(evt.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.AgendaItems);
        Assert.Single(result.EventOptions);
    }

    [Fact]
    public async Task GetEvents_FiltersPastByDefault()
    {
        // Arrange
        var futureEvent = new Event
        {
            Title = "Future Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2
        };

        var pastEvent = new Event
        {
            Title = "Past Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(-8),
            EndDateUtc = DateTime.UtcNow.AddDays(-7),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(-10),
            MaxCapacity = 100,
            MaxCompanions = 2
        };

        _context.Events.AddRange(futureEvent, pastEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEventsAsync(includePast: false, sortColumn: null, ascending: false, page: 1, pageSize: 10);

        // Assert
        Assert.Single(result);
        Assert.Equal("Future Event", result[0].Title);
    }

    [Fact]
    public async Task GetEvents_IncludesPastWhenRequested()
    {
        // Arrange
        var futureEvent = new Event
        {
            Title = "Future Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2
        };

        var pastEvent = new Event
        {
            Title = "Past Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(-8),
            EndDateUtc = DateTime.UtcNow.AddDays(-7),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(-10),
            MaxCapacity = 100,
            MaxCompanions = 2
        };

        _context.Events.AddRange(futureEvent, pastEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEventsAsync(includePast: true, sortColumn: null, ascending: false, page: 1, pageSize: 10);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetEvents_SortsByStartDate()
    {
        // Arrange
        var events = new[]
        {
            new Event
            {
                Title = "Event 1",
                Location = "Location",
                StartDateUtc = DateTime.UtcNow.AddDays(5),
                EndDateUtc = DateTime.UtcNow.AddDays(6),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(3),
                MaxCapacity = 100,
                MaxCompanions = 2
            },
            new Event
            {
                Title = "Event 2",
                Location = "Location",
                StartDateUtc = DateTime.UtcNow.AddDays(10),
                EndDateUtc = DateTime.UtcNow.AddDays(11),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(8),
                MaxCapacity = 100,
                MaxCompanions = 2
            },
            new Event
            {
                Title = "Event 3",
                Location = "Location",
                StartDateUtc = DateTime.UtcNow.AddDays(1),
                EndDateUtc = DateTime.UtcNow.AddDays(2),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(-1),
                MaxCapacity = 100,
                MaxCompanions = 2
            }
        };

        _context.Events.AddRange(events);
        await _context.SaveChangesAsync();

        // Act - default sort is StartDateUtc descending
        var result = await _service.GetEventsAsync(includePast: true, sortColumn: "StartDateUtc", ascending: false, page: 1, pageSize: 10);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Event 2", result[0].Title); // Latest first
        Assert.Equal("Event 1", result[1].Title);
        Assert.Equal("Event 3", result[2].Title);
    }

    [Fact]
    public async Task GetEvents_Paginates()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            _context.Events.Add(new Event
            {
                Title = $"Event {i}",
                Location = "Location",
                StartDateUtc = DateTime.UtcNow.AddDays(i),
                EndDateUtc = DateTime.UtcNow.AddDays(i + 1),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(i - 1),
                MaxCapacity = 100,
                MaxCompanions = 2
            });
        }
        await _context.SaveChangesAsync();

        // Act - page 2, pageSize 2
        var result = await _service.GetEventsAsync(includePast: true, sortColumn: null, ascending: false, page: 2, pageSize: 2);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task PublishEvent_SetsIsPublished()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2,
            IsPublished = false
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        // Act
        var (success, error) = await _service.PublishEventAsync(evt.Id);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        var updated = await _context.Events.FindAsync(evt.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsPublished);
    }

    [Fact]
    public async Task UnpublishEvent_BlockedWithRegistrations()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2,
            IsPublished = true
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        _context.Registrations.Add(new Registration
        {
            EventId = evt.Id,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "Makler",
            RegistrationDateUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UnpublishEventAsync(evt.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Anmeldungen", result.ErrorMessage);
    }

    [Fact]
    public async Task UnpublishEvent_SucceedsWithoutRegistrations()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2,
            IsPublished = true
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UnpublishEventAsync(evt.Id);

        // Assert
        Assert.True(result.Success);
        var updated = await _context.Events.FindAsync(evt.Id);
        Assert.NotNull(updated);
        Assert.False(updated.IsPublished);
    }

    [Fact]
    public async Task DuplicateEvent_CopiesFieldsAndChildren()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Original Event",
            Location = "Location",
            Description = "Description",
            StartDateUtc = DateTime.UtcNow.AddDays(30),
            EndDateUtc = DateTime.UtcNow.AddDays(31),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(28),
            MaxCapacity = 100,
            MaxCompanions = 2,
            IsPublished = true,
            ContactName = "Contact",
            ContactEmail = "contact@example.com"
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        _context.AgendaItems.AddRange(
            new EventAgendaItem
            {
                EventId = evt.Id,
                Title = "Agenda Item 1",
                StartDateTimeUtc = evt.StartDateUtc,
                EndDateTimeUtc = evt.StartDateUtc.AddHours(1)
            },
            new EventAgendaItem
            {
                EventId = evt.Id,
                Title = "Agenda Item 2",
                StartDateTimeUtc = evt.StartDateUtc.AddHours(2),
                EndDateTimeUtc = evt.StartDateUtc.AddHours(3)
            }
        );

        _context.EventOptions.Add(new EventOption
        {
            EventId = evt.Id,
            Name = "Option 1",
            Price = 10.00m
        });

        await _context.SaveChangesAsync();

        // Act
        var duplicate = await _service.DuplicateEventAsync(evt.Id);

        // Assert
        Assert.NotEqual(evt.Id, duplicate.Id);
        Assert.Equal("Original Event (Kopie)", duplicate.Title);
        Assert.False(duplicate.IsPublished);
        Assert.Equal(2, duplicate.AgendaItems.Count);
        Assert.Single(duplicate.EventOptions);
        Assert.Empty(duplicate.Registrations);
    }

    [Fact]
    public async Task DuplicateEvent_ShiftsDates()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Original Event",
            Location = "Location",
            StartDateUtc = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 3, 2, 16, 0, 0, DateTimeKind.Utc),
            RegistrationDeadlineUtc = new DateTime(2026, 2, 25, 23, 59, 59, DateTimeKind.Utc),
            MaxCapacity = 100,
            MaxCompanions = 2
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        // Act
        var duplicate = await _service.DuplicateEventAsync(evt.Id);

        // Assert
        // Dates should be shifted by ~1 month
        Assert.True(duplicate.StartDateUtc > evt.StartDateUtc.AddDays(25));
        Assert.True(duplicate.StartDateUtc < evt.StartDateUtc.AddDays(35));
    }

    [Fact]
    public async Task DeleteEventOption_BlockedWhenBooked()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        var option = new EventOption
        {
            EventId = evt.Id,
            Name = "Option 1",
            Price = 10.00m
        };
        _context.EventOptions.Add(option);
        await _context.SaveChangesAsync();

        var registration = new Registration
        {
            EventId = evt.Id,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "Makler",
            RegistrationDateUtc = DateTime.UtcNow
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Link registration to option (simulate booking)
        registration.SelectedOptions.Add(option);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteEventOptionAsync(option.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Buchungen", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteEventOption_SucceedsWhenNotBooked()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        var option = new EventOption
        {
            EventId = evt.Id,
            Name = "Option 1",
            Price = 10.00m
        };
        _context.EventOptions.Add(option);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteEventOptionAsync(option.Id);

        // Assert
        Assert.True(result.Success);
        var deleted = await _context.EventOptions.FindAsync(option.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteEvent_BlockedWithRegistrations()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        _context.Registrations.Add(new Registration
        {
            EventId = evt.Id,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "Makler",
            RegistrationDateUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteEventAsync(evt.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteEvent_CascadesAgendaItemsAndOptions()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        _context.AgendaItems.Add(new EventAgendaItem
        {
            EventId = evt.Id,
            Title = "Agenda Item 1",
            StartDateTimeUtc = evt.StartDateUtc,
            EndDateTimeUtc = evt.StartDateUtc.AddHours(1)
        });

        _context.EventOptions.Add(new EventOption
        {
            EventId = evt.Id,
            Name = "Option 1",
            Price = 10.00m
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteEventAsync(evt.Id);

        // Assert
        Assert.True(result);
        var deleted = await _context.Events.FindAsync(evt.Id);
        Assert.Null(deleted);

        // Verify cascade deletion
        var agendaItems = _context.AgendaItems.Where(a => a.EventId == evt.Id).ToList();
        var options = _context.EventOptions.Where(o => o.EventId == evt.Id).ToList();
        Assert.Empty(agendaItems);
        Assert.Empty(options);
    }

    [Fact]
    public async Task SaveDocument_CreatesFileAndReturnsPath()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        var fileName = "test-document.pdf";
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        var path = await _service.SaveDocumentAsync(evt.Id, fileName, content);

        // Assert
        Assert.StartsWith($"/uploads/events/{evt.Id}/", path);
        Assert.Contains("test-document.pdf", path);

        // Verify file exists
        var fullPath = Path.Combine(_tempDirectory, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPath));
    }

    [Fact]
    public async Task DeleteDocument_RemovesFile()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        var fileName = "test-document.pdf";
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var path = await _service.SaveDocumentAsync(evt.Id, fileName, content);

        // Act
        await _service.DeleteDocumentAsync(path);

        // Assert
        var fullPath = Path.Combine(_tempDirectory, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public async Task GetPublicEventsAsync_ReturnsOnlyPublished()
    {
        // Arrange
        var publishedEvent = new Event
        {
            Title = "Published Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2,
            IsPublished = true
        };

        var unpublishedEvent = new Event
        {
            Title = "Unpublished Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(10),
            EndDateUtc = DateTime.UtcNow.AddDays(11),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(8),
            MaxCapacity = 100,
            MaxCompanions = 2,
            IsPublished = false
        };

        _context.Events.AddRange(publishedEvent, unpublishedEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPublicEventsAsync(null, null, null, null);

        // Assert
        Assert.Single(result);
        Assert.Equal("Published Event", result[0].Title);
    }

    [Fact]
    public async Task GetPublicEventsAsync_SearchByTitle()
    {
        // Arrange
        var events = new[]
        {
            new Event
            {
                Title = "React Workshop",
                Location = "Berlin",
                StartDateUtc = DateTime.UtcNow.AddDays(7),
                EndDateUtc = DateTime.UtcNow.AddDays(8),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
                MaxCapacity = 100,
                MaxCompanions = 2,
                IsPublished = true
            },
            new Event
            {
                Title = "Angular Conference",
                Location = "Munich",
                StartDateUtc = DateTime.UtcNow.AddDays(10),
                EndDateUtc = DateTime.UtcNow.AddDays(11),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(8),
                MaxCapacity = 100,
                MaxCompanions = 2,
                IsPublished = true
            },
            new Event
            {
                Title = "Vue Meetup",
                Location = "Hamburg",
                StartDateUtc = DateTime.UtcNow.AddDays(14),
                EndDateUtc = DateTime.UtcNow.AddDays(15),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(12),
                MaxCapacity = 100,
                MaxCompanions = 2,
                IsPublished = true
            }
        };

        _context.Events.AddRange(events);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPublicEventsAsync("Workshop", null, null, null);

        // Assert
        Assert.Single(result);
        Assert.Equal("React Workshop", result[0].Title);
    }

    [Fact]
    public async Task GetPublicEventsAsync_SearchByLocation()
    {
        // Arrange
        var events = new[]
        {
            new Event
            {
                Title = "Event 1",
                Location = "Berlin",
                StartDateUtc = DateTime.UtcNow.AddDays(7),
                EndDateUtc = DateTime.UtcNow.AddDays(8),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
                MaxCapacity = 100,
                MaxCompanions = 2,
                IsPublished = true
            },
            new Event
            {
                Title = "Event 2",
                Location = "Munich",
                StartDateUtc = DateTime.UtcNow.AddDays(10),
                EndDateUtc = DateTime.UtcNow.AddDays(11),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(8),
                MaxCapacity = 100,
                MaxCompanions = 2,
                IsPublished = true
            },
            new Event
            {
                Title = "Event 3",
                Location = "Berlin Mitte",
                StartDateUtc = DateTime.UtcNow.AddDays(14),
                EndDateUtc = DateTime.UtcNow.AddDays(15),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(12),
                MaxCapacity = 100,
                MaxCompanions = 2,
                IsPublished = true
            }
        };

        _context.Events.AddRange(events);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPublicEventsAsync("Berlin", null, null, null);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Contains("Berlin", e.Location));
    }

    [Fact]
    public async Task GetPublicEventsAsync_FilterByDateRange()
    {
        // Arrange
        var events = new[]
        {
            new Event
            {
                Title = "Event 1",
                Location = "Location",
                StartDateUtc = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc),
                EndDateUtc = new DateTime(2026, 3, 2, 16, 0, 0, DateTimeKind.Utc),
                RegistrationDeadlineUtc = new DateTime(2026, 2, 25, 23, 59, 59, DateTimeKind.Utc),
                MaxCapacity = 100,
                MaxCompanions = 2,
                IsPublished = true
            },
            new Event
            {
                Title = "Event 2",
                Location = "Location",
                StartDateUtc = new DateTime(2026, 4, 15, 10, 0, 0, DateTimeKind.Utc),
                EndDateUtc = new DateTime(2026, 4, 16, 16, 0, 0, DateTimeKind.Utc),
                RegistrationDeadlineUtc = new DateTime(2026, 4, 10, 23, 59, 59, DateTimeKind.Utc),
                MaxCapacity = 100,
                MaxCompanions = 2,
                IsPublished = true
            },
            new Event
            {
                Title = "Event 3",
                Location = "Location",
                StartDateUtc = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc),
                EndDateUtc = new DateTime(2026, 6, 21, 16, 0, 0, DateTimeKind.Utc),
                RegistrationDeadlineUtc = new DateTime(2026, 6, 15, 23, 59, 59, DateTimeKind.Utc),
                MaxCapacity = 100,
                MaxCompanions = 2,
                IsPublished = true
            }
        };

        _context.Events.AddRange(events);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPublicEventsAsync(
            null,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 31, 23, 59, 59, DateTimeKind.Utc),
            null);

        // Assert
        Assert.Single(result);
        Assert.Equal("Event 2", result[0].Title);
    }

    [Fact]
    public async Task GetPublicEventsAsync_SortsByStartDateAscending()
    {
        // Arrange
        var events = new[]
        {
            new Event
            {
                Title = "Event 3",
                Location = "Location",
                StartDateUtc = DateTime.UtcNow.AddDays(20),
                EndDateUtc = DateTime.UtcNow.AddDays(21),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(18),
                MaxCapacity = 100,
                MaxCompanions = 2,
                IsPublished = true
            },
            new Event
            {
                Title = "Event 1",
                Location = "Location",
                StartDateUtc = DateTime.UtcNow.AddDays(7),
                EndDateUtc = DateTime.UtcNow.AddDays(8),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
                MaxCapacity = 100,
                MaxCompanions = 2,
                IsPublished = true
            },
            new Event
            {
                Title = "Event 2",
                Location = "Location",
                StartDateUtc = DateTime.UtcNow.AddDays(14),
                EndDateUtc = DateTime.UtcNow.AddDays(15),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(12),
                MaxCapacity = 100,
                MaxCompanions = 2,
                IsPublished = true
            }
        };

        _context.Events.AddRange(events);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPublicEventsAsync(null, null, null, null);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Event 1", result[0].Title);
        Assert.Equal("Event 2", result[1].Title);
        Assert.Equal("Event 3", result[2].Title);
    }

    [Fact]
    public async Task GetPublicEventsAsync_IncludesRegistrationCount()
    {
        // Arrange
        var evt = new Event
        {
            Title = "Test Event",
            Location = "Location",
            StartDateUtc = DateTime.UtcNow.AddDays(7),
            EndDateUtc = DateTime.UtcNow.AddDays(8),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(5),
            MaxCapacity = 100,
            MaxCompanions = 2,
            IsPublished = true
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        _context.Registrations.AddRange(
            new Registration
            {
                EventId = evt.Id,
                Email = "user1@example.com",
                FirstName = "User",
                LastName = "One",
                RegistrationDateUtc = DateTime.UtcNow
            },
            new Registration
            {
                EventId = evt.Id,
                Email = "user2@example.com",
                FirstName = "User",
                LastName = "Two",
                RegistrationDateUtc = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPublicEventsAsync(null, null, null, null);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].Registrations.Count);
    }
}
