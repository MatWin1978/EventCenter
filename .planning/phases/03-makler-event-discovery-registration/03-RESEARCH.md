# Phase 3: Makler Event Discovery & Registration - Research

**Researched:** 2026-02-26
**Domain:** ASP.NET Core Blazor Server, event registration, email notifications, iCalendar export
**Confidence:** HIGH

## Summary

Phase 3 implements broker-facing event discovery and self-registration with agenda item selection. The research validates that the existing stack (Blazor Server, EF Core, FluentValidation, Bootstrap 5) is sufficient with two key additions: MailKit for SMTP email notifications and Ical.NET for iCalendar export functionality.

Key technical challenges identified: optimistic concurrency for preventing race conditions during simultaneous registrations, proper transaction boundaries for registration with related entities, and email deliverability configuration (SPF/DKIM/DMARC).

The project already has established patterns for list/detail pages (EventList/EventForm), service layer architecture (EventService), timezone handling (TimeZoneHelper), and FluentValidation integration with Blazored.FluentValidation, which can be directly applied to the makler portal pages.

**Primary recommendation:** Use existing architectural patterns with RowVersion concurrency tokens for Registration entity, implement dedicated RegistrationService following EventService pattern, use MailKit with IEmailSender abstraction for testability, and leverage Ical.NET for RFC 5545-compliant calendar exports.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Event list page:**
- Card grid layout (responsive, 2-3 columns)
- Each card shows: title, date, location, short description excerpt, status badge, cost indication
- Colored status badges: green (Plätze frei), blue (Angemeldet), red (Ausgebucht), gray (Verpasst)
- Active events displayed first, past/full events in a separate collapsible section below

**Registration flow:**
- Single page flow: agenda item selection, cost summary, and submit all on one page
- Confirmation dialog (modal) before final submission — summarizes selections and total costs
- After successful registration: full summary page with registration details, selected agenda items, total cost, iCal download button, and "Zurück zur Übersicht" link

**Event detail page:**
- Sidebar layout: main content on left (description, agenda, documents), sidebar on right with key info (date, location, contact, register button)
- Documents shown as file cards (name, type, size, download button)
- Register button and iCal export in sticky sidebar — always visible while scrolling
- Agenda items with times and costs visible on detail page before registration (full program preview)

**Search & filtering:**
- Instant text filter: filters event list as user types (no submit button)
- Date filter via quick presets: "Diesen Monat", "Nächste 3 Monate", "Dieses Jahr" plus optional custom range
- Search bar and filter controls in a horizontal top bar above the event grid
- Default sort: nearest upcoming events first

### Claude's Discretion

- Agenda item presentation style during registration (checklist vs cards vs other)
- Loading states and skeleton designs
- Exact spacing, typography, and color palette
- Error state handling and validation message styling
- Empty search results state design
- Mobile responsive breakpoint behavior

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope

</user_constraints>

<phase_requirements>
## Phase Requirements

This phase must address the following requirements from REQUIREMENTS.md:

| ID | Description | Research Support |
|----|-------------|-----------------|
| MLST-01 | Makler sieht Liste aller für ihn sichtbaren Veranstaltungen | Event query with IsPublished filter, EventState calculation via EventExtensions.GetCurrentState(), registration status from Registrations collection |
| MLST-02 | Makler kann nach Name/Ort suchen und nach Datum filtern | IQueryable LINQ filters on Title/Location with Contains(), DateTime range filters on StartDateUtc, existing EventService pagination pattern |
| MLST-03 | Makler sieht Anmeldestatus pro Veranstaltung (Plätze frei, Angemeldet, Ausgebucht, Verpasst) | EventState enum (Public, DeadlineReached, Finished), capacity check via MaxCapacity vs RegistrationCount, user registration lookup via Registrations.Any(r => r.Email == currentUser) |
| MDET-01 | Makler sieht Veranstaltungsdetails (Titel, Beschreibung, Ort, Zeit, Kontakt) | Event entity properties with TimeZoneHelper.ConvertUtcToCet() for display, existing EventService.GetEventByIdAsync() with Include navigation properties |
| MDET-02 | Makler kann Dokumente herunterladen | Event.DocumentPaths collection, file download via FileStreamResult with content-disposition attachment header, path validation to prevent traversal attacks |
| MDET-03 | Makler kann Termin als iCalendar exportieren | Ical.NET library (v5+), Calendar with CalendarEvent, VEVENT with DTSTART/DTEND/LOCATION/SUMMARY, FileContentResult with "text/calendar" MIME type |
| MREG-01 | Makler kann sich für Veranstaltung anmelden mit Agendapunkt-Auswahl | Registration entity with many-to-many to EventAgendaItem via join table, EditForm with Blazored.FluentValidation, transaction scope for atomic registration creation |
| MREG-02 | System prüft Deadline, Kapazität und Berechtigung vor Anmeldung | EventState.Public check, TimeZoneHelper.IsRegistrationOpen() for deadline validation, capacity check with RowVersion optimistic concurrency to prevent race conditions |
| MREG-03 | Makler sieht Teilnahmekosten pro Agendapunkt | EventAgendaItem.CostForMakler property, client-side calculation sum in Blazor component, confirmation modal with cost breakdown before final submit |
| MREG-04 | Makler erhält Bestätigungsseite nach erfolgreicher Anmeldung | Redirect to confirmation page with registration ID, display registered agenda items and total cost, include iCal download button |
| MAIL-01 | System sendet Bestätigung an Makler nach Selbstanmeldung | MailKit with SmtpClient, IEmailSender interface for DI and testing, MimeMessage with HTML body, async SendAsync() after successful SaveChangesAsync() |

