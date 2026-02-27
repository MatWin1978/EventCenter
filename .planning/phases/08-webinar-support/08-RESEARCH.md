# Phase 8: Webinar Support - Research

**Researched:** 2026-02-27
**Domain:** Blazor Server — polymorphic event type extension (discriminator column pattern), Bootstrap tab UI, conditional form field rendering
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Admin creation flow**
- Type selector at the top of the create/edit form: "In-Person / Webinar" — switching type shows/hides relevant fields
- External registration URL field is required to publish (not to save as draft); can't publish a webinar without a URL
- Hide registration deadline and capacity fields for webinars — not applicable
- No agenda section for webinars — hidden entirely

**Visual differentiation**
- Webinar events display a Bootstrap badge with `bi-camera-video` icon and text "Webinar" (e.g., `badge bg-info`)
- Badge appears in both the admin event list and the broker portal event list
- On the webinar event detail page (broker view): show a prominent webinar banner/header callout at the top of the page (in addition to replacing the registration form)

**Filtering behavior**
- Tab bar above event list: **All / In-Person / Webinar**
- Additive with other filters — if search is active, the tab narrows within those results
- Default tab is **All**
- Both admin event management list and broker portal event list get the filter tabs

**External link UX**
- Webinar detail page shows a prominent CTA button ("Zur Webinar-Anmeldung") opening the external URL in a new tab
- Button only — no explanatory text about external registration
- iCal calendar export button remains on webinar event detail page
- If a broker navigates to `/portal/events/{id}/register` for a webinar: redirect to the event detail page

### Claude's Discretion
- Exact banner/callout styling on the webinar detail page header
- Badge color choice (bg-info vs other Bootstrap color)
- Which fields beyond deadline and capacity are hidden/shown for webinars

### Deferred Ideas (OUT OF SCOPE)
- None — discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| WBNR-01 | Admin kann Webinar anlegen und bearbeiten (US-04) | Discriminator column on Event entity + EventForm.razor conditional field rendering + EventValidator extension + EventService.CreateEventAsync/UpdateEventAsync already exist |
| WBNR-02 | Admin kann Webinar veröffentlichen/zurückziehen (US-04) | EventService.PublishEventAsync already exists but needs webinar-specific publish guard (URL required); UnpublishEventAsync already works for webinars (no registrations to block) |
</phase_requirements>

---

## Summary

Phase 8 adds webinar as a second event type alongside in-person events. The implementation is a disciplined extension of the existing Event entity and UI — no new aggregate, no new service class. The key change is adding two new nullable columns to the Events table: `EventType` (discriminator, string enum) and `ExternalRegistrationUrl` (nullable string, required for webinar publish).

The existing `EventForm.razor`, `EventList.razor` (admin and portal), `EventDetail.razor`, `EventCard.razor`, and `EventValidator` all need targeted modifications. The EventService publish guard needs a new check: webinar events cannot publish without a URL. The EventRegistration page needs a redirect guard. No new pages are required — only modifications to existing components plus one EF Core migration.

The filter tab bars (All / In-Person / Webinar) are additive client-side filters: the existing `GetPublicEventsAsync` and `GetEventsAsync` queries already load all events, so the tab can filter the in-memory list without a new DB query. The admin list uses paginated server-side queries, so a type filter parameter must be added to `GetEventsAsync` and `GetEventCountAsync`.

**Primary recommendation:** Add `EventType` (enum, stored as string) and `ExternalRegistrationUrl` to the `Event` entity. Extend all existing components conditionally. Do NOT create new services or pages — modify existing ones.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Blazor Server | .NET 8 | UI framework | Already in use throughout project |
| Entity Framework Core | .NET 8 / SQL Server | ORM + migrations | Already in use — `EventCenterDbContext` |
| FluentValidation | (existing) | Model validation | Already in use — `EventValidator`, `Blazored.FluentValidation` |
| Bootstrap 5 + Bootstrap Icons | (existing) | Tab bar, badges, icons | Already in use — `bi-camera-video` is available |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| FluentValidation.DependencyInjectionExtensions | (existing) | Auto-discover validators | Needed if adding `WebinarEventValidator` (optional — can extend EventValidator instead) |

### No New Packages Needed
This phase requires zero new NuGet packages. All tooling is already present.

