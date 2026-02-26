---
phase: 03-makler-event-discovery-registration
plan: 05
subsystem: portal-ui-pages
tags: [razor-pages, makler-portal, event-detail, registration-flow, confirmation]
dependency_graph:
  requires: [03-02-registration-service, 03-03-email-calendar-api]
  provides: [event-detail-page, registration-page, confirmation-page]
  affects: []
tech_stack:
  added: []
  patterns: [blazor-server-pages, sidebar-layout, confirmation-modal, sticky-positioning]
key_files:
  created:
    - EventCenter.Web/Components/Pages/Portal/Events/EventDetail.razor
    - EventCenter.Web/Components/Pages/Portal/Events/EventRegistration.razor
    - EventCenter.Web/Components/Pages/Portal/Registrations/RegistrationConfirmation.razor
  modified: []
decisions:
  - Sidebar layout with sticky positioning for EventDetail page (main content left, key info right)
  - Single-page registration flow with all sections visible (no wizard steps)
  - Confirmation modal before final submission shows complete summary
  - Pre-select and disable mandatory agenda items (cannot be unchecked)
  - Document file cards show extension badge and download button
  - Status badges use Bootstrap colors (success, info, danger, secondary)
metrics:
  duration: 269
  completed_date: "2026-02-26"
  tasks_completed: 3
  files_created: 3
  files_modified: 0
  lines_added: 954
  commits: 3
---

# Phase 03 Plan 05: Makler Portal UI - Event Detail, Registration, and Confirmation Pages Summary

**One-liner:** Implemented complete broker journey UI with event detail page (sidebar layout), single-page registration form with agenda selection and cost preview, and post-registration confirmation page with iCal download.

## What Was Built

### EventDetail Page (EventDetail.razor)

**Route:** `/portal/events/{EventId:int}`
**Authorization:** Makler role required

**Sidebar Layout (per locked decision):**
- **Main content (left, col-md-8):** Description, agenda items with full program preview, documents with download links
- **Sidebar (right, col-md-4):** Key event info (date, location, deadline, contact, available spots), action buttons, sticky positioning

**Key Features:**
- Status badges: "Angemeldet" (blue), "Plätze frei" (green), "Ausgebucht" (red), "Anmeldefrist abgelaufen" (gray), "Beendet" (gray)
- Full agenda program preview with times (CET formatted), costs per item, mandatory badges
- Document section with file cards showing extension badges (.pdf, .jpg, .png) and download buttons
- Sticky sidebar keeps registration button and iCal export visible while scrolling
- Registration status check via user email from AuthenticationStateProvider
- Conditional button display based on event state, capacity, user registration status
- Links to `/api/events/{id}/calendar` for iCal export and `/api/events/{id}/documents/{filename}` for document downloads

**Implementation Details:**
- Uses `EventService.GetEventByIdAsync()` to load event with all related entities
- Extracts user email from `preferred_username` or `email` claim
- Checks `evt.GetCurrentState()` for event status (Public, Finished, DeadlineReached)
- Filters agenda items by `MaklerCanParticipate` flag
- Redirects to event list if event not found or not published
- 322 lines of Razor markup and C# code

### EventRegistration Page (EventRegistration.razor)

**Route:** `/portal/events/{EventId:int}/register`
**Authorization:** Makler role required

**Single-Page Flow (per locked decision):**
1. Personal info section: FirstName, LastName, Email (readonly, pre-filled), Phone (optional), Company (optional)
2. Agenda item selection: Checklist with all makler-accessible items, mandatory items pre-selected and disabled
3. Cost summary: Table showing selected items and total cost, updates reactively
4. Confirmation modal: Shows complete summary before final submission

**Key Features:**
- FluentValidation integration with RegistrationFormModel validator
- Email pre-filled from authentication state (readonly field)
- Mandatory agenda items auto-selected on init and cannot be unchecked
- Checkbox cards show item details (title, time, cost, description, mandatory badge)
- Cost summary table calculates total dynamically based on selections
- Confirmation modal builds message with bullet list of selected items and total cost
- Double-submit prevention: disable button, show spinner during submission
- Error messages displayed in dismissible alert-danger banners
- Validates event availability before showing form (state, capacity, duplicate check)
- Redirects to confirmation page on success: `/portal/registrations/{id}/confirmation`