</phase_requirements>

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core Blazor | 8.0 | UI framework | Already used in project, Server rendering for direct DB access |
| Entity Framework Core | 9.0 | Data access | Already configured with SQL Server, established pattern in EventService |
| FluentValidation | 11.* | Validation | Already integrated, existing validators for Event/AgendaItem/EventOption |
| Blazored.FluentValidation | 2.* | Blazor integration | Already in project, proven pattern in EventForm.razor |
| Bootstrap 5 | 5.x | UI styling | Project standard, card grids and responsive layouts built-in |
| MailKit | 4.15+ | Email sending | Industry standard for .NET SMTP, cross-platform, actively maintained |
| Ical.NET | 5.2+ | iCalendar export | RFC 5545 compliant, .NET 8 compatible, v5 rewrite for performance |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| TimeZoneConverter | 6.* | Timezone handling | Already in project for CET conversions, use TimeZoneHelper utility class |
| xUnit + bUnit | Latest | Testing | Already configured for unit and Blazor component tests |
| Moq | 4.* | Mocking | Already in test project for service layer mocks |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| MailKit | System.Net.Mail.SmtpClient | SmtpClient is obsolete, lacks modern features, MailKit is recommended replacement |
| Ical.NET | Manual .ics generation | Manual generation error-prone, misses edge cases (DST, recurrence), Ical.NET is battle-tested |
| Server-side filtering | Client-side JS | Blazor Server already maintains state on server, client-side requires API boundary and duplication |

**Installation:**

```bash
# MailKit for email notifications
dotnet add EventCenter.Web package MailKit --version 4.*

# Ical.NET for calendar export
dotnet add EventCenter.Web package Ical.Net --version 5.*
```

## Architecture Patterns

### Recommended Project Structure

```
EventCenter.Web/
├── Components/Pages/Portal/
│   ├── Events/
│   │   ├── EventList.razor          # Card grid with search/filter
│   │   ├── EventDetail.razor        # Sidebar layout with sticky actions
│   │   └── EventRegistration.razor  # Single-page registration form
│   └── Registrations/
│       └── RegistrationConfirmation.razor  # Success page with iCal download
├── Services/
│   ├── EventService.cs              # Existing - extend with makler queries
│   ├── RegistrationService.cs       # NEW - registration business logic
│   └── EmailService.cs              # NEW - email sending with MailKit
├── Domain/Entities/
│   ├── Registration.cs              # Extend with RowVersion for concurrency
│   └── RegistrationAgendaItem.cs    # NEW - join table for many-to-many
├── Validators/
│   └── RegistrationValidator.cs     # NEW - validate registration submissions
└── Infrastructure/
    ├── Email/
    │   ├── IEmailSender.cs          # Interface for DI
    │   ├── MailKitEmailSender.cs    # Production implementation
    │   └── EmailTemplates.cs        # HTML templates for notifications
    └── Calendar/
        └── ICalendarExportService.cs  # iCalendar generation with Ical.NET
```

### Pattern 1: Service Layer with Transaction Boundaries

**What:** Business logic in dedicated service classes with explicit transaction control for multi-entity operations.

**When to use:** Registration creates Registration + join table entries + updates counts atomically.

**Example:**

```csharp
// Source: Existing EventService.cs pattern + EF Core transactions
public class RegistrationService
{
    private readonly EventCenterDbContext _context;
    private readonly IEmailSender _emailSender;

    public async Task<(bool Success, int? RegistrationId, string? Error)>
        RegisterMaklerAsync(int eventId, string userEmail, List<int> agendaItemIds)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Load event with optimistic locking
            var evt = await _context.Events
                .Include(e => e.Registrations)
                .Include(e => e.AgendaItems)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            // 2. Validate business rules
            if (evt == null) return (false, null, "Veranstaltung nicht gefunden");
            if (evt.GetCurrentState() != EventState.Public)
                return (false, null, "Anmeldung nicht möglich");
            if (evt.Registrations.Count >= evt.MaxCapacity)
                return (false, null, "Veranstaltung ausgebucht");

            // 3. Create registration
            var registration = new Registration { /* ... */ };
            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync(); // Gets ID

            // 4. Add agenda items (many-to-many)
            foreach (var itemId in agendaItemIds)
            {
                _context.RegistrationAgendaItems.Add(new RegistrationAgendaItem
                {
                    RegistrationId = registration.Id,
                    AgendaItemId = itemId
                });
            }
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // 5. Send email AFTER commit (async, don't block)
            _ = _emailSender.SendRegistrationConfirmationAsync(registration);

            return (true, registration.Id, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return (false, null, "Veranstaltung wurde zwischenzeitlich geändert");
        }
    }
}
```

### Pattern 2: Optimistic Concurrency for Race Conditions

**What:** Use RowVersion concurrency token on Event entity to detect concurrent modifications during registration.

**When to use:** Multiple maklers registering simultaneously for limited capacity events.

**Example:**

```csharp
// Source: Microsoft Learn EF Core Concurrency + existing Event entity
public class Event
{
    // ... existing properties ...

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}

// Configuration (if not using data annotation)
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Event>()
        .Property(e => e.RowVersion)
        .IsRowVersion();
}
```

### Pattern 3: Email Service with IEmailSender Abstraction

**What:** Interface-based email service for testability and configuration flexibility.

**When to use:** Production SMTP via MailKit, test mocks, potential future providers.

**Example:**

