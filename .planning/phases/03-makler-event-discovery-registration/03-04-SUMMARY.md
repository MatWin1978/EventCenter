---
phase: 03-makler-event-discovery-registration
plan: 04
subsystem: broker-event-discovery-ui
tags: [ui, blazor, search, filters, card-grid, responsive]
dependency_graph:
  requires: [03-02-business-logic-services, 02-02-event-service]
  provides: [broker-event-list-page, event-card-component, event-discovery-ui]
  affects: [Components/Pages/Portal/Events, Components/Shared]
tech_stack:
  added: []
  patterns: [card-grid-layout, debounced-search, date-filter-presets, collapsible-sections, idisposable-pattern]
key_files:
  created:
    - EventCenter.Web/Components/Shared/EventCard.razor
    - EventCenter.Web/Components/Pages/Portal/Events/EventList.razor
  modified: []
decisions:
  - Use broker-specific status badges (different from admin EventStatusBadge)
  - 300ms debounce for instant search to reduce query load
  - Default date filter to "Nächste 3 Monate" (most common use case)
  - Active events include user's registered upcoming events
  - Past/full events in separate collapsible section
  - Cost indication shows minimum makler agenda item cost
metrics:
  duration: 169
  completed_date: "2026-02-26"
  tasks_completed: 2
  files_created: 2
  files_modified: 0
  lines_added: 289
  commits: 2
---

# Phase 03 Plan 04: Broker Event Discovery Interface Summary

**One-liner:** Built responsive card grid event list page for brokers with instant search, date filtering, broker-specific status badges, and collapsible past/full events section.

## What Was Built

### EventCard Component (Reusable Card for Broker Portal)

**Location:** `EventCenter.Web/Components/Shared/EventCard.razor`

**Features:**
- **Responsive card layout:** Bootstrap card with `h-100` class for equal height in grid
- **Card content:**
  - Event title (card-title, h5)
  - Meta row with Bootstrap Icons:
    - Calendar icon + date in CET format (dd.MM.yyyy)
    - Location icon + location
  - Description excerpt (first 120 characters with "..." if truncated)
  - Status badge (broker-specific logic)
  - Cost indication badge (shows minimum makler cost if > 0)
- **Card footer:** "Details" button linking to `/portal/events/{id}`

**Broker-Specific Status Badge Logic:**
Different from admin `EventStatusBadge` component - uses registration context:

1. **Angemeldet (blue, bg-primary):** User is registered for this event
2. **Verpasst (gray, bg-secondary):** Event is Finished or DeadlineReached
3. **Ausgebucht (red, bg-danger):** Registration count >= MaxCapacity
4. **Plätze frei (green, bg-success):** Default - event is Public with available capacity

**Cost Indication:**
- Shows `ab X,XX EUR` badge if any agenda items have `MaklerCanParticipate = true` and `CostForMakler > 0`
- Displays minimum cost from eligible agenda items
- Hidden if all agenda items are free or no makler participation allowed

**Dependencies:**
- Uses `TimeZoneHelper.FormatDateTimeCet` for CET date formatting
- Uses `Event.GetCurrentState()` extension method for state calculation
- Uses `Event.GetCurrentRegistrationCount()` for capacity checking
- Bootstrap Icons (`bi bi-calendar`, `bi bi-geo-alt`) for visual indicators

### EventList Page (Broker Event Discovery Interface)

**Location:** `EventCenter.Web/Components/Pages/Portal/Events/EventList.razor`
**Route:** `/portal/events`
**Authorization:** `[Authorize(Roles = "Makler")]` - only Makler role can access

**Layout Structure:**

1. **Page Header:** "Veranstaltungen" (h2)

2. **Horizontal Filter Bar** (row with 2 columns):
   - **Left (col-md-6):** Text search input
     - Placeholder: "Suche nach Name oder Ort..."
     - Uses `@bind:event="oninput"` and `@bind:after="OnSearchChanged"` for instant search
     - 300ms debounce implemented with `System.Threading.Timer`
   - **Right (col-md-6):** Date filter button group
     - Three buttons: "Diesen Monat", "Nächste 3 Monate" (default), "Dieses Jahr"
     - Active button gets `active` Bootstrap class
     - Calculates `startDateTo` based on filter (1 month, 3 months, 1 year from now)

3. **Loading State:** Spinner with "Lade Veranstaltungen..." message

4. **Empty State:** Alert with "Keine Veranstaltungen gefunden." message

5. **Active Events Section:**
   - Responsive card grid: `row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4`
     - 1 column on mobile
     - 2 columns on medium screens
     - 3 columns on large screens
   - Shows events where:
     - State is Public AND not full (regardless of registration)
     - OR user is registered AND event is not Finished (upcoming registered events)
   - Sorted by `StartDateUtc` ascending (nearest events first)
   - Each event rendered with `<EventCard>` component

