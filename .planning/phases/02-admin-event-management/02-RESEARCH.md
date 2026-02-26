# Phase 2: Admin Event Management - Research

**Researched:** 2026-02-26
**Domain:** ASP.NET Core Blazor Server CRUD Forms, File Upload, Data Tables
**Confidence:** HIGH

## Summary

Phase 02 enables admins to create, configure, and publish in-person events with agenda items and extra options. The phase builds on the existing Blazor Server foundation from Phase 01 and leverages FluentValidation, EF Core, and Bootstrap for the admin UI.

Key technical challenges include: (1) inline editing of agenda items and extra options without multi-step wizards, (2) file upload for event documents with proper security, (3) data table display with sorting/pagination for event lists, (4) preventing deletion of extra options that have already been booked, and (5) calculating EventState automatically based on current date and event dates.

The existing codebase already has a solid foundation with Event, EventAgendaItem, and EventOption entities, TimeZoneHelper for CET/UTC conversions, FluentValidation setup, and SQLite-based integration tests. However, the Event entity needs additional fields for contact person information and file storage.

**Primary recommendation:** Use single-page EditForm with clear sections (not wizard), inline list editing for agenda items/options, Blazor InputFile component for document uploads stored in filesystem, and custom calculated properties for EventState display. Leverage existing FluentValidation patterns and extend with nested validators for collections.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Single-page form with clear sections** (basic info, dates, agenda, options) — no multi-step wizard
- **Admin enters all dates in CET**; system converts to UTC behind the scenes using existing TimeZoneHelper
- **Include simple file upload for event documents** (PDFs, flyers) — brokers will download these in Phase 3 (MDET-02)
- **Include explicit contact person fields** (name, email, phone) — not derived from creating admin
- **Data table layout with sortable columns**: Title, Date, Location, Status badge, Registration count (x/max), Published indicator
- **Default view shows upcoming/active events only**; toggle/tab to see past/finished events
- **Event duplication action available** — copy event with all agenda items and options, admin adjusts dates/details
- **Agenda items managed inline on the event form as a sub-section**
- **Sorted by StartDateTime (chronological)** — no manual drag-to-reorder
- **Each agenda item has two toggles**: "Makler can participate" and "Guests can participate" (both on by default, per AGND-03)
- **Extra options (Zusatzoptionen) follow the same inline pattern**, as a separate sub-section below agenda items
- **Publish action requires confirmation dialog** ("This event will be visible to all brokers. Continue?")
- **EventState (Public, DeadlineReached, Finished) is fully automatic** — calculated from current date vs event dates. Admin cannot override state, only controls IsPublished
- **Color-coded status badges**: Draft=gray, Public=green, DeadlineReached=orange, Finished=blue
- **Unpublishing blocked if registrations exist** — admin must cancel all registrations first before unpublishing

### Claude's Discretion
- Exact form field layout, spacing, and section ordering
- Validation error message placement and styling
- File upload implementation details (storage location, size limits)
- Table pagination strategy and page size
- Confirmation dialog design

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| EVNT-01 | Admin kann Präsenzveranstaltung anlegen (US-01) | Blazor EditForm + FluentValidation for form handling; InputFile component for document uploads; EF Core DbContext for persistence |
| EVNT-02 | Admin kann Veranstaltung bearbeiten (US-02) | Same EditForm component in edit mode; load existing Event entity via EF Core; warning display when registrations exist |
| EVNT-03 | Admin kann Veranstaltung veröffentlichen/zurückziehen (US-03) | Boolean IsPublished property; confirmation dialog using Bootstrap modal; validation check for existing registrations before unpublish |
| EVNT-04 | System berechnet EventState automatisch (Public, DeadlineReached, Finished) | Computed property pattern in C# using TimeZoneHelper; status badge component with color coding |
| AGND-01 | Admin kann Agendapunkt anlegen mit Kosten für Makler/Gäste (US-05) | Inline list editing pattern; nested collection in EditForm; FluentValidation for nested objects |
| AGND-02 | Admin kann Agendapunkt bearbeiten und löschen (US-06) | Inline edit/delete actions; cascade delete via EF Core relationship |
| AGND-03 | Admin kann Teilnahme für Makler oder Gäste pro Agendapunkt deaktivieren | Boolean toggles (MaklerCanParticipate, GuestsCanParticipate) in EventAgendaItem entity; inline editing pattern |
| XOPT-01 | Admin kann Zusatzoptionen anlegen, bearbeiten und löschen (US-07) | Same inline editing pattern as agenda items; EventOption entity already exists |
| XOPT-02 | System verhindert Löschen bereits gebuchter Zusatzoptionen | DeleteBehavior.Restrict in EF Core; check for related Registration records before delete; display error message |