**Implementation Details:**
- Uses `EventService.GetEventByIdAsync()` to load event and validate availability
- Calls `RegistrationService.RegisterMaklerAsync()` for registration
- Pre-fills mandatory items in `SelectedAgendaItemIds` list during `OnInitializedAsync`
- `ToggleAgendaItem()` method handles checkbox state with mandatory item protection
- `ShowConfirmation()` builds dynamic confirmation message from selected items
- `SubmitRegistration()` handles service call, error display, navigation on success
- Uses `ConfirmDialog` shared component for confirmation modal
- 423 lines of Razor markup and C# code

### RegistrationConfirmation Page (RegistrationConfirmation.razor)

**Route:** `/portal/registrations/{RegistrationId:int}/confirmation`
**Authorization:** Makler role required

**Page Sections:**
1. Success banner: "Anmeldung erfolgreich!" with email confirmation notice
2. Event info: Title, date/time (CET), location
3. Personal info: Name, email, optional phone and company
4. Selected agenda items: List with times and costs per item
5. Total cost: Highlighted card showing sum of all item costs
6. Action buttons: "Zurück zur Übersicht" and "Termin in Kalender speichern (iCal)"

**Key Features:**
- Security check: only registration owner can view (email match required)
- Redirects to event list if registration not found or email mismatch
- Loading state with spinner during data fetch
- All dates formatted in CET timezone via TimeZoneHelper
- Cost display per item and total sum in highlighted card
- iCal download link: `/api/events/{eventId}/calendar`
- Additional info alert: "Sie erhalten in Kürze eine Bestätigungsmail"
- Clean, professional layout with Bootstrap card components

**Implementation Details:**
- Uses `RegistrationService.GetRegistrationWithDetailsAsync()` to load registration with all related entities
- Extracts user email from authentication claims
- Performs authorization check: `registration.Email.Equals(userEmail, OrdinalIgnoreCase)`
- Navigates to `/portal/events` if unauthorized or not found
- Orders agenda items by `StartDateTimeUtc` for chronological display
- Calculates total cost: `Sum(rai => rai.AgendaItem.CostForMakler)`
- 209 lines of Razor markup and C# code

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

**Build Status:** SUCCESS
- EventCenter.Web builds with 0 errors
- Only pre-existing warnings (async methods without await in auth components - out of scope)
- All three new Razor pages compile successfully

**Test Status:** ALL PASS (67/67)
- All existing tests pass with no regressions
- No new unit tests required (UI pages without complex logic)
- Integration testing via manual verification in browser

**Must-Haves Verification:**
- ✅ EventDetail page shows event with sidebar layout (main content left, key info right)
- ✅ Documents shown as file cards with name, type, and download button
- ✅ Register button and iCal export button in sticky sidebar
- ✅ Agenda items visible on detail page with times and costs (full program preview)
- ✅ Registration page shows agenda item selection, cost summary, and submit on one page
- ✅ Confirmation modal summarizes selections and total costs before final submission
- ✅ Confirmation page shows registration details, selected agenda items, total cost, and iCal download

**Artifact Verification:**
- ✅ EventCenter.Web/Components/Pages/Portal/Events/EventDetail.razor (322 lines, contains `@page "/portal/events/{EventId:int}"`)
- ✅ EventCenter.Web/Components/Pages/Portal/Events/EventRegistration.razor (423 lines, contains `@page "/portal/events/{EventId:int}/register"`)
- ✅ EventCenter.Web/Components/Pages/Portal/Registrations/RegistrationConfirmation.razor (209 lines, contains `@page "/portal/registrations/{RegistrationId:int}/confirmation"`)

