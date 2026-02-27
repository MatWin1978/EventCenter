---
phase: 06-guest-management
plan: 02
subsystem: frontend
tags: [blazor-ui, guest-registration, inline-form, cost-display, limit-enforcement]
dependencies:
  requires:
    - 06-01-PLAN.md  # Guest registration backend (RegistrationService, GuestRegistrationFormModel)
    - 03-05-PLAN.md  # EventDetail.razor page structure
  provides:
    - Guest registration UI section on EventDetail.razor
    - Inline guest registration form with limit counter
    - Live cost calculation with CostForGuest pricing
    - Guest list display with relationship and costs
  affects:
    - EventCenter.Web/Components/Pages/Portal/Events/EventDetail.razor
    - EventCenter.Web/Services/RegistrationService.cs (bug fix)
    - EventCenter.Tests/Services/RegistrationServiceTests.cs (bug fix)
tech_stack:
  added: []
  patterns:
    - Inline form pattern (collapse/expand on button click)
    - One-guest-at-a-time registration flow
    - HashSet for checkbox state management (manual binding)
    - Conditional section visibility (isUserRegistered && MaxCompanions > 0)
    - Live reactive cost calculation
key_files:
  created: []
  modified:
    - EventCenter.Web/Components/Pages/Portal/Events/EventDetail.razor
    - EventCenter.Web/Services/RegistrationService.cs
    - EventCenter.Tests/Services/RegistrationServiceTests.cs
decisions:
  - Inline form expands/collapses on button click for one-guest-at-a-time flow
  - Manual checkbox state management using HashSet and @onchange handlers (not InputCheckbox with bind)
  - Pre-select mandatory agenda items when form opens
  - Guest section hidden when MaxCompanions=0 or broker not registered
  - Display limit counter badge (X/Y) in card header for constant visibility
  - Total guest cost calculated from CostForGuest field on agenda items
  - Success message shown after registration, form collapses automatically
  - Fire-and-forget email pattern already implemented in backend (no UI changes needed)
metrics:
  duration: 446s (7.4 minutes)
  tasks_completed: 2
  files_created: 0
  files_modified: 3
  tests_added: 0
  completed_date: "2026-02-27"
---

# Phase 06 Plan 02: Guest Management UI Summary

**One-liner:** Complete guest registration UI on EventDetail.razor with inline form, limit counter, live cost calculation, and CostForGuest pricing display.

## Overview

Extended the EventDetail.razor page with a comprehensive guest management section that enables brokers to register companions directly on the event detail page. Implemented inline form UX with one-guest-at-a-time flow, live cost calculation, limit enforcement display, and guest list with relationship details and CostForGuest pricing.

## Tasks Completed

### Task 1: Add guest registration section to EventDetail.razor
**Status:** ✅ Complete
**Commit:** 9ecb3cf

**Implementation:**
- Injected `RegistrationService` and added `@using` directives for `EventCenter.Web.Models` and `Blazored.FluentValidation`
- Added "Begleitpersonen" section in main content area (col-md-8) after Documents section
- Section visibility: only shown when `isUserRegistered && evt.MaxCompanions > 0`
- **Card header** with "Begleitpersonen" title and limit counter badge showing `currentGuestCount / MaxCompanions`
- **Guest list ("Meine Begleitpersonen")**:
  - list-group displaying registered guests with Salutation, FirstName, LastName
  - RelationshipType shown as text-muted small text
  - Agenda item costs calculated from `CostForGuest` field, displayed as badge
  - Total guest cost displayed at bottom: "Gesamtkosten Begleitpersonen: X.XX EUR"
- **Register button or limit warning**:
  - "Begleitperson anmelden" button shown when `currentGuestCount < MaxCompanions && !showGuestForm`
  - Alert-warning shown when `currentGuestCount >= MaxCompanions`: "Maximale Anzahl Begleitpersonen erreicht"
- **Inline guest registration form** (border p-3 rounded) with:
  - Row 1: Anrede (InputSelect: Herr/Frau/Divers), Vorname (InputText), Nachname (InputText)
  - Row 2: E-Mail (InputText type="email"), Beziehungstyp (InputText with placeholder)
  - Agenda items section: filtered by `GuestsCanParticipate == true`, ordered by StartDateTimeUtc
  - Checkboxes with manual state management using `selectedGuestAgendaItemIds` HashSet
  - Mandatory items pre-selected and disabled (same pattern as broker registration)
  - CostForGuest pricing badges: bg-primary for paid items, bg-success "Kostenfrei" for 0
  - Live cost display calculated from selected items
  - Action buttons: "Abbrechen" (calls CancelGuestForm) and "Begleitperson anmelden" submit button
  - Submit button shows spinner when `isSubmittingGuest=true`