</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Blazored.FluentValidation | 2.* | FluentValidation integration for Blazor EditForm | Standard for Blazor form validation with FluentValidation; already in use in Phase 01 |
| TimeZoneConverter | 6.* | Cross-platform timezone handling | Already implemented in Phase 01; needed for CET/UTC conversion |
| Microsoft.AspNetCore.Components.Forms | 8.0.* | InputFile component for file uploads | Built-in Blazor component for file uploads; part of .NET SDK |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.* | Database persistence | Already in use; provides cascade delete and relationship management |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap 5 | (via CDN) | Modal dialogs, badges, responsive tables | Already in use for UI; needed for confirmation dialogs and status badges |
| None needed | — | Server-side pagination | EF Core Skip()/Take() provides sufficient pagination; no additional library needed for admin UI |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Blazored.FluentValidation | Blazilla | Blazilla is newer (2025+) with better nested object support, but Blazored is already integrated and working |
| Local filesystem | Database BLOB storage | Filesystem is simpler for document storage; database BLOBs add complexity without clear benefit for this use case |
| Custom data table | MudBlazor/Radzen DataGrid | Third-party grids offer rich features but add dependencies; simple table with server-side sorting is sufficient for admin UI |

**Installation:**
```bash
# All required packages already installed in Phase 01
# No additional NuGet packages needed for Phase 02
```

## Architecture Patterns

### Recommended Project Structure
```
EventCenter.Web/
├── Components/
│   ├── Pages/
│   │   └── Admin/
│   │       ├── Events/
│   │       │   ├── EventList.razor           # List view with table
│   │       │   ├── EventForm.razor           # Create/Edit form
│   │       │   └── _EventStatusBadge.razor   # Reusable status badge component
│   │       └── Index.razor
│   └── Shared/
│       └── ConfirmDialog.razor               # Reusable confirmation dialog
├── Domain/
│   ├── Entities/
│   │   ├── Event.cs                          # Add ContactName, ContactEmail, ContactPhone, DocumentsPaths
│   │   ├── EventAgendaItem.cs                # Add MaklerCanParticipate, GuestsCanParticipate
│   │   └── EventOption.cs                    # No changes needed
│   └── Extensions/
│       └── EventExtensions.cs                # Computed EventState logic
├── Validators/
│   ├── EventValidator.cs                     # Extend with contact and file validation
│   ├── EventAgendaItemValidator.cs           # New validator for agenda items
│   └── EventOptionValidator.cs               # New validator for extra options
└── wwwroot/
    └── uploads/
        └── events/                           # File storage directory
```

### Pattern 1: Computed EventState Property
**What:** Calculate EventState dynamically based on IsPublished, current date, and event dates
**When to use:** Display status in UI; never store in database
**Example:**
```csharp
// EventExtensions.cs
public static class EventExtensions
{
    public static EventState GetCurrentState(this Event evt)
    {
        if (!evt.IsPublished)
            return EventState.NotPublished;

        var nowCet = TimeZoneHelper.ConvertUtcToCet(DateTime.UtcNow);
        var endCet = TimeZoneHelper.ConvertUtcToCet(evt.EndDateUtc);

        if (endCet < nowCet.Date)
            return EventState.Finished;

        var deadlineEndOfDayUtc = TimeZoneHelper.GetEndOfDayCetAsUtc(
            TimeZoneHelper.ConvertUtcToCet(evt.RegistrationDeadlineUtc));

        if (DateTime.UtcNow > deadlineEndOfDayUtc)
            return EventState.DeadlineReached;

        return EventState.Public;
    }

    public static int GetCurrentRegistrationCount(this Event evt)
    {
        return evt.Registrations?.Count ?? 0;
    }
}
```