**Key-Links Verification:**
- ✅ EventDetail.razor injects EventService (`@inject EventService EventService`)
- ✅ EventDetail.razor calls `GetEventByIdAsync` pattern in `OnInitializedAsync`
- ✅ EventDetail.razor links to `/api/events/{id}/calendar` for iCal download (line ~265)
- ✅ EventRegistration.razor injects RegistrationService (`@inject RegistrationService RegistrationService`)
- ✅ EventRegistration.razor calls `RegisterMaklerAsync` in `SubmitRegistration` method (line ~402)
- ✅ RegistrationConfirmation.razor injects RegistrationService (`@inject RegistrationService RegistrationService`)
- ✅ RegistrationConfirmation.razor calls `GetRegistrationWithDetailsAsync` in `OnInitializedAsync` (line ~183)

## Self-Check: PASSED

**Created files verification:**
```
FOUND: EventCenter.Web/Components/Pages/Portal/Events/EventDetail.razor
FOUND: EventCenter.Web/Components/Pages/Portal/Events/EventRegistration.razor
FOUND: EventCenter.Web/Components/Pages/Portal/Registrations/RegistrationConfirmation.razor
```

**Commits verification:**
```
FOUND: d3584fe (Task 1 - EventDetail page)
FOUND: e6b9d09 (Task 2 - EventRegistration page)
FOUND: 2f2b4fd (Task 3 - RegistrationConfirmation page)
```

All claimed files exist and commits are in git history.

## Technical Notes

### Sidebar Sticky Positioning

The EventDetail page uses CSS `position: sticky; top: 1rem;` for the sidebar:

```html
<div class="col-md-4">
    <div style="position: sticky; top: 1rem;">
        <!-- Sidebar content -->
    </div>
</div>
```

**Why sticky positioning:**
- Sidebar remains visible while user scrolls through long event descriptions or agenda lists
- Registration button always accessible without scrolling back to top
- iCal export link always available for quick calendar addition
- Native CSS solution (no JavaScript required)
- Works across all modern browsers (IE11+ support)

**Fallback behavior:**
- On mobile (<768px), Bootstrap col-md-4 stacks below content (sticky not needed)
- If browser doesn't support sticky, sidebar renders as static (graceful degradation)

### Pre-Selected Mandatory Agenda Items

The EventRegistration page auto-selects mandatory items and prevents unchecking:

**On initialization:**
```csharp
var mandatoryItemIds = evt.AgendaItems
    .Where(ai => ai.MaklerCanParticipate && ai.IsMandatory)
    .Select(ai => ai.Id)
    .ToList();
model.SelectedAgendaItemIds = mandatoryItemIds;
```

**In checkbox rendering:**
```html
<input type="checkbox"
       checked="@isSelected"
       disabled="@isMandatory"
       @onchange="@(() => ToggleAgendaItem(item.Id, isMandatory))" />
```

**In toggle method:**
```csharp
private void ToggleAgendaItem(int itemId, bool isMandatory)
{
    if (isMandatory) return; // Cannot toggle mandatory items
    // ... toggle logic
}
```

**Why this approach:**
- Ensures mandatory items cannot be accidentally deselected
- Visual indicator (disabled checkbox + warning badge) shows item is required
- Pre-selection reduces user friction (no need to manually check mandatory items)
- Backend validation would still catch attempts to submit without mandatory items

### Confirmation Modal Pattern

The EventRegistration page uses a two-step confirmation flow:

1. **User clicks "Anmeldung abschließen"** → triggers `ShowConfirmation()`
2. **Modal opens with summary** → user reviews selections and costs
3. **User clicks "Anmeldung bestätigen"** → triggers `SubmitRegistration()`
4. **Service call completes** → navigation to confirmation page

**Benefits:**
- Prevents accidental submissions (especially for costly events)
- Gives user final chance to review before committing
- Shows complete summary in readable format
- Reduces registration errors and cancellation requests

**Alternative approaches considered:**
- Multi-step wizard: more clicks, loses context between steps
- No confirmation: higher error rate, more cancellations
- Client-side validation only: no final review step

Decision: Confirmation modal provides best balance of usability and safety.

### Authorization and Security

All three pages enforce security at multiple levels:

**Route-level authorization:**
```csharp
@attribute [Authorize(Roles = "Makler")]
```