**No installation command** — use existing packages.

---

## Architecture Patterns

### Recommended Project Structure Changes
```
EventCenter.Web/
├── Domain/
│   ├── Entities/
│   │   └── Event.cs                    # ADD: EventType, ExternalRegistrationUrl
│   └── Enums/
│       └── EventType.cs                # NEW: enum InPerson, Webinar
├── Data/
│   ├── Configurations/
│   │   └── EventConfiguration.cs       # MODIFY: add EventType, ExternalRegistrationUrl config
│   └── Migrations/
│       └── XXXXXX_AddPhase08WebinarFields.cs  # NEW: migration
├── Validators/
│   └── EventValidator.cs               # MODIFY: conditional rules for webinar
├── Services/
│   └── EventService.cs                 # MODIFY: PublishEventAsync guard + type filter params
└── Components/Pages/
    ├── Admin/Events/
    │   ├── EventForm.razor             # MODIFY: type selector + conditional sections
    │   └── EventList.razor             # MODIFY: add type tab filter
    └── Portal/Events/
        ├── EventList.razor             # MODIFY: add type tab filter
        ├── EventCard.razor             # MODIFY: show webinar badge
        ├── EventDetail.razor           # MODIFY: webinar callout + CTA button
        └── EventRegistration.razor     # MODIFY: redirect guard for webinars
```

### Pattern 1: Discriminator Column (Single-Table Inheritance Light)
**What:** Add `EventType` enum (stored as string) and `ExternalRegistrationUrl` to the existing `Event` entity. No table splitting, no inheritance hierarchy in C#. Just nullable fields + a type discriminator.
**When to use:** When adding a second type to an existing entity that shares >90% of fields. EF Core table-per-hierarchy is overkill for two types sharing most columns.
**Example:**
```csharp
// Domain/Enums/EventType.cs
namespace EventCenter.Web.Domain.Enums;

public enum EventType
{
    InPerson,
    Webinar
}

// Domain/Entities/Event.cs — add two properties
public EventType EventType { get; set; } = EventType.InPerson;  // default: InPerson
public string? ExternalRegistrationUrl { get; set; }
```

### Pattern 2: Conditional Form Sections in Blazor
**What:** A `bool isWebinar` computed property drives `@if` blocks that show/hide form sections.
**When to use:** Switching form behavior based on a type selector.
**Example:**
```razor
<!-- Type selector at top of EventForm.razor -->
<div class="mb-3">
    <label class="form-label">Veranstaltungstyp *</label>
    <InputSelect @bind-Value="Model.EventType" class="form-select">
        <option value="@EventType.InPerson">Präsenzveranstaltung</option>
        <option value="@EventType.Webinar">Webinar</option>
    </InputSelect>
</div>

@if (Model.EventType == EventType.Webinar)
{
    <!-- ExternalRegistrationUrl field -->
    <div class="mb-3">
        <label for="extUrl" class="form-label">Externe Anmelde-URL *</label>
        <InputText id="extUrl" class="form-control" @bind-Value="Model.ExternalRegistrationUrl" />
        <ValidationMessage For="@(() => Model.ExternalRegistrationUrl)" />
    </div>
}

@if (Model.EventType == EventType.InPerson)
{
    <!-- Dates section, Capacity, Agenda, Extra Options — unchanged -->
}
```

### Pattern 3: Client-Side Tab Filter (Portal Event List)
**What:** A `string typeFilter` variable ("All", "InPerson", "Webinar") applied as a LINQ Where on the already-loaded `allEvents` list.
**When to use:** Portal event list already loads all events in-memory (no pagination). Adding a type tab adds zero DB queries.
**Example:**
```razor
<!-- Tab bar -->
<ul class="nav nav-tabs mb-3">
    <li class="nav-item">
        <button class="nav-link @(typeFilter == "All" ? "active" : "")"
                @onclick="@(() => SetTypeFilter("All"))">Alle</button>
    </li>
    <li class="nav-item">
        <button class="nav-link @(typeFilter == "InPerson" ? "active" : "")"
                @onclick="@(() => SetTypeFilter("InPerson"))">Präsenzveranstaltung</button>
    </li>
    <li class="nav-item">
        <button class="nav-link @(typeFilter == "Webinar" ? "active" : "")"
                @onclick="@(() => SetTypeFilter("Webinar"))">Webinar</button>
    </li>
</ul>

@code {
    private string typeFilter = "All";

    private void SetTypeFilter(string filter)
    {
        typeFilter = filter;
        // No DB call needed — filter applied in GetActiveEvents() / GetPastOrFullEvents()
    }

    private List<Event> ApplyTypeFilter(List<Event> events)
        => typeFilter switch
        {
            "InPerson" => events.Where(e => e.EventType == EventType.InPerson).ToList(),
            "Webinar"  => events.Where(e => e.EventType == EventType.Webinar).ToList(),
            _          => events
        };
}
```