```csharp
// Source: MailKit documentation + ASP.NET Core patterns
public interface IEmailSender
{
    Task SendRegistrationConfirmationAsync(Registration registration);
}

public class MailKitEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;

    public async Task SendRegistrationConfirmationAsync(Registration registration)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(new MailboxAddress(
            $"{registration.FirstName} {registration.LastName}",
            registration.Email));
        message.Subject = $"Anmeldebestätigung: {registration.Event.Title}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = BuildConfirmationHtml(registration)
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl);
        await client.AuthenticateAsync(_settings.Username, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}

// appsettings.json
{
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "UseSsl": true,
    "Username": "eventcenter@example.com",
    "Password": "PLACEHOLDER",
    "SenderName": "Veranstaltungscenter",
    "SenderEmail": "eventcenter@example.com"
  }
}
```

### Pattern 4: iCalendar Export with Content-Disposition

**What:** Generate RFC 5545-compliant .ics file and return as file download.

**When to use:** Event detail page iCal export, registration confirmation download.

**Example:**

```csharp
// Source: Ical.NET documentation + ASP.NET Core file results
public class CalendarExportService
{
    public byte[] GenerateEventCalendar(Event evt)
    {
        var calendar = new Ical.Net.Calendar();

        var calEvent = new Ical.Net.CalendarComponents.CalendarEvent
        {
            Summary = evt.Title,
            Description = evt.Description,
            Location = evt.Location,
            Start = new Ical.Net.DataTypes.CalDateTime(evt.StartDateUtc, "UTC"),
            End = new Ical.Net.DataTypes.CalDateTime(evt.EndDateUtc, "UTC"),
            Uid = $"event-{evt.Id}@eventcenter.example.com"
        };

        calendar.Events.Add(calEvent);

        var serializer = new Ical.Net.Serialization.CalendarSerializer();
        var icsContent = serializer.SerializeToString(calendar);
        return Encoding.UTF8.GetBytes(icsContent);
    }
}

// In Blazor page code-behind or API controller
public IActionResult DownloadCalendar(int eventId)
{
    var evt = _eventService.GetEventByIdAsync(eventId).Result;
    var icsBytes = _calendarService.GenerateEventCalendar(evt);

    return File(
        icsBytes,
        "text/calendar",
        $"event-{evt.Id}.ics");
}
```

### Pattern 5: Client-Side Search/Filter with Debouncing

**What:** Instant search with debounced server calls to avoid excessive queries.

**When to use:** Text search in event list as user types.

**Example:**

```razor
@* Source: Blazor patterns + existing EventList.razor *@
<input type="text" class="form-control"
       placeholder="Suche nach Name oder Ort..."
       @bind="searchTerm"
       @bind:event="oninput"
       @bind:after="OnSearchChanged" />

@code {
    private string searchTerm = "";
    private System.Threading.Timer? debounceTimer;

    private void OnSearchChanged()
    {
        debounceTimer?.Dispose();
        debounceTimer = new System.Threading.Timer(_ =>
        {
            InvokeAsync(async () =>
            {
                await LoadEventsAsync();
                StateHasChanged();
            });
        }, null, 300, Timeout.Infinite);
    }

    private async Task LoadEventsAsync()
    {
        // Apply filters to query
        var query = _context.Events
            .Where(e => e.IsPublished)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(e =>
                e.Title.Contains(searchTerm) ||
                e.Location.Contains(searchTerm));
        }

        events = await query.ToListAsync();
    }
}
```

### Anti-Patterns to Avoid

- **Loading all events client-side:** Use server-side filtering and pagination (existing EventService pattern) to avoid memory issues and slow initial load
- **Fire-and-forget email sending without error handling:** Wrap email calls in try-catch, log failures, consider background queue for resilience
- **Checking capacity without optimistic locking:** Race condition where two users register for last slot simultaneously - use RowVersion
- **Storing registration in session before commit:** Browser refresh loses data - use single transaction with immediate persistence
- **Manual iCal string building:** Misses edge cases (timezone DST transitions, escaping special characters) - use Ical.NET library

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| iCalendar generation | Manual .ics string formatting | Ical.NET (RFC 5545 library) | Timezone handling (DST transitions), recurrence rules, escaping special characters (commas, semicolons), VTIMEZONE blocks, UID generation all have edge cases |
| SMTP email sending | Raw TcpClient with SMTP commands | MailKit | SMTP protocol complexity (STARTTLS, AUTH mechanisms, MIME encoding, attachments), connection pooling, error handling, modern security standards |
| Concurrent registration handling | Application-level locks/semaphores | EF Core optimistic concurrency | Distributed system (multiple server instances), database already handles this with row versioning, application locks don't scale across servers |
| Search tokenization | Custom string splitting logic | EF Core Contains() for simple search, SQL Server Full-Text Search for complex | Unicode normalization, diacritics, word boundaries, performance indexing all handled by database |
| HTML email templates | String concatenation | Razor Class Library or templating engine | HTML escaping, responsive design, testing, maintainability - templates should be separate from code |

**Key insight:** Email and calendar standards have decades of edge cases (timezones, encoding, security) that specialized libraries handle. Registration race conditions need database-level solutions because application-level locks don't work across multiple server instances. Building these manually leads to production bugs that are hard to reproduce and fix.

## Common Pitfalls

### Pitfall 1: Race Conditions in Concurrent Registration

**What goes wrong:** Two maklers register for the last available slot simultaneously. Both see "1 spot remaining", both submit, both succeed, capacity exceeded.

**Why it happens:** Time gap between reading capacity (SELECT) and writing registration (INSERT). Without optimistic concurrency, "last writer wins" or both succeed.