### Pattern 2: Inline List Editing with EditForm
**What:** Embed agenda items and extra options as editable lists within the main event form
**When to use:** Managing child collections without leaving the parent form
**Example:**
```razor
<!-- EventForm.razor -->
<EditForm Model="@Model" OnValidSubmit="@HandleValidSubmit">
    <FluentValidationValidator />

    <!-- Basic Event Info Section -->
    <div class="form-section">
        <h3>Grunddaten</h3>
        <!-- Title, Location, Dates, etc. -->
    </div>

    <!-- Agenda Items Section -->
    <div class="form-section">
        <h3>Agendapunkte</h3>
        <button type="button" @onclick="AddAgendaItem">Hinzufügen</button>

        @foreach (var item in Model.AgendaItems.OrderBy(a => a.StartDateTimeUtc))
        {
            <div class="agenda-item">
                <InputText @bind-Value="item.Title" />
                <InputDate @bind-Value="item.StartDateTimeUtc" />
                <InputNumber @bind-Value="item.CostForMakler" />
                <InputCheckbox @bind-Value="item.MaklerCanParticipate" />
                <InputCheckbox @bind-Value="item.GuestsCanParticipate" />
                <button type="button" @onclick="() => RemoveAgendaItem(item)">Löschen</button>
            </div>
        }
    </div>

    <button type="submit">Speichern</button>
</EditForm>

@code {
    private void AddAgendaItem()
    {
        Model.AgendaItems.Add(new EventAgendaItem
        {
            MaklerCanParticipate = true,
            GuestsCanParticipate = true
        });
    }
}
```

### Pattern 3: File Upload with Security
**What:** Use InputFile component with size limits, type validation, and filesystem storage
**When to use:** Uploading event documents (PDFs, flyers)
**Example:**
```razor
<!-- EventForm.razor -->
<div class="form-section">
    <h4>Dokumente</h4>
    <InputFile OnChange="@HandleFileSelection" multiple accept=".pdf,.jpg,.png" />
    <ul>
        @foreach (var file in Model.DocumentPaths ?? Enumerable.Empty<string>())
        {
            <li>@Path.GetFileName(file) <button @onclick="() => RemoveFile(file)">Entfernen</button></li>
        }
    </ul>
</div>

@code {
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    private async Task HandleFileSelection(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles())
        {
            if (file.Size > MaxFileSize)
            {
                // Show error
                continue;
            }

            var uploadPath = Path.Combine("wwwroot", "uploads", "events", Model.Id.ToString());
            Directory.CreateDirectory(uploadPath);

            var fileName = $"{Guid.NewGuid()}_{file.Name}";
            var filePath = Path.Combine(uploadPath, fileName);

            await using var stream = file.OpenReadStream(MaxFileSize);
            await using var fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream);

            Model.DocumentPaths.Add($"/uploads/events/{Model.Id}/{fileName}");
        }
    }
}
```

### Pattern 4: Bootstrap Confirmation Dialog
**What:** Modal dialog for publish/unpublish actions requiring confirmation
**When to use:** Critical actions that cannot be easily undone
**Example:**
```razor
<!-- ConfirmDialog.razor -->
<div class="modal fade @(Show ? "show d-block" : "")" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">@Title</h5>
            </div>
            <div class="modal-body">
                <p>@Message</p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" @onclick="OnCancel">Abbrechen</button>
                <button type="button" class="btn btn-primary" @onclick="OnConfirm">@ConfirmText</button>
            </div>
        </div>
    </div>
</div>
@if (Show)
{
    <div class="modal-backdrop fade show"></div>
}

@code {
    [Parameter] public bool Show { get; set; }
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public string Message { get; set; } = "";
    [Parameter] public string ConfirmText { get; set; } = "Bestätigen";
    [Parameter] public EventCallback OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
}
```

### Pattern 5: Server-Side Pagination and Sorting
**What:** Use EF Core Skip/Take with query parameters for efficient data loading
**When to use:** Event list with potentially many records
**Example:**
```csharp
// EventList.razor.cs
public class EventListModel
{
    public async Task<List<Event>> LoadEvents(int page, int pageSize, string sortColumn, bool showPast)
    {
        var query = _context.Events
            .Include(e => e.Registrations)
            .AsQueryable();

        if (!showPast)
        {
            query = query.Where(e => e.EndDateUtc >= DateTime.UtcNow);
        }

        query = sortColumn switch
        {
            "Title" => query.OrderBy(e => e.Title),
            "Date" => query.OrderBy(e => e.StartDateUtc),
            "Location" => query.OrderBy(e => e.Location),
            _ => query.OrderByDescending(e => e.StartDateUtc)
        };

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
```