### Pattern 4: Server-Side Type Filter (Admin Event List)
**What:** The admin list uses paginated server queries (`GetEventsAsync` + `GetEventCountAsync`). Add an optional `EventType? typeFilter` parameter to both methods.
**When to use:** Pagination means we cannot filter client-side like the portal.
**Example:**
```csharp
// EventService.cs
public async Task<List<Event>> GetEventsAsync(
    bool includePast,
    string? sortColumn,
    bool ascending,
    int page,
    int pageSize,
    EventType? typeFilter = null)   // NEW parameter
{
    var query = _context.Events
        .Include(e => e.Registrations)
        .AsQueryable();

    if (typeFilter.HasValue)
        query = query.Where(e => e.EventType == typeFilter.Value);

    // ... rest unchanged
}
```

### Pattern 5: Webinar Publish Guard in EventService
**What:** `PublishEventAsync` checks that webinar events have a non-empty `ExternalRegistrationUrl` before publishing.
**Example:**
```csharp
public async Task<(bool Success, string? ErrorMessage)> PublishEventAsync(int eventId)
{
    var evt = await _context.Events.FindAsync(eventId);
    if (evt == null) return (false, "Veranstaltung nicht gefunden.");

    if (evt.EventType == EventType.Webinar &&
        string.IsNullOrWhiteSpace(evt.ExternalRegistrationUrl))
    {
        return (false, "Webinar kann nicht veröffentlicht werden ohne externe Anmelde-URL.");
    }

    evt.IsPublished = true;
    await _context.SaveChangesAsync();
    return (true, null);
}
```
Note: `PublishEventAsync` currently returns `bool`, not a tuple. It needs to change signature to `Task<(bool, string?)>` like `UnpublishEventAsync`. Update the admin list call site accordingly.

### Pattern 6: EventRegistration.razor Redirect Guard
**What:** On `OnInitializedAsync`, after loading the event, check if `evt.EventType == EventType.Webinar` and redirect to the detail page.
**Example:**
```csharp
// In EventRegistration.razor @code block, after loading evt:
if (evt.EventType == EventType.Webinar)
{
    NavigationManager.NavigateTo($"/portal/events/{EventId}");
    return;
}
```

### Pattern 7: Webinar Badge Component Reuse
**What:** The existing `EventStatusBadge.razor` pattern — a simple shared badge. The webinar badge is not a separate component; it is an inline `@if` block in each list and card. This matches how existing status badges are done in `EventCard.razor` (inline logic, not a shared component).
**Example:**
```razor
<!-- In EventCard.razor, after existing status badge -->
@if (Event.EventType == EventType.Webinar)
{
    <span class="badge bg-info ms-1">
        <i class="bi bi-camera-video"></i> Webinar
    </span>
}
```

### Anti-Patterns to Avoid
- **Creating a new `Webinar` entity class separate from `Event`:** Introduces a second aggregate for what is 95% the same data. Single table with discriminator is the right fit here.
- **Adding EventType as a navigation property / separate table:** Over-engineering. A string-stored enum column is sufficient.
- **New `WebinarService`:** All webinar CRUD is handled by the existing `EventService`. Do not create a parallel service.
- **Client-side tab filter on admin list (paginated):** The admin list is paginated server-side. The type filter must be passed as a query parameter to `GetEventsAsync`.
- **Changing `PublishEventAsync` return type without updating call sites:** The admin list calls this method and checks `bool`. If signature changes to tuple, update `EventList.razor` (admin) to handle the new return type.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| URL validation for ExternalRegistrationUrl | Custom regex | FluentValidation `.Must(Uri.IsWellFormedUriString)` or `.Url()` | Handles edge cases, integrates with existing form validation |
| Tab state management | Custom JS | Blazor `@onclick` state variable | No JS needed — Bootstrap nav-tabs with `active` class driven by C# state |
| Conditional form field hiding | CSS `display:none` in JS | Blazor `@if` blocks | Server-rendered, no JS interop required |
| EventType persistence | Raw string column | EF Core string enum conversion in `EventConfiguration` | Type-safe, readable in DB |