**How to avoid:**
1. Add `[Timestamp] public byte[] RowVersion { get; set; }` to Event entity
2. Include Event in query: `Include(e => e.Registrations)` to load RowVersion
3. Wrap in try-catch for `DbUpdateConcurrencyException`
4. Check capacity BEFORE SaveChangesAsync() but INSIDE transaction
5. Show user-friendly error: "Veranstaltung wurde zwischenzeitlich ausgebucht"

**Warning signs:**
- Intermittent capacity violations in production (not reproducible in dev)
- More registrations than MaxCapacity in database
- Users reporting "it said spots were available" but got rejected

### Pitfall 2: Email Deliverability and SPF/DKIM Configuration

**What goes wrong:** Emails sent successfully from code but land in spam folder or get rejected by recipient mail servers.

**Why it happens:** Modern email servers check SPF (Sender Policy Framework) and DKIM (DomainKeys Identified Mail) to prevent spoofing. If your SMTP server IP isn't authorized in DNS records, emails are marked suspicious.

**How to avoid:**
1. Use authenticated SMTP server (not direct SMTP from app server)
2. Configure SPF record in DNS: `v=spf1 include:_spf.example.com ~all`
3. Configure DKIM signing with your email provider
4. Test with mail-tester.com before production
5. Use reputable SMTP provider (Office 365, SendGrid, Mailgun) for production
6. Set proper From address matching domain (not noreply@localhost)

**Warning signs:**
- Test emails arrive but production emails don't
- Users don't receive confirmations but no errors logged
- Emails in spam folder with "via external-server.com" warning
- Bounce-back messages about policy violations

### Pitfall 3: Timezone Confusion in iCalendar Export

**What goes wrong:** Event exported to .ics shows wrong time in user's calendar app (Outlook, Google Calendar, iPhone).

**Why it happens:** Event stored as UTC in database, but .ics file needs explicit timezone or UTC designator. Mixing local time without timezone causes calendar app to assume user's local timezone.

**How to avoid:**
1. Store UTC in database (already done: StartDateUtc, EndDateUtc)
2. For .ics export, use `CalDateTime(utcDateTime, "UTC")` with explicit UTC timezone
3. Or convert to CET and include VTIMEZONE component: `CalDateTime(cetDateTime, "Europe/Berlin")`
4. Test export by opening .ics on different computers in different timezones
5. Use Ical.NET's timezone support: `calendar.AddTimeZone(VTimeZone.FromSystemTimeZone(...))`

**Warning signs:**
- Times correct in web UI but wrong after calendar import
- User reports "event shows wrong time on my phone"
- Times off by exactly N hours (timezone offset)
- DST transition dates show wrong times

### Pitfall 4: Memory Leaks from Event Handler Registration

**What goes wrong:** Blazor Server circuit memory usage grows over time, eventually crashes with OutOfMemoryException.

**Why it happens:** Event handlers (debounce timers, service events) not disposed. Blazor circuit stays alive between navigations, accumulated handlers prevent garbage collection.

**How to avoid:**
1. Implement IDisposable/IAsyncDisposable in components with timers
2. Dispose timers in Dispose(): `debounceTimer?.Dispose();`
3. Unsubscribe from events in Dispose(): `service.SomeEvent -= Handler;`
4. Use weak event patterns for long-lived services
5. Test with repeated navigation: navigate away and back 50 times, check memory

**Warning signs:**
- Memory usage increases with each page navigation
- Circuit disposal errors in logs
- "Cannot access disposed object" exceptions after navigation
- Slow page transitions over time

### Pitfall 5: N+1 Query Problem in Event List

**What goes wrong:** Event list page loads slowly, database shows hundreds of individual SELECT queries for agenda items and registrations.

**Why it happens:** Each event card accesses navigation properties (AgendaItems, Registrations) without eager loading. EF Core lazy loads each separately.

**How to avoid:**
1. Use `.Include(e => e.Registrations)` for registration count
2. Don't include AgendaItems unless actually displayed on list (detail page only)
3. Use `.Select()` projection to load only needed fields
4. Consider computed columns or view models for list data
5. Use EF Core logging to detect N+1: EnableSensitiveDataLogging in development

**Warning signs:**
- Page loads slowly despite few events (< 20)
- SQL profiler shows hundreds of queries for single page load
- Each query retrieves single row
- Performance degrades proportionally with number of events

### Pitfall 6: File Download from Blazor Server Interactivity

**What goes wrong:** iCal download button click does nothing, or triggers full page reload, or downloads empty file.

**Why it happens:** Blazor Server uses SignalR, not HTTP responses. Can't return FileResult from @onclick handler. Need either NavigationManager redirect to endpoint or JavaScript interop.

**How to avoid:**
1. Create minimal API endpoint: `app.MapGet("/api/events/{id}/calendar", ...)`
2. Return File() result from endpoint with proper content-disposition
3. Use `<a href="/api/events/@EventId/calendar">` for download (simple)
4. Or NavigationManager.NavigateTo() from button click
5. Or use IJSRuntime to trigger download via JavaScript blob

**Warning signs:**
- Download button does nothing
- Console errors about "return value not supported"
- Full page reload instead of file download
- File downloads as "download" with no extension

## Code Examples

Verified patterns from official sources and project codebase:

### Event List with Search and Filter