- **Success/error messages**:
  - alert-success shown after successful registration: "Begleitperson erfolgreich angemeldet."
  - alert-danger shown on error with message from service
  - Both dismissible with close button
- **State variables added**:
  - `showGuestForm`, `isSubmittingGuest`, `guestSuccessMessage`, `guestErrorMessage`
  - `currentGuestCount`, `totalGuestCost`, `userGuestRegistrations`
  - `guestFormModel`, `selectedGuestAgendaItemIds`, `userBrokerRegistrationId`
- **Methods implemented**:
  - `LoadGuestDataAsync()`: loads guest count, guest registrations, calculates total cost
  - `HandleGuestRegistrationAsync()`: calls `RegisterGuestAsync`, handles success/error, refreshes guest data
  - `OpenGuestForm()`: opens form and pre-selects mandatory agenda items
  - `CancelGuestForm()`: closes form and resets state
  - `ToggleGuestAgendaItem(int agendaItemId)`: toggles checkbox state and syncs to form model
  - `CalculateSelectedGuestCost()`: returns live cost sum from selected agenda items
- **Modified OnInitializedAsync**:
  - Find broker's Registration matching `userEmail` with `RegistrationType.Makler`
  - Store `userBrokerRegistrationId` if found
  - Call `LoadGuestDataAsync()` to initialize guest state

**Files:**
- EventCenter.Web/Components/Pages/Portal/Events/EventDetail.razor

**Verification:** Project builds without errors. All service injections and model references work correctly.

---

### Task 2: Verify full integration and run test suite
**Status:** ✅ Complete
**Commit:** 7b5be41

**Implementation:**
- Built entire solution - both EventCenter.Web and EventCenter.Tests compile successfully
- Ran full test suite - all 131 tests pass
- **Bug fix (Rule 1 - Deviation)**: Fixed EF Core Include chain in `RegisterGuestAsync`:
  - **Issue:** Incorrect Include syntax `.Include(r => r.Event.Registrations)` was causing EF Core to throw exception
  - **Root cause:** Cannot chain Include after ThenInclude without starting new Include path
  - **Fix:** Changed to explicit collection load using `_context.Entry(evt).Collection(e => e.Registrations).LoadAsync()`
  - **Impact:** All guest registration tests now pass (9/9 passing)
- **Bug fix (Rule 1 - Deviation)**: Added missing `MaklerCanParticipate = true` to test agenda item in `RegisterGuestAsync_LimitReached_ReturnsError` test
  - **Issue:** Test broker registration failing because agenda item didn't allow broker participation
  - **Fix:** Added `MaklerCanParticipate = true` to test setup (same pattern as fixed in 06-01)
- Verified EventDetail.razor contains all required elements:
  - "Begleitpersonen" section header (line 185)
  - GuestRegistrationFormModel reference (lines 495, 576, 598, 623)
  - RegistrationService injection (line 11)
  - RegisterGuestAsync call (line 563)
  - GetGuestCountAsync usage (in LoadGuestDataAsync)
  - GetGuestRegistrationsAsync usage (in LoadGuestDataAsync)
  - CostForGuest pricing references (lines 214, 337, 339, 551, 650)
  - MaxCompanions limit check (lines 180, 186, 250, 253)
  - FluentValidationValidator usage (in EditForm)

**Files:**
- EventCenter.Web/Services/RegistrationService.cs (bug fix)
- EventCenter.Tests/Services/RegistrationServiceTests.cs (bug fix)

**Verification:** Solution builds successfully. Full test suite passes (131/131). No regressions introduced.

---

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Incorrect EF Core Include chain in RegisterGuestAsync**
- **Found during:** Task 2 (test execution)
- **Issue:** `RegisterGuestAsync` using incorrect Include syntax `.Include(r => r.Event.Registrations)` after `.ThenInclude(e => e.AgendaItems)`, causing EF Core to throw exception and all guest registration attempts to fail with generic error message
- **Root cause:** EF Core requires new `.Include()` call to start a new navigation path after `.ThenInclude()`. The syntax `.Include(r => r.Event.Registrations)` is invalid after a ThenInclude
- **Fix:** Removed problematic Include and replaced with explicit collection load: `await _context.Entry(evt).Collection(e => e.Registrations).LoadAsync()` after initial query. Also removed duplicate `var evt = brokerRegistration.Event;` declaration
- **Files modified:** EventCenter.Web/Services/RegistrationService.cs
- **Commit:** 7b5be41
- **Impact:** All guest registration tests now pass (9/9). Production code now works correctly for guest registration