**Key insight:** This phase is 100% additive modification of existing patterns. Every pattern needed already exists in the codebase.

---

## Common Pitfalls

### Pitfall 1: PublishEventAsync Signature Mismatch
**What goes wrong:** `PublishEventAsync` currently returns `bool`. Adding a validation check requires returning an error message, so the signature must change to `Task<(bool Success, string? ErrorMessage)>`. If the admin `EventList.razor` is not updated to match, it will fail to compile.
**Why it happens:** The guard logic requires returning an error string, which the current `bool` return cannot carry.
**How to avoid:** Change `PublishEventAsync` signature AND update the call site in `EventList.razor` (`PublishEvent` method) in the same task.
**Warning signs:** Compile error in `EventList.razor` at the `PublishEvent` method after changing the service.

### Pitfall 2: Default EventType for Existing Events
**What goes wrong:** The EF Core migration adds `EventType` column. Existing rows will have `NULL` or empty string unless a default is set. The `GetCurrentState()` extension and all queries that check `EventType == EventType.InPerson` will behave unexpectedly on legacy rows.
**Why it happens:** SQL Server does not auto-populate existing rows with C# default values unless the migration specifies `defaultValue`.
**How to avoid:** In the migration `Up()` method, use `defaultValue: "InPerson"` when adding the column:
```csharp
migrationBuilder.AddColumn<string>(
    name: "EventType",
    table: "Events",
    type: "nvarchar(50)",
    maxLength: 50,
    nullable: false,
    defaultValue: "InPerson");
```
**Warning signs:** Existing events failing to load or showing wrong type filter behavior after migration.

### Pitfall 3: EventValidator Requiring Deadline/Capacity for Webinars
**What goes wrong:** The current `EventValidator` unconditionally requires `RegistrationDeadlineUtc <= StartDateUtc` and `MaxCapacity > 0`. Webinar events should not require these.
**Why it happens:** The validator has no concept of event type.
**How to avoid:** Add `.When(e => e.EventType == EventType.InPerson)` conditions on the deadline, capacity, and max-companions rules.
**Warning signs:** Validation errors on the webinar form when leaving deadline/capacity at defaults.

### Pitfall 4: EventForm Defaults for Webinar Mode
**What goes wrong:** When type selector switches to Webinar, the CET date fields and capacity fields still hold values from default initialization. If the form is submitted as a webinar, those values persist to the DB even though they are not displayed.
**Why it happens:** The fields are hidden but not zeroed out.
**How to avoid:** On type switch (EventType.Webinar), set `Model.MaxCapacity = 0`, `Model.MaxCompanions = 0`. The `RegistrationDeadlineUtc` can stay as-is (it will be ignored for webinars in the validator) or set to `DateTime.MinValue`. The key is the fields are nullable or defaultable in the entity.
**Warning signs:** Webinar events appearing in "full" state (`MaxCapacity == 0, registrations == 0` triggers `GetCurrentRegistrationCount() >= MaxCapacity`).

**Critical:** `GetCurrentRegistrationCount()` returning >= `MaxCapacity` when both are 0 would trigger "Ausgebucht" in `EventCard.razor`. For webinars, the `IsEventActive()` check in the portal list and the `isFull` check in `EventDetail.razor` must be skipped. Either set `MaxCapacity` to a large sentinel value for webinars (e.g., `int.MaxValue`) or add `EventType == Webinar` short-circuit to capacity checks.

### Pitfall 5: EventCard.razor Capacity Badge on Webinars
**What goes wrong:** `EventCard.razor` shows `GetMaklerCost()` (minimum agenda item cost). Webinars have no agenda items, so this returns 0 and shows nothing — acceptable. But `GetStatusBadge()` checks `registrationCount >= Event.MaxCapacity`. For webinars with `MaxCapacity = 0` (or similar), this would show "Ausgebucht".
**Why it happens:** No guard for webinar type in the card status logic.
**How to avoid:** In `GetStatusBadge()` (and `IsEventActive()` in portal list), add a check: if `Event.EventType == EventType.Webinar`, skip capacity/deadline checks and return a "Webinar" status or "Plätze frei" equivalently.
**Warning signs:** Webinar cards showing "Ausgebucht" immediately after publish.