```razor
@* Source: Existing EventList.razor pattern + Blazor best practices *@
@page "/portal/events"
@attribute [Authorize(Roles = "Makler")]
@inject EventService EventService
@inject AuthenticationStateProvider AuthStateProvider

<div class="container-fluid">
    <div class="row mb-3">
        <div class="col-md-6">
            <input type="text" class="form-control"
                   placeholder="Suche nach Name oder Ort..."
                   @bind="searchTerm"
                   @bind:event="oninput"
                   @bind:after="OnSearchChanged" />
        </div>
        <div class="col-md-6">
            <div class="btn-group" role="group">
                <button class="btn btn-outline-primary @(dateFilter == "month" ? "active" : "")"
                        @onclick='() => SetDateFilter("month")'>Diesen Monat</button>
                <button class="btn btn-outline-primary @(dateFilter == "quarter" ? "active" : "")"
                        @onclick='() => SetDateFilter("quarter")'>Nächste 3 Monate</button>
                <button class="btn btn-outline-primary @(dateFilter == "year" ? "active" : "")"
                        @onclick='() => SetDateFilter("year")'>Dieses Jahr</button>
            </div>
        </div>
    </div>

    @if (isLoading)
    {
        <div class="text-center my-5">
            <div class="spinner-border" role="status"></div>
        </div>
    }
    else if (!events.Any())
    {
        <div class="alert alert-info">Keine Veranstaltungen gefunden.</div>
    }
    else
    {
        <div class="row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4">
            @foreach (var evt in events)
            {
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <h5 class="card-title">@evt.Title</h5>
                            <p class="card-text">
                                <small class="text-muted">
                                    <i class="bi bi-calendar"></i> @TimeZoneHelper.FormatDateTimeCet(evt.StartDateUtc, "dd.MM.yyyy")<br/>
                                    <i class="bi bi-geo-alt"></i> @evt.Location
                                </small>
                            </p>
                            <p class="card-text">@GetShortDescription(evt.Description)</p>
                            <span class="badge @GetStatusBadgeClass(evt)">@GetStatusText(evt)</span>
                            @if (GetTotalCost(evt) > 0)
                            {
                                <span class="badge bg-secondary ms-2">@GetTotalCost(evt).ToString("C")</span>
                            }
                        </div>
                        <div class="card-footer">
                            <a href="/portal/events/@evt.Id" class="btn btn-primary btn-sm">Details</a>
                        </div>
                    </div>
                </div>
            }
        </div>
    }
</div>

@code {
    private List<Event> events = new();
    private string searchTerm = "";
    private string dateFilter = "quarter";
    private bool isLoading = true;
    private System.Threading.Timer? debounceTimer;

    protected override async Task OnInitializedAsync()
    {
        await LoadEventsAsync();
        isLoading = false;
    }

    private void OnSearchChanged()
    {
        debounceTimer?.Dispose();
        debounceTimer = new System.Threading.Timer(_ =>
        {
            InvokeAsync(async () =>
            {
                await LoadEventsAsync();
                StateHasChanged();
            });
        }, null, 300, Timeout.Infinite);
    }

    private async Task SetDateFilter(string filter)
    {
        dateFilter = filter;
        await LoadEventsAsync();
    }

    private async Task LoadEventsAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var userEmail = authState.User.Identity?.Name ?? "";

        // Get date range based on filter
        var now = DateTime.UtcNow;
        var endDate = dateFilter switch
        {
            "month" => now.AddMonths(1),
            "quarter" => now.AddMonths(3),
            "year" => now.AddYears(1),
            _ => now.AddMonths(3)
        };

        // Query with filters
        events = await EventService.GetPublicEventsAsync(
            searchTerm: searchTerm,
            startDateFrom: now,
            startDateTo: endDate,
            userEmail: userEmail
        );
    }

    private string GetStatusBadgeClass(Event evt)
    {
        var state = evt.GetCurrentState();
        var isRegistered = evt.Registrations.Any(r => r.Email == userEmail);
        var isFull = evt.Registrations.Count >= evt.MaxCapacity;

        if (isRegistered) return "bg-primary";
        if (state == EventState.Finished || state == EventState.DeadlineReached) return "bg-secondary";
        if (isFull) return "bg-danger";
        return "bg-success";
    }

    private string GetStatusText(Event evt)
    {
        var state = evt.GetCurrentState();
        var isRegistered = evt.Registrations.Any(r => r.Email == userEmail);
        var isFull = evt.Registrations.Count >= evt.MaxCapacity;

        if (isRegistered) return "Angemeldet";
        if (state == EventState.Finished) return "Verpasst";
        if (state == EventState.DeadlineReached) return "Verpasst";
        if (isFull) return "Ausgebucht";
        return "Plätze frei";
    }

    public void Dispose()
    {
        debounceTimer?.Dispose();
    }
}
```

### Registration Service with Optimistic Concurrency