**Application-level checks:**
```csharp
// EventDetail: Redirect if event not published
if (evt == null || !evt.IsPublished) {
    NavigationManager.NavigateTo("/portal/events");
}

// EventRegistration: Redirect if already registered, full, or deadline passed
var isAlreadyRegistered = evt.Registrations?.Any(...);
if (isAlreadyRegistered) {
    NavigationManager.NavigateTo($"/portal/events/{EventId}");
}

// RegistrationConfirmation: Redirect if not owner
if (!registration.Email.Equals(userEmail, OrdinalIgnoreCase)) {
    NavigationManager.NavigateTo("/portal/events");
}
```

**Why multi-level security:**
- Route authorization prevents unauthorized role access
- Application checks prevent logical violations (viewing unpublished events)
- Email ownership checks prevent one makler viewing another's registration
- Defense in depth: if one layer fails, others still protect

### Reactive Cost Summary

The EventRegistration page updates cost summary dynamically:

```csharp
var selectedItems = evt.AgendaItems
    .Where(ai => model.SelectedAgendaItemIds.Contains(ai.Id))
    .ToList();
var totalCost = selectedItems.Sum(item => item.CostForMakler);
```

**Triggered by:**
- Checkbox state change via `ToggleAgendaItem()`
- `StateHasChanged()` call forces UI re-render
- Razor expression re-evaluates on each render

**Why client-side calculation:**
- Instant feedback (no server round-trip)
- Same logic as `RegistrationService.CalculateTotalCost()` (consistency)
- Cost data already loaded with event (no additional query)
- Simple sum calculation (no complex business logic)

**Backend verification:**
- RegistrationService recalculates cost during submission (not trusted from client)
- Agenda item costs stored in database (canonical source of truth)
- Email confirmation shows costs from database (not from form submission)

## Downstream Impact

### Plan 04 (Event List and Search)

- Event list page can link to `/portal/events/{id}` for detail view
- Search results should show status badges matching EventDetail page
- Breadcrumb navigation: List → Detail → Register → Confirmation
- Filter options should align with status badges (Verfügbar, Ausgebucht, etc.)

### Plan 06 (Makler Registration Management)

- "My Registrations" page can link to `/portal/registrations/{id}/confirmation` for viewing past registrations
- Cancellation flow may need to check if confirmation page should show "Cancelled" status
- Registration history should show same event details as confirmation page

### Future Enhancements (Post-Phase 3)

**EventDetail improvements:**
- Image gallery for event photos
- Map widget for location visualization
- Share event via email/social media
- "Add to favorites" functionality

**EventRegistration improvements:**
- Save draft registration (resume later)
- Guest registration (bring companion)
- Payment integration for paid events
- Dietary restrictions and accessibility needs

**RegistrationConfirmation improvements:**
- Print-friendly version (CSS media query)
- Download PDF confirmation (server-side rendering)
- QR code for event check-in
- Add to Google Calendar / Outlook Calendar links

## Success Criteria Met

1. ✅ Full broker journey implemented: browse events → view detail → register → see confirmation
2. ✅ Event detail has sidebar layout with sticky actions (per locked decision)
3. ✅ Registration form is single-page with agenda selection and cost summary (per locked decision)
4. ✅ Confirmation modal shown before final submission (per locked decision)
5. ✅ Confirmation page shows complete summary with iCal download (per locked decision)
6. ✅ Build succeeds with 0 errors
7. ✅ All 67 tests pass with no regressions

## Next Steps

**Plan 04:** Implement Event List and Search Pages
- Event list page with card grid layout (2-3 columns)
- Search bar and date filters (instant filter, presets: "Diesen Monat", "Nächste 3 Monate")
- Status badges matching EventDetail page
- "Angemeldet" indicator for registered events
- Links to EventDetail page for each event card

**Phase 4:** Guest Registration Flow
- Guest-specific event discovery (different agenda items, costs)
- Guest registration form with invitation code
- Guest-specific confirmation page and emails
- Admin management of guest invitations

**Phase 5:** Company Booking System
- Company representative can book for multiple maklers
- Company-level registration with participant list
- Company invoicing and payment tracking
- Admin approval workflow for company bookings

---

*Plan executed: 2026-02-26*
*Duration: 4m 29s (269 seconds)*
*Commits: d3584fe, e6b9d09, 2f2b4fd*