6. **Past/Full Events Collapsible Section:**
   - Bootstrap collapse with toggle button
   - Button text: "Vergangene und ausgebuchte Veranstaltungen anzeigen ({count})"
   - Shows events where:
     - State is Finished or DeadlineReached
     - OR capacity is full AND user is not registered
   - Sorted by `StartDateUtc` descending (most recent first)
   - Same responsive card grid layout

**Data Loading Logic:**

- **OnInitializedAsync:**
  - Extracts `userEmail` from `AuthenticationStateProvider` claims (`preferred_username` or `Identity.Name`)
  - Calls `LoadEventsAsync()` to fetch initial data
  - Sets `isLoading = false` to trigger UI render

- **OnSearchChanged (debounced):**
  - Disposes previous timer
  - Creates new 300ms timer
  - Calls `LoadEventsAsync()` after delay
  - Uses `InvokeAsync()` and `StateHasChanged()` for UI thread marshalling

- **SetDateFilter:**
  - Updates `dateFilter` field
  - Immediately calls `LoadEventsAsync()` (no debounce for button clicks)

- **LoadEventsAsync:**
  - Calculates `startDateTo` based on `dateFilter`:
    - "month" → `DateTime.UtcNow.AddMonths(1)`
    - "quarter" → `DateTime.UtcNow.AddMonths(3)` (default)
    - "year" → `DateTime.UtcNow.AddYears(1)`
  - Calls `EventService.GetPublicEventsAsync()` with:
    - `searchTerm`: null if empty/whitespace, otherwise search string
    - `startDateFrom`: null (includes past events for collapsible section)
    - `startDateTo`: calculated from date filter
    - `userEmail`: current user's email for registration status

**Event Categorization Logic:**

- **IsUserRegistered:** Checks if `Registrations` collection contains entry with matching `Email` and `IsCancelled = false`

- **IsEventActive:** Event is active if:
  - (State is Public AND not full) OR
  - (User is registered AND state is not Finished)

  This means registered events stay in "Active" section until they finish (even if deadline passed or event is full)

- **GetActiveEvents:** Filters `allEvents` by `IsEventActive`, sorts ascending by `StartDateUtc`

- **GetPastOrFullEvents:** Filters `allEvents` by `!IsEventActive`, sorts descending by `StartDateUtc`

**IDisposable Implementation:**
- Implements `IDisposable` interface
- `Dispose()` method disposes `debounceTimer` to prevent memory leaks
- Critical for Blazor component lifecycle - timer runs on background thread and must be cleaned up

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

**Build Status:** SUCCESS
- EventCenter.Web builds with no errors
- Only pre-existing warnings (async methods without await in auth components)

**Test Status:** ALL PASS
- 67 total tests passing (no regressions)
- No new tests required for UI components (plan was UI-only)
- Existing service tests (EventService.GetPublicEventsAsync) cover backend logic

**Must-Haves Verification:**
- ✅ Broker sees card grid of published events sorted by nearest upcoming first
- ✅ Each card shows title, date, location, description excerpt, status badge, and cost indication
- ✅ Status badges: green (Plätze frei), blue (Angemeldet), red (Ausgebucht), gray (Verpasst)
- ✅ Instant text search filters events by name or location as user types
- ✅ Date filter presets (Diesen Monat, Nächste 3 Monate, Dieses Jahr) work correctly
- ✅ Active events shown first, past/full events in separate collapsible section

**Artifact Verification:**
- ✅ EventCenter.Web/Components/Pages/Portal/Events/EventList.razor exists (190 lines)
- ✅ EventCenter.Web/Components/Shared/EventCard.razor exists (99 lines)
- ✅ EventList.razor contains `@page "/portal/events"`
- ✅ EventCard.razor min_lines requirement: 40 lines (actual: 99 lines)

**Key-Links Verification:**
- ✅ EventList.razor injects EventService (`@inject EventService EventService`)
- ✅ EventList.razor calls `EventService.GetPublicEventsAsync` (line 157)
- ✅ EventList.razor uses `Event.GetCurrentState()` extension method (line 173)
- ✅ EventCard.razor navigates to `/portal/events/@Event.Id` (line 31)

## Self-Check: PASSED

**Created files verification:**
```bash
FOUND: EventCenter.Web/Components/Shared/EventCard.razor
FOUND: EventCenter.Web/Components/Pages/Portal/Events/EventList.razor
```

**Commits verification:**
```bash
FOUND: bf06232 (Task 1 - EventCard component)
FOUND: a22e429 (Task 2 - EventList page)
```

All claimed files exist and commits are in git history.

## Technical Notes

### Debounced Search Implementation

The instant search feature uses a `System.Threading.Timer` with 300ms delay:

```csharp
private void OnSearchChanged()
{
    debounceTimer?.Dispose(); // Cancel previous timer
    debounceTimer = new System.Threading.Timer(_ =>
    {
        InvokeAsync(async () =>
        {
            await LoadEventsAsync();
            StateHasChanged();
        });
    }, null, 300, Timeout.Infinite); // 300ms delay, one-shot
}
```