### Anti-Patterns to Avoid
- **Loading entire Event collection into memory for display**: Use Skip/Take for pagination
- **Storing EventState in database**: Always calculate dynamically to avoid stale data
- **Public setters on Event.AgendaItems**: Use methods like AddAgendaItem/RemoveAgendaItem to maintain invariants
- **Reading entire file stream into memory**: Use streaming for file uploads
- **Allowing file upload without type/size validation**: Security risk and DoS vulnerability

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Form validation with nested objects | Custom validation logic | FluentValidation with nested validators | Handles complex validation rules, conditional logic, and localization; tested by community |
| Timezone conversion | Manual UTC offset calculations | TimeZoneHelper with TimeZoneConverter | Handles DST transitions correctly; cross-platform (Windows/Linux) |
| File upload security | Custom file type checking | InputFile with OpenReadStream limits + Path.GetExtension | Built-in size limits; prevents path traversal attacks |
| Cascade delete prevention | Manual FK checks before delete | EF Core DeleteBehavior.Restrict | Database-level constraint enforcement; prevents race conditions |
| Date range validation | Custom comparison logic | FluentValidation rules with .GreaterThan() | Handles edge cases; provides clear error messages |

**Key insight:** Blazor Server form handling with FluentValidation is mature and handles complex scenarios (nested objects, async validation, conditional rules) that would take significant effort to replicate correctly. File uploads have security implications (DoS, malicious files, path traversal) that are better handled by framework components with established security patterns.

## Common Pitfalls

### Pitfall 1: Circuit-Based Form State Loss
**What goes wrong:** User fills out form, SignalR circuit disconnects, form state is lost
**Why it happens:** Blazor Server maintains state in memory on the server; circuit failures lose state
**How to avoid:**
- Use [PersistentState] attribute on form model properties (requires .NET 10+, not available in .NET 8)
- Or implement auto-save to database as user fills form
- Or show warning message when circuit reconnects
**Warning signs:** Users report losing form data after network issues or server restarts

### Pitfall 2: File Upload Without Stream Limits
**What goes wrong:** Attacker uploads very large file, server runs out of memory or disk space
**Why it happens:** Default file upload reads entire stream into memory
**How to avoid:**
- Always call OpenReadStream with maxAllowedSize parameter
- Set max file size (e.g., 10 MB for documents)
- Store files in dedicated directory with disk quota
- Scan files with anti-virus after upload
**Warning signs:** Server memory usage spikes during file uploads; OutOfMemoryException

### Pitfall 3: Race Condition on Delete with Related Records
**What goes wrong:** Admin deletes EventOption while broker is selecting it for registration; FK constraint violation
**Why it happens:** No transaction isolation between read (form load) and write (delete)
**How to avoid:**
- Use DeleteBehavior.Restrict on EventOption relationship
- Check for related Registrations before delete: `if (option.Registrations.Any())`
- Display error: "Diese Zusatzoption kann nicht gelöscht werden, da bereits Buchungen existieren"
- Consider soft delete (IsDeleted flag) instead of hard delete
**Warning signs:** Intermittent SqlException with FK constraint violations

### Pitfall 4: Stale EventState Display
**What goes wrong:** Event shows "Public" status but deadline has passed; state is stale
**Why it happens:** EventState calculated once and cached in UI; doesn't update automatically
**How to avoid:**
- Always calculate EventState on-demand using GetCurrentState() extension method
- Never store EventState in database
- Use @bind-Value:after to recalculate state when dates change in form
- Consider timer for auto-refresh on event list page (every 60 seconds)
**Warning signs:** Users report seeing events with wrong status; status only updates after page refresh

### Pitfall 5: Timezone Confusion in Date Inputs
**What goes wrong:** Admin enters "15.03.2026 14:00" in form, gets saved as different time in database
**Why it happens:** Blazor InputDate uses browser's local time; needs explicit CET conversion
**How to avoid:**
- Display clear label: "Startdatum (CET)"
- Convert InputDate value to UTC before saving: `TimeZoneHelper.ConvertCetToUtc(cetDateTime)`
- Show confirmation message with both CET and UTC times
- Test with browser in different timezones
**Warning signs:** Event times appear wrong to users in different timezones; off-by-one-hour errors during DST transitions

### Pitfall 6: Cascade Delete Destroying Data
**What goes wrong:** Admin deletes Event, all agenda items and registrations cascade delete
**Why it happens:** EF Core DeleteBehavior.Cascade on Event relationships (intentional)
**How to avoid:**
- This is CORRECT behavior for Event → AgendaItems (part of aggregate)
- But PREVENT unpublishing/deleting Event if Registrations exist
- Check before delete: `if (evt.Registrations.Any()) throw new InvalidOperationException("...")`
- Show warning in UI before delete action
**Warning signs:** Users report registrations disappearing after event deletion