**2. [Rule 1 - Bug] Missing MaklerCanParticipate in test agenda item**
- **Found during:** Task 2 (test execution)
- **Issue:** `RegisterGuestAsync_LimitReached_ReturnsError` test failing because agenda item missing `MaklerCanParticipate = true`, preventing broker registration from succeeding
- **Fix:** Added `MaklerCanParticipate = true` to test agenda item setup (same pattern as fixed in plan 06-01)
- **Files modified:** EventCenter.Tests/Services/RegistrationServiceTests.cs
- **Commit:** 7b5be41
- **Impact:** Test now correctly validates limit enforcement after successful broker and first guest registrations

---

## Key Implementation Details

### UI Structure
```razor
@if (isUserRegistered && evt.MaxCompanions > 0)
{
    <div class="card">
        <div class="card-header">
            <h5>Begleitpersonen</h5>
            <span class="badge bg-primary">@currentGuestCount / @evt.MaxCompanions</span>
        </div>
        <div class="card-body">
            <!-- Success/Error Messages -->
            <!-- Guest List (Meine Begleitpersonen) -->
            <!-- Register Button or Limit Warning -->
            <!-- Inline Guest Registration Form -->
        </div>
    </div>
}
```

### Checkbox State Management
Manual binding using HashSet and @onchange (not InputCheckbox with @bind-Value for collections):
```razor
<input type="checkbox" class="form-check-input"
       checked="@(selectedGuestAgendaItemIds.Contains(itemId) || isMandatory)"
       disabled="@isMandatory"
       @onchange="() => ToggleGuestAgendaItem(itemId)" />
```

### Live Cost Calculation
```csharp
private decimal CalculateSelectedGuestCost()
{
    return evt.AgendaItems
        .Where(ai => selectedGuestAgendaItemIds.Contains(ai.Id))
        .Sum(ai => ai.CostForGuest);
}
```

### Guest Registration Flow
1. Broker clicks "Begleitperson anmelden" → `OpenGuestForm()` called
2. Form expands with mandatory items pre-selected
3. Broker fills in guest details and selects optional agenda items
4. Live cost updates as items selected
5. On submit → `HandleGuestRegistrationAsync()` calls `RegisterGuestAsync`
6. On success → success message shown, form collapses, guest data refreshed via `LoadGuestDataAsync()`
7. Guest list updates to show new guest with costs
8. Limit counter updates (e.g., "1/2" → "2/2")
9. If limit reached → button replaced with warning message

---

## Requirements Traceability

**GREG-01:** ✅ UI enables brokers to register guests via inline form on event detail page
**GREG-02:** ✅ UI displays limit counter and disables registration when MaxCompanions reached
**GREG-03:** ✅ UI filters agenda items by GuestsCanParticipate, validates via FluentValidationValidator
**MAIL-02:** ✅ Email already sent by backend (fire-and-forget pattern in RegisterGuestAsync) - no UI changes needed

---

## Next Steps

**Phase 7:** Guest Cancellation & Management
- Allow brokers to cancel/remove guest registrations
- Update guest details (if needed)
- Handle cancellation email notifications
- Adjust cost calculations after guest cancellation

---

## Self-Check

Verifying commits exist:

```bash
git log --oneline --all | grep -q "9ecb3cf" && echo "FOUND: 9ecb3cf" || echo "MISSING: 9ecb3cf"
git log --oneline --all | grep -q "7b5be41" && echo "FOUND: 7b5be41" || echo "MISSING: 7b5be41"
```

Verifying modified files exist:

```bash
[ -f "/home/winkler/dev/EventCenter/EventCenter.Web/Components/Pages/Portal/Events/EventDetail.razor" ] && echo "FOUND: EventDetail.razor" || echo "MISSING: EventDetail.razor"
[ -f "/home/winkler/dev/EventCenter/EventCenter.Web/Services/RegistrationService.cs" ] && echo "FOUND: RegistrationService.cs" || echo "MISSING: RegistrationService.cs"
[ -f "/home/winkler/dev/EventCenter/EventCenter.Tests/Services/RegistrationServiceTests.cs" ] && echo "FOUND: RegistrationServiceTests.cs" || echo "MISSING: RegistrationServiceTests.cs"
```

## Self-Check: PASSED