```csharp
// Source: EF Core concurrency documentation + existing EventService pattern
public class RegistrationService
{
    private readonly EventCenterDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        EventCenterDbContext context,
        IEmailSender emailSender,
        ILogger<RegistrationService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<(bool Success, int? RegistrationId, string? ErrorMessage)>
        RegisterMaklerAsync(
            int eventId,
            string userEmail,
            string firstName,
            string lastName,
            string? phone,
            string? company,
            List<int> selectedAgendaItemIds)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Load event with all related data and RowVersion for concurrency
            var evt = await _context.Events
                .Include(e => e.Registrations)
                .Include(e => e.AgendaItems)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (evt == null)
                return (false, null, "Veranstaltung nicht gefunden.");

            // Business rule validation
            var state = evt.GetCurrentState();
            if (state != EventState.Public)
                return (false, null, "Anmeldung nicht möglich - Frist abgelaufen.");

            if (evt.Registrations.Count >= evt.MaxCapacity)
                return (false, null, "Veranstaltung ist ausgebucht.");

            if (evt.Registrations.Any(r => r.Email == userEmail))
                return (false, null, "Sie sind bereits für diese Veranstaltung angemeldet.");

            // Validate agenda items
            var validAgendaIds = evt.AgendaItems
                .Where(a => a.MaklerCanParticipate)
                .Select(a => a.Id)
                .ToList();

            if (selectedAgendaItemIds.Any(id => !validAgendaIds.Contains(id)))
                return (false, null, "Ungültige Agendapunkt-Auswahl.");

            // Create registration
            var registration = new Registration
            {
                EventId = eventId,
                RegistrationType = RegistrationType.Makler,
                FirstName = firstName,
                LastName = lastName,
                Email = userEmail,
                Phone = phone,
                Company = company,
                RegistrationDateUtc = DateTime.UtcNow,
                IsConfirmed = true,
                NumberOfCompanions = 0
            };

            _context.Registrations.Add(registration);

            // SaveChanges to get Registration.Id
            // This is where DbUpdateConcurrencyException would be thrown
            await _context.SaveChangesAsync();

            // Add selected agenda items (many-to-many)
            foreach (var agendaItemId in selectedAgendaItemIds)
            {
                _context.Set<RegistrationAgendaItem>().Add(new RegistrationAgendaItem
                {
                    RegistrationId = registration.Id,
                    AgendaItemId = agendaItemId
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send confirmation email (fire and forget, log errors)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendRegistrationConfirmationAsync(registration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send confirmation email for registration {RegistrationId}", registration.Id);
                }
            });

            return (true, registration.Id, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return (false, null, "Die Veranstaltung wurde zwischenzeitlich geändert. Bitte versuchen Sie es erneut.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during registration for event {EventId}", eventId);
            return (false, null, "Ein Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.");
        }
    }
}
```

### Email Service with MailKit

```csharp
// Source: MailKit documentation + ASP.NET Core IEmailSender pattern
public interface IEmailSender
{
    Task SendRegistrationConfirmationAsync(Registration registration);
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool UseSsl { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
}

public class MailKitEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IOptions<SmtpSettings> settings, ILogger<MailKitEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendRegistrationConfirmationAsync(Registration registration)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(new MailboxAddress(
            $"{registration.FirstName} {registration.LastName}",
            registration.Email));
        message.Subject = $"Anmeldebestätigung: {registration.Event.Title}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = BuildConfirmationHtml(registration)
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();

            // Connect to SMTP server
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl);

            // Authenticate if credentials provided
            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password);
            }

            // Send email
            await client.SendAsync(message);

            // Disconnect
            await client.DisconnectAsync(true);

            _logger.LogInformation("Confirmation email sent to {Email} for registration {RegistrationId}",
                registration.Email, registration.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", registration.Email);
            throw;
        }
    }

    private string BuildConfirmationHtml(Registration registration)
    {
        var evt = registration.Event;
        var startDate = TimeZoneHelper.FormatDateTimeCet(evt.StartDateUtc, "dd.MM.yyyy HH:mm");
        var endDate = TimeZoneHelper.FormatDateTimeCet(evt.EndDateUtc, "dd.MM.yyyy HH:mm");

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0d6efd; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Anmeldebestätigung</h1>
        </div>
        <div class='content'>
            <p>Sehr geehrte(r) {registration.FirstName} {registration.LastName},</p>
            <p>Ihre Anmeldung für die folgende Veranstaltung wurde erfolgreich registriert:</p>

            <h3>{evt.Title}</h3>
            <p>
                <strong>Datum:</strong> {startDate} - {endDate}<br/>
                <strong>Ort:</strong> {evt.Location}
            </p>

            <p>Wir freuen uns auf Ihre Teilnahme!</p>
        </div>
        <div class='footer'>
            <p>Veranstaltungscenter - {_settings.SenderEmail}</p>
        </div>
    </div>
</body>
</html>";
    }
}

// Program.cs registration
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();
```

### iCalendar Export Service