### Pitfall 6: iCal Export on Webinar Events
**What goes wrong:** The webinar detail page keeps the iCal export button per the locked decision. The existing iCal endpoint at `/api/events/@EventId/calendar` uses event `Location` for the calendar entry. Webinar events may have a placeholder location or none.
**Why it happens:** The iCal endpoint is unchanged, but webinar location may not be a physical address.
**How to avoid:** No change needed to the iCal endpoint. The Location field is still required for webinars (it can hold "Online" or similar). This is acceptable behavior — keep the field visible in the form for webinars.
**Warning signs:** Not applicable — no code change needed, just document that Location is still shown for webinars.

### Pitfall 7: DuplicateEventAsync for Webinars
**What goes wrong:** `DuplicateEventAsync` in `EventService` copies all fields. If a webinar is duplicated, the copy must also preserve `EventType` and `ExternalRegistrationUrl`.
**Why it happens:** The duplicate method was written before webinar support and does not copy the new fields.
**How to avoid:** Add `EventType = source.EventType` and `ExternalRegistrationUrl = source.ExternalRegistrationUrl` to the duplicate object initializer.
**Warning signs:** Duplicated webinar appearing as an in-person event.

---

## Code Examples

Verified patterns from the existing codebase:

### EventType Enum (new file)
```csharp
// Source: Project pattern — mirrors existing EventState.cs
// Domain/Enums/EventType.cs
namespace EventCenter.Web.Domain.Enums;

public enum EventType
{
    InPerson,
    Webinar
}
```

### Event Entity Extension
```csharp
// Source: Existing Event.cs pattern
public EventType EventType { get; set; } = EventType.InPerson;
public string? ExternalRegistrationUrl { get; set; }
```

### EventConfiguration Extension
```csharp
// Source: Existing EventConfiguration.cs pattern
builder.Property(e => e.EventType)
    .IsRequired()
    .HasMaxLength(50)
    .HasConversion<string>();  // stores "InPerson" / "Webinar" as string

builder.Property(e => e.ExternalRegistrationUrl)
    .HasMaxLength(2000);
```

### EF Core Migration (naming convention)
```csharp
// Source: Existing migration naming pattern (AddPhaseXX...)
// File: XXXXXX_AddPhase08WebinarFields.cs
migrationBuilder.AddColumn<string>(
    name: "EventType",
    table: "Events",
    type: "nvarchar(50)",
    maxLength: 50,
    nullable: false,
    defaultValue: "InPerson");

migrationBuilder.AddColumn<string>(
    name: "ExternalRegistrationUrl",
    table: "Events",
    type: "nvarchar(2000)",
    maxLength: 2000,
    nullable: true);
```

### FluentValidation Conditional Rules
```csharp
// Source: Existing EventValidator.cs pattern + FluentValidation .When() API
RuleFor(e => e.MaxCapacity)
    .GreaterThan(0).WithMessage("Maximale Kapazität muss größer als 0 sein")
    .When(e => e.EventType == EventType.InPerson);

RuleFor(e => e.RegistrationDeadlineUtc)
    .LessThanOrEqualTo(e => e.StartDateUtc)
    .WithMessage("Anmeldefrist muss vor Veranstaltungsbeginn liegen")
    .When(e => e.EventType == EventType.InPerson);

RuleFor(e => e.ExternalRegistrationUrl)
    .NotEmpty().WithMessage("Externe Anmelde-URL ist erforderlich für Webinare")
    .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
    .WithMessage("Bitte eine gültige URL eingeben (z.B. https://...)")
    .When(e => e.EventType == EventType.Webinar && /* only on publish check */ false);
    // NOTE: URL required-for-publish is a SERVICE-LEVEL check (PublishEventAsync guard),
    // not a form-level validation. The form saves drafts without URL.
```