## Code Examples

Verified patterns from official sources and existing codebase:

### EventState Calculation (Computed Property)
```csharp
// EventExtensions.cs
public static EventState GetCurrentState(this Event evt)
{
    if (!evt.IsPublished)
        return EventState.NotPublished;

    var nowUtc = DateTime.UtcNow;

    // Check if event has finished
    if (evt.EndDateUtc < nowUtc)
        return EventState.Finished;

    // Check if registration deadline has passed (inclusive end-of-day)
    var deadlineCet = TimeZoneHelper.ConvertUtcToCet(evt.RegistrationDeadlineUtc);
    var deadlineEndOfDayUtc = TimeZoneHelper.GetEndOfDayCetAsUtc(deadlineCet);

    if (nowUtc > deadlineEndOfDayUtc)
        return EventState.DeadlineReached;

    return EventState.Public;
}
```

### FluentValidation for Nested Collections
```csharp
// EventValidator.cs (extend existing)
public class EventValidator : AbstractValidator<Event>
{
    public EventValidator()
    {
        // Existing rules...

        RuleFor(e => e.ContactEmail)
            .EmailAddress().When(e => !string.IsNullOrEmpty(e.ContactEmail))
            .WithMessage("Ungültige E-Mail-Adresse");

        RuleFor(e => e.ContactPhone)
            .MaximumLength(50)
            .WithMessage("Telefonnummer darf maximal 50 Zeichen lang sein");

        RuleForEach(e => e.AgendaItems)
            .SetValidator(new EventAgendaItemValidator());

        RuleForEach(e => e.EventOptions)
            .SetValidator(new EventOptionValidator());
    }
}

// EventAgendaItemValidator.cs (new)
public class EventAgendaItemValidator : AbstractValidator<EventAgendaItem>
{
    public EventAgendaItemValidator()
    {
        RuleFor(a => a.Title)
            .NotEmpty().WithMessage("Titel ist erforderlich")
            .MaximumLength(200);

        RuleFor(a => a.EndDateTimeUtc)
            .GreaterThan(a => a.StartDateTimeUtc)
            .WithMessage("Endzeitpunkt muss nach Startzeitpunkt liegen");

        RuleFor(a => a.CostForMakler)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Kosten dürfen nicht negativ sein");
    }
}
```

### Prevent Delete of Booked Options
```csharp
// EventService.cs or in component code-behind
public async Task<bool> DeleteEventOption(int optionId)
{
    var option = await _context.EventOptions
        .Include(o => o.Registrations)
        .FirstOrDefaultAsync(o => o.Id == optionId);

    if (option == null)
        return false;

    if (option.Registrations.Any())
    {
        throw new InvalidOperationException(
            "Diese Zusatzoption kann nicht gelöscht werden, da bereits Buchungen existieren.");
    }

    _context.EventOptions.Remove(option);
    await _context.SaveChangesAsync();
    return true;
}
```