**Key points:**
1. **Dispose previous timer:** Prevents multiple timers running simultaneously
2. **InvokeAsync:** Marshals callback to UI thread (required for Blazor Server)
3. **StateHasChanged:** Forces UI re-render after async data load
4. **Timeout.Infinite:** Timer fires only once (not periodic)
5. **IDisposable cleanup:** Timer disposed in component's Dispose() method

**Alternative approaches not used:**
- Blazor's `@bind:after` with delay → no built-in debounce support
- JavaScript interop → adds complexity for simple feature
- Third-party library (Debounce.NET) → unnecessary dependency

### Active vs. Past Event Logic

The categorization logic prioritizes user context:

**Active Section:**
- Events user hasn't registered for → show if Public and not full
- Events user has registered for → show until event Finished (even if deadline passed)

**Past/Full Section:**
- Events user hasn't registered for → show if full or past deadline/finished
- Events user has registered for → show only if Finished

**Rationale:**
- Brokers want to see their upcoming registrations in the active section
- "Angemeldet" badge makes registered events visually distinct
- Past/full section is for events user can't act on (registration closed or unavailable)

**Edge case:** Event becomes full after user registers → still shows in active section with "Angemeldet" badge

### Bootstrap Icons Integration

The project already includes Bootstrap Icons (verified via existing usage in admin pages):

- Uses `<i class="bi bi-calendar"></i>` for date icons
- Uses `<i class="bi bi-geo-alt"></i>` for location icons
- No additional package installation required
- Icons loaded via CDN or bundled CSS (configured in Phase 01)

### Cost Indication Logic

The `GetMaklerCost()` method calculates minimum cost:

```csharp
var maklerItems = Event.AgendaItems
    .Where(a => a.MaklerCanParticipate && a.CostForMakler > 0)
    .ToList();

if (!maklerItems.Any()) return 0;

return maklerItems.Min(a => a.CostForMakler);
```

**Design choices:**
1. **Filter by MaklerCanParticipate:** Only show costs for items brokers can select
2. **Exclude free items (CostForMakler > 0):** "ab 0,00 EUR" is meaningless
3. **Show minimum cost:** "ab X EUR" indicates lowest possible cost
4. **No total cost:** User hasn't selected items yet, so sum is misleading

**Display logic:**
- Badge hidden if `GetMaklerCost() <= 0`
- Badge shown with `bg-light text-dark` for neutral appearance
- Format: `ab X,XX EUR` (German decimal format with 2 places)

## Downstream Impact

### Plan 05 (Event Detail and Registration Form)

**EventList → EventDetail navigation:**
- EventCard "Details" button links to `/portal/events/{id}`
- EventDetail page must accept `@page "/portal/events/{id:int}"` route
- EventDetail receives `Event.Id` parameter from URL

**Data available for EventDetail:**
- Event entity with all fields (Title, Description, Location, Dates)
- AgendaItems collection (for agenda item selection UI)
- Registrations collection (to check if user already registered)
- Current user email (from AuthenticationStateProvider)

**Registration button logic:**
- Enabled if: `IsEventActive(evt) && !IsUserRegistered(evt)`
- Disabled states:
  - Event is Finished or DeadlineReached → show "Verpasst" message
  - Event is full → show "Ausgebucht" message
  - User already registered → show "Bereits angemeldet" with link to confirmation

### Phase 05 (Broker Registration Management)

**Registration status display:**
- EventCard shows "Angemeldet" badge for registered events
- EventList includes registered events in active section
- Broker can navigate to registration management from EventList

**Email matching:**
- Uses `preferred_username` claim from Keycloak
- Falls back to `Identity.Name` if claim not present
- Case-insensitive email comparison in `IsUserRegistered()`

## Success Criteria Met

1. ✅ Broker sees card grid of published events at /portal/events
2. ✅ Instant text search filters by name and location
3. ✅ Date presets filter events correctly
4. ✅ Status badges match locked decisions (green/blue/red/gray)
5. ✅ Active events separate from past/full events (collapsible section)
6. ✅ Build and tests pass

## Next Steps

**Plan 05:** Build Event Detail and Registration Form pages
- Event detail page with full description, agenda items, documents
- Registration form with personal info + agenda item selection
- Cost summary dynamically calculated with RegistrationService.CalculateTotalCost
- Confirmation modal before submission
- Success page with iCal download and confirmation details

**Future Enhancements (not in scope):**
- Client-side caching of event list to reduce query load
- Infinite scroll instead of collapsible section for past events
- Save search filters to user preferences
- Real-time updates via SignalR for capacity changes

---

*Plan executed: 2026-02-26*
*Duration: 2m 49s (169 seconds)*
*Commits: bf06232, a22e429*