```csharp
// Source: Ical.NET documentation + RFC 5545 examples
public interface ICalendarExportService
{
    byte[] GenerateEventCalendar(Event evt);
}

public class IcalNetCalendarService : ICalendarExportService
{
    public byte[] GenerateEventCalendar(Event evt)
    {
        var calendar = new Ical.Net.Calendar();

        // Set calendar properties
        calendar.Method = "PUBLISH";
        calendar.ProductId = "-//Veranstaltungscenter//EventCenter 1.0//DE";

        // Create event
        var calEvent = new Ical.Net.CalendarComponents.CalendarEvent
        {
            Summary = evt.Title,
            Description = evt.Description ?? string.Empty,
            Location = evt.Location,

            // Use UTC timezone explicitly
            Start = new Ical.Net.DataTypes.CalDateTime(evt.StartDateUtc, "UTC"),
            End = new Ical.Net.DataTypes.CalDateTime(evt.EndDateUtc, "UTC"),

            // Unique identifier
            Uid = $"event-{evt.Id}@eventcenter.example.com",

            // Created and last modified timestamps
            Created = new Ical.Net.DataTypes.CalDateTime(DateTime.UtcNow),
            LastModified = new Ical.Net.DataTypes.CalDateTime(DateTime.UtcNow),

            // Status
            Status = Ical.Net.CalendarComponents.EventStatus.Confirmed
        };

        // Add contact information if available
        if (!string.IsNullOrEmpty(evt.ContactEmail))
        {
            calEvent.Organizer = new Ical.Net.DataTypes.Organizer($"mailto:{evt.ContactEmail}")
            {
                CommonName = evt.ContactName ?? "Veranstaltungscenter"
            };
        }

        calendar.Events.Add(calEvent);

        // Serialize to string
        var serializer = new Ical.Net.Serialization.CalendarSerializer();
        var icsContent = serializer.SerializeToString(calendar);

        return Encoding.UTF8.GetBytes(icsContent);
    }
}

// Minimal API endpoint for download
app.MapGet("/api/events/{eventId:int}/calendar", async (
    int eventId,
    EventService eventService,
    ICalendarExportService calendarService) =>
{
    var evt = await eventService.GetEventByIdAsync(eventId);
    if (evt == null)
        return Results.NotFound();

    var icsBytes = calendarService.GenerateEventCalendar(evt);

    return Results.File(
        icsBytes,
        contentType: "text/calendar",
        fileDownloadName: $"event-{evt.Id}.ics");
});
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| System.Net.Mail.SmtpClient | MailKit | 2016 (SmtpClient marked obsolete) | Modern security (OAuth2, STARTTLS), async support, cross-platform |
| Manual iCal string building | Ical.NET v5 | 2023 (v5 rewrite) | RFC 5545 compliance, timezone handling, performance improvements |
| Application-level locks for concurrency | EF Core optimistic concurrency (RowVersion) | EF Core 1.0+ (2016) | Distributed system support, scales across multiple servers, database-level guarantees |
| Client-side only validation | FluentValidation with server-side + Blazored client-side | 2020+ (Blazored library) | Security (client bypass prevention), consistent validation rules, better UX with instant feedback |
| Separate list/filter API calls | Server-side Blazor with single service | Blazor Server 3.0+ (2019) | Reduced latency, no API boundary overhead, shared state management |

**Deprecated/outdated:**
- System.Net.Mail.SmtpClient: Microsoft recommends MailKit as replacement, lacks modern authentication
- DDay.iCal: Unmaintained predecessor to Ical.NET, compatibility issues with .NET Core
- Manual SQL locking (UPDLOCK, ROWLOCK hints): Overrides EF Core tracking, breaks change tracking, use optimistic concurrency instead
- Session-based cart pattern for registration: Doesn't work with Blazor Server circuits, use component state with single transaction

## Open Questions

1. **Email Retry Strategy for Transient Failures**
   - What we know: MailKit throws on SMTP failures, registration already committed
   - What's unclear: Should we implement outbox pattern with background queue, or accept fire-and-forget with manual admin notification?
   - Recommendation: Start with fire-and-forget + error logging. If production shows frequent email failures, implement hangfire/background job queue in later phase. Trade-off: complexity vs. reliability for non-critical notification.

2. **Search Performance with Large Event Count**
   - What we know: Current LIKE queries work for Phase 1-2 small dataset (< 100 events)
   - What's unclear: At what scale does LIKE performance degrade requiring full-text search?
   - Recommendation: Use EF Core Contains() for v1 (projected < 500 events), add SQL Server Full-Text Search index if search becomes slow. Measure with realistic data during testing.

3. **iCalendar Timezone Strategy: UTC vs CET**
   - What we know: Can export as UTC or include VTIMEZONE component for CET
   - What's unclear: Which approach provides better compatibility across calendar clients (Outlook, Google, Apple)?
   - Recommendation: Use UTC export (`CalDateTime(utcDateTime, "UTC")`) for simplicity. Calendar apps handle timezone conversion automatically. Reduces complexity and potential DST bugs. Test with multiple calendar clients during implementation.

4. **Registration Cancellation Scope**
   - What we know: MCAN-01 (makler cancellation) is in Phase 7, not Phase 3
   - What's unclear: Should Registration entity include IsCancelled flag now for future-proofing, or add in Phase 7?
   - Recommendation: Add IsCancelled + CancellationDateUtc fields now with default false. Low risk, avoids migration later. Cancellation logic implemented in Phase 7.

## Validation Architecture

> Note: workflow.nyquist_validation not found in config.json, assuming validation is desired.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.4.2 + bUnit 1.* (Blazor component testing) |
| Config file | EventCenter.Tests/EventCenter.Tests.csproj |
| Quick run command | `dotnet test --filter "FullyQualifiedName~RegistrationService" --logger "console;verbosity=detailed"` |
| Full suite command | `dotnet test EventCenter.Tests/EventCenter.Tests.csproj` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MLST-01 | Makler sees published events with status | integration | `dotnet test --filter "FullyQualifiedName~EventService\|PublicEvents"` | ❌ Wave 0 |
| MLST-02 | Search by name/location, filter by date | integration | `dotnet test --filter "FullyQualifiedName~EventService\|Search"` | ❌ Wave 0 |
| MLST-03 | Registration status calculation (badges) | unit | `dotnet test --filter "FullyQualifiedName~EventExtensions\|RegistrationStatus"` | ❌ Wave 0 |
| MDET-01 | Event detail page renders correctly | component | `dotnet test --filter "FullyQualifiedName~EventDetail"` | ❌ Wave 0 |
| MDET-02 | Document download with proper headers | integration | `dotnet test --filter "FullyQualifiedName~EventService\|Download"` | ✅ EventServiceTests.cs (extend) |
| MDET-03 | iCalendar export RFC 5545 compliance | unit | `dotnet test --filter "FullyQualifiedName~CalendarExport"` | ❌ Wave 0 |
| MREG-01 | Registration with agenda selection | integration | `dotnet test --filter "FullyQualifiedName~RegistrationService\|CreateRegistration"` | ❌ Wave 0 |
| MREG-02 | Validation: deadline, capacity, duplicate | integration | `dotnet test --filter "FullyQualifiedName~RegistrationService\|Validation"` | ❌ Wave 0 |
| MREG-03 | Cost calculation for selected agenda | unit | `dotnet test --filter "FullyQualifiedName~Registration\|CostCalculation"` | ❌ Wave 0 |
| MREG-04 | Confirmation page displays correctly | component | `dotnet test --filter "FullyQualifiedName~RegistrationConfirmation"` | ❌ Wave 0 |
| MAIL-01 | Email sent after registration | integration | `dotnet test --filter "FullyQualifiedName~EmailService\|Confirmation"` | ❌ Wave 0 |

**Concurrency Testing:**
- **Race condition simulation:** integration test with `Task.WhenAll()` - multiple concurrent registrations for last slot, expect exactly one success
- **Command:** `dotnet test --filter "FullyQualifiedName~RegistrationService\|Concurrency"`
- **File:** ❌ Wave 0 - Services/RegistrationServiceConcurrencyTests.cs

### Sampling Rate

- **Per task commit:** `dotnet test --filter "FullyQualifiedName~{TaskArea}" --logger "console;verbosity=minimal"` (< 30 seconds)
- **Per wave merge:** `dotnet test EventCenter.Tests/EventCenter.Tests.csproj --logger "console;verbosity=normal"` (full suite)
- **Phase gate:** Full suite green + manual smoke test (register, receive email, download iCal) before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `EventCenter.Tests/Services/RegistrationServiceTests.cs` — covers MREG-01, MREG-02, concurrency
- [ ] `EventCenter.Tests/Services/RegistrationServiceConcurrencyTests.cs` — isolated concurrency scenarios
- [ ] `EventCenter.Tests/Services/EmailServiceTests.cs` — covers MAIL-01 with mock SMTP
- [ ] `EventCenter.Tests/Services/CalendarExportServiceTests.cs` — covers MDET-03 RFC compliance
- [ ] `EventCenter.Tests/Components/EventListTests.cs` — covers MLST-01, MLST-02, MLST-03 (bUnit)
- [ ] `EventCenter.Tests/Components/EventDetailTests.cs` — covers MDET-01 (bUnit)
- [ ] `EventCenter.Tests/Components/RegistrationConfirmationTests.cs` — covers MREG-04 (bUnit)
- [ ] `EventCenter.Tests/Validators/RegistrationValidatorTests.cs` — validation rules coverage
- [ ] `EventCenter.Tests/Helpers/TestEmailSender.cs` — mock implementation for tests
- [ ] Extend `EventCenter.Tests/Services/EventServiceTests.cs` — add PublicEvents query tests

**Framework install:** Already present (xUnit + bUnit configured in EventCenter.Tests.csproj)

**Note:** Test strategy mirrors existing patterns from Phase 1/2. Use TestDbContextFactory.CreateInMemoryContext() (SQLite) for integration tests with FK constraints. Use bUnit's TestContext for Blazor component tests with mock AuthenticationStateProvider.

## Sources

### Primary (HIGH confidence)

- [Microsoft Learn: EF Core Concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency) - Optimistic concurrency with RowVersion, DbUpdateConcurrencyException handling
- [Microsoft Learn: ASP.NET Core Blazor Performance](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/?view=aspnetcore-10.0) - Best practices for Blazor Server applications
- [MailKit NuGet](https://www.nuget.org/packages/mailkit/) - Latest version 4.15.0, .NET 8 compatibility confirmed
- [MailKit GitHub](https://github.com/jstedfast/MailKit) - Official documentation and examples
- [Ical.NET NuGet](https://www.nuget.org/packages/Ical.Net/) - Latest version 5.2.1, .NET 8 compatible
- [Ical.NET GitHub](https://github.com/ical-org/ical.net) - RFC 5545 implementation details
- [Blazored.FluentValidation GitHub](https://github.com/Blazored/FluentValidation) - EditForm integration patterns
- Project codebase: EventService.cs, EventForm.razor, TimeZoneHelper.cs, EventValidator.cs - Established architectural patterns

### Secondary (MEDIUM confidence)

- [Mailtrap: ASP.NET Core Send Email](https://mailtrap.io/blog/asp-net-core-send-email/) - MailKit configuration and examples
- [Solving Race Conditions With EF Core Optimistic Locking](https://www.milanjovanovic.tech/blog/solving-race-conditions-with-ef-core-optimistic-locking) - RowVersion implementation patterns
- [End-to-End Server-Side Paging, Sorting, and Filtering in Blazor](https://medium.com/dotnet-new/end-to-end-server-side-paging-sorting-and-filtering-in-blazor-14078b147cc2) - Server-side filtering patterns
- [Generate iCal calendar with .NET using iCAL.NET](https://blog.elmah.io/generate-calendar-in-ical-format-with-net-using-ical-net/) - iCal.NET usage examples
- [Blazor Bootstrap Documentation](https://docs.blazorbootstrap.com/) - Card grid component patterns

### Tertiary (LOW confidence)

- Various blog posts on Blazor patterns and MailKit configuration verified against official docs

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All libraries actively maintained with .NET 8 support, MailKit and Ical.NET are industry standards
- Architecture: HIGH - Patterns verified from project codebase (EventService, TimeZoneHelper) and Microsoft official docs
- Pitfalls: HIGH - Race conditions and email deliverability documented from official EF Core and SMTP sources, timezone issues from RFC 5545
- Email implementation: MEDIUM - MailKit well-documented but SPF/DKIM configuration is deployment-specific
- iCalendar compatibility: MEDIUM - Ical.NET RFC compliant but calendar app testing needed for edge cases

**Research date:** 2026-02-26
**Valid until:** 2026-04-26 (60 days - stable technologies with established best practices)

**Notes:**
- Existing project patterns (service layer, timezone handling, validation) directly applicable
- No breaking changes expected in dependencies (mature libraries)
- Main unknowns are deployment-specific (SMTP configuration, performance at scale)
- Test infrastructure ready (xUnit + bUnit configured)