### Event Duplication
```csharp
// EventService.cs
public async Task<Event> DuplicateEvent(int sourceEventId)
{
    var source = await _context.Events
        .Include(e => e.AgendaItems)
        .Include(e => e.EventOptions)
        .FirstOrDefaultAsync(e => e.Id == sourceEventId);

    if (source == null)
        throw new ArgumentException("Event not found");

    var duplicate = new Event
    {
        Title = $"{source.Title} (Kopie)",
        Description = source.Description,
        Location = source.Location,
        MaxCapacity = source.MaxCapacity,
        MaxCompanions = source.MaxCompanions,
        ContactName = source.ContactName,
        ContactEmail = source.ContactEmail,
        ContactPhone = source.ContactPhone,
        IsPublished = false, // Always start as draft
        // Dates need to be adjusted by admin
        StartDateUtc = source.StartDateUtc.AddMonths(1),
        EndDateUtc = source.EndDateUtc.AddMonths(1),
        RegistrationDeadlineUtc = source.RegistrationDeadlineUtc.AddMonths(1)
    };

    // Copy agenda items
    foreach (var item in source.AgendaItems)
    {
        duplicate.AgendaItems.Add(new EventAgendaItem
        {
            Title = item.Title,
            Description = item.Description,
            CostForMakler = item.CostForMakler,
            CostForGuest = item.CostForGuest,
            MaklerCanParticipate = item.MaklerCanParticipate,
            GuestsCanParticipate = item.GuestsCanParticipate,
            IsMandatory = item.IsMandatory,
            MaxParticipants = item.MaxParticipants,
            // Dates relative to event dates
            StartDateTimeUtc = duplicate.StartDateUtc.Add(item.StartDateTimeUtc - source.StartDateUtc),
            EndDateTimeUtc = duplicate.EndDateUtc.Add(item.EndDateTimeUtc - source.EndDateUtc)
        });
    }

    // Copy extra options (but NOT registrations)
    foreach (var option in source.EventOptions)
    {
        duplicate.EventOptions.Add(new EventOption
        {
            Name = option.Name,
            Description = option.Description,
            Price = option.Price,
            MaxQuantity = option.MaxQuantity
        });
    }

    _context.Events.Add(duplicate);
    await _context.SaveChangesAsync();

    return duplicate;
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Blazored.FluentValidation | Blazilla (newer alternative) | 2025 | Blazilla has better nested object support and performance, but Blazored is still maintained and works well |
| Client-side pagination only | Server-side pagination with Skip/Take | Always preferred | Essential for scalability with large datasets; reduces memory usage |
| Manual timezone math | TimeZoneConverter package | Phase 01 | Cross-platform DST handling; eliminates timezone bugs |
| Storing files in database BLOBs | Filesystem storage with path references | Current best practice | Better performance; easier backup; simpler code |
| Multi-step wizard forms | Single-page sectioned forms | UX trend 2024+ | Faster for power users; better for forms with interdependent fields |

**Deprecated/outdated:**
- **Blazor WebAssembly for admin UI**: Blazor Server is simpler for CRUD forms with direct database access; no need for API layer
- **jQuery-based modals**: Bootstrap 5 modals work natively with Blazor without jQuery dependency
- **Custom file upload handlers**: InputFile component handles multipart/form-data automatically

## Open Questions

1. **File Upload Storage Strategy**
   - What we know: InputFile component works; filesystem storage is standard
   - What's unclear: Should files be organized by event ID or date? How to handle file cleanup when event is deleted?
   - Recommendation: Store in `/wwwroot/uploads/events/{eventId}/` structure; implement background cleanup job (Phase 7) to remove files from deleted events

2. **Event List Auto-Refresh**
   - What we know: EventState needs to update when deadline passes
   - What's unclear: Should event list auto-refresh on a timer or require manual refresh?
   - Recommendation: Manual refresh for Phase 02; consider SignalR timer in Phase 7 if users report stale data issues

3. **Agenda Item Time Validation**
   - What we know: Agenda items have start/end times that should fall within event dates
   - What's unclear: Should this be a hard validation error or just a warning?
   - Recommendation: Warning only, not hard error; allow flexibility for pre-event networking or post-event activities

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.6.6 |
| Config file | EventCenter.Tests/EventCenter.Tests.csproj (existing) |
| Quick run command | `dotnet test EventCenter.Tests/EventCenter.Tests.csproj --filter "FullyQualifiedName~Phase02" --no-build` |
| Full suite command | `dotnet test EventCenter.Tests/EventCenter.Tests.csproj` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| EVNT-01 | Admin can create new event with all required fields | integration | `dotnet test --filter "FullyQualifiedName~EventCreationTests" -x` | ❌ Wave 0 |
| EVNT-01 | Contact person fields validated (email format) | unit | `dotnet test --filter "FullyQualifiedName~EventValidator" -x` | ❌ Wave 0 |
| EVNT-02 | Admin can edit existing event | integration | `dotnet test --filter "FullyQualifiedName~EventEditTests" -x` | ❌ Wave 0 |
| EVNT-02 | Warning shown when editing event with registrations | integration | `dotnet test --filter "FullyQualifiedName~EventEditTests" -x` | ❌ Wave 0 |
| EVNT-03 | Admin can publish/unpublish event | integration | `dotnet test --filter "FullyQualifiedName~EventPublishTests" -x` | ❌ Wave 0 |
| EVNT-03 | Unpublish blocked if registrations exist | integration | `dotnet test --filter "FullyQualifiedName~EventPublishTests" -x` | ❌ Wave 0 |
| EVNT-04 | EventState calculated correctly for all scenarios | unit | `dotnet test --filter "FullyQualifiedName~EventStateTests" -x` | ❌ Wave 0 |
| AGND-01 | Admin can add agenda item with costs | integration | `dotnet test --filter "FullyQualifiedName~AgendaItemTests" -x` | ❌ Wave 0 |
| AGND-02 | Admin can edit/delete agenda item | integration | `dotnet test --filter "FullyQualifiedName~AgendaItemTests" -x` | ❌ Wave 0 |
| AGND-03 | Participation toggles work correctly | unit | `dotnet test --filter "FullyQualifiedName~AgendaItemTests" -x` | ❌ Wave 0 |
| XOPT-01 | Admin can add/edit/delete extra options | integration | `dotnet test --filter "FullyQualifiedName~ExtraOptionTests" -x` | ❌ Wave 0 |
| XOPT-02 | Delete blocked if option is booked | integration | `dotnet test --filter "FullyQualifiedName~ExtraOptionTests" -x` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~{TaskName}" -x` (fail fast on first error)
- **Per wave merge:** `dotnet test EventCenter.Tests/EventCenter.Tests.csproj` (full suite)
- **Phase gate:** Full suite green + manual smoke test before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `EventCenter.Tests/EventManagementTests.cs` — covers EVNT-01, EVNT-02, EVNT-03 (create, edit, publish/unpublish)
- [ ] `EventCenter.Tests/EventStateCalculationTests.cs` — covers EVNT-04 (state calculation logic)
- [ ] `EventCenter.Tests/AgendaItemManagementTests.cs` — covers AGND-01, AGND-02, AGND-03 (agenda CRUD and toggles)
- [ ] `EventCenter.Tests/ExtraOptionManagementTests.cs` — covers XOPT-01, XOPT-02 (option CRUD and delete prevention)
- [ ] `EventCenter.Tests/Validators/EventAgendaItemValidatorTests.cs` — unit tests for agenda item validation
- [ ] `EventCenter.Tests/Validators/EventOptionValidatorTests.cs` — unit tests for extra option validation