### Webinar Detail Page CTA Button
```razor
<!-- Source: Existing EventDetail.razor sidebar pattern -->
@if (evt.EventType == EventType.Webinar)
{
    <!-- Webinar banner/callout at top of main content -->
    <div class="alert alert-info d-flex align-items-center mb-4">
        <i class="bi bi-camera-video me-2 fs-4"></i>
        <div>
            <strong>Dieses Event findet als Webinar statt.</strong>
        </div>
    </div>

    <!-- In sidebar: CTA button replaces registration form -->
    <a href="@evt.ExternalRegistrationUrl"
       target="_blank"
       rel="noopener noreferrer"
       class="btn btn-primary btn-lg w-100 mb-2">
        <i class="bi bi-box-arrow-up-right"></i> Zur Webinar-Anmeldung
    </a>
}
```

### Admin Event List — Type Column + Tab Filter
```razor
<!-- Source: Existing EventList.razor (admin) table row pattern -->
<td>
    @if (evt.EventType == EventType.Webinar)
    {
        <span class="badge bg-info">
            <i class="bi bi-camera-video"></i> Webinar
        </span>
    }
    else
    {
        <span class="badge bg-secondary">Präsenz</span>
    }
</td>
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single event type (InPerson only) | Two event types with discriminator column | Phase 8 | All event-related components need `EventType` guards |
| `PublishEventAsync` returns `bool` | Must return `(bool, string?)` tuple | Phase 8 | Admin EventList.razor call site must be updated |

**No deprecated patterns introduced.** This phase follows existing EF Core, FluentValidation, and Blazor patterns exactly.

---

## Open Questions

1. **What happens to EventCard for webinars with MaxCapacity = 0?**
   - What we know: `GetCurrentRegistrationCount() >= MaxCapacity` triggers "Ausgebucht" when both are 0
   - What's unclear: Should we set `MaxCapacity = int.MaxValue` for webinars, or add `EventType` guard to capacity logic?
   - Recommendation: Add `EventType == Webinar` guard in `IsEventActive()` and `GetStatusBadge()`. Webinars are never "full" or "past deadline" in the broker-facing capacity sense. Set `MaxCapacity = 0` in the DB (do not set sentinel value) and fix the display logic to handle webinars separately.

2. **Should Location be required for webinars?**
   - What we know: `EventValidator` requires Location. iCal export uses Location. The CONTEXT.md says "Claude's discretion" on which fields beyond deadline/capacity are hidden.
   - What's unclear: Should admins enter a location for webinars (e.g., "Online")?
   - Recommendation: Keep Location required for webinars (the iCal export needs it). Do not hide the Location field for webinars. This avoids a validator rule change for Location.

3. **Should AgendaItems section be entirely hidden in the form for webinars, or just not shown?**
   - What we know: CONTEXT.md says "No agenda section for webinars — hidden entirely."
   - What's unclear: When switching back from Webinar to InPerson, should previously-added agenda items re-appear?
   - Recommendation: When type switches to Webinar, clear `Model.AgendaItems` and `agendaItemDatesCet`. When switching back to InPerson, the form starts fresh (no items). This is simplest and avoids stale state.

---

## Sources

### Primary (HIGH confidence)
- Direct codebase inspection — `Event.cs`, `EventForm.razor`, `EventList.razor` (admin + portal), `EventDetail.razor`, `EventCard.razor`, `EventService.cs`, `EventValidator.cs`, `EventConfiguration.cs`, migrations — all read directly
- `EventCenterDbContext.cs` — confirmed entity registration pattern
- `.planning/phases/08-webinar-support/08-CONTEXT.md` — locked user decisions

### Secondary (MEDIUM confidence)
- EF Core string enum conversion (`HasConversion<string>()`) — standard EF Core pattern, verified against codebase use of `InvitationStatus` stored as string (STATE.md decision log)
- FluentValidation `.When()` conditional rules — standard API, verified against existing `EventValidator.cs` which uses `When(e => e.Id == 0)` pattern

### Tertiary (LOW confidence)
- None — all findings verified directly against the codebase

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — zero new packages, all existing tooling
- Architecture: HIGH — all patterns derived directly from existing code
- Pitfalls: HIGH — derived from direct code analysis of existing logic (capacity check, validator, migrate default)

**Research date:** 2026-02-27
**Valid until:** 2026-03-29 (30 days — stable project, no external dependencies added)