## Sources

### Primary (HIGH confidence)
- [Blazored.FluentValidation GitHub](https://github.com/Blazored/FluentValidation) - Integration patterns for Blazor EditForm
- [Microsoft Learn: ASP.NET Core Blazor file uploads](https://learn.microsoft.com/en-us/aspnet/core/blazor/file-uploads?view=aspnetcore-10.0) - Official InputFile documentation
- [Microsoft Learn: ASP.NET Core Blazor forms validation](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/validation?view=aspnetcore-10.0) - Form validation patterns
- [Microsoft Learn: Cascade Delete - EF Core](https://learn.microsoft.com/en-us/ef/core/saving/cascade-delete) - DeleteBehavior configuration
- Existing codebase: EventCenter.Web/Validators/EventValidator.cs - Current validation patterns

### Secondary (MEDIUM confidence)
- [Medium: End-to-End Server-Side Paging, Sorting, and Filtering in Blazor](https://medium.com/dotnet-new/end-to-end-server-side-paging-sorting-and-filtering-in-blazor-14078b147cc2) - Server-side pagination patterns (Dec 2025)
- [Medium: Mapping Domain-Driven Design Concepts To The Database With EF Core](https://medium.com/startup-insider-edge/mapping-domain-driven-design-concepts-to-the-database-with-ef-core-4bfd3f0aa146) - DDD with EF Core patterns (Feb 2026)
- [Blazor Bootstrap: ConfirmDialog Component](https://docs.blazorbootstrap.com/components/confirm-dialog) - Confirmation dialog patterns

### Tertiary (LOW confidence)
- [Blazilla GitHub](https://github.com/loresoft/Blazilla) - Alternative FluentValidation library (newer, less mature)
- Various WebSearch results for inline editing patterns - Multiple vendor solutions, not standardized

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All packages already in use from Phase 01; no new dependencies
- Architecture: HIGH - Patterns align with existing codebase (EditForm, FluentValidation, TimeZoneHelper)
- Pitfalls: HIGH - Based on official docs and known Blazor Server issues (circuit state, file upload, race conditions)
- File upload: MEDIUM - Standard approach but need to test size limits and security in production environment
- Inline editing: MEDIUM - Pattern is well-established but needs careful UX design for nested collections

**Research date:** 2026-02-26
**Valid until:** 2026-03-28 (30 days for stable stack; Blazor 8.0 patterns are stable)
