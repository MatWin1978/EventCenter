---
phase: 02-admin-event-management
plan: 03
subsystem: admin-ui
tags: [blazor-components, data-table, pagination, modal-dialogs, ui]
completed: 2026-02-26T16:32:19Z
duration: 413

dependency_graph:
  requires:
    - EventService (from 02-02)
    - EventExtensions (from 02-01)
    - EventState enum (from 02-01)
    - TimeZoneHelper (from phase 01)
  provides:
    - EventList admin page at /admin/events
    - EventStatusBadge reusable component
    - ConfirmDialog reusable component
  affects:
    - Admin navigation (already had link to /admin/events)

tech_stack:
  added:
    - EventList.razor (355 lines)
    - EventStatusBadge.razor (20 lines)
    - ConfirmDialog.razor (29 lines)
  patterns:
    - Bootstrap table with sorting
    - Client-side pagination
    - Modal confirmation dialogs
    - EventCallback for component communication
    - Blazor @bind:after for reactive updates

key_files:
  created:
    - EventCenter.Web/Components/Shared/EventStatusBadge.razor
    - EventCenter.Web/Components/Shared/ConfirmDialog.razor
    - EventCenter.Web/Components/Pages/Admin/Events/EventList.razor
  modified: []

decisions:
  - Use page size of 15 items per page for event list pagination
  - Show sort direction indicator (arrow) only on active column
  - Use Bootstrap button groups for action buttons (Edit, Publish, Duplicate, Delete)
  - Variable name changed from 'page' to 'pageNum' in pagination loop to avoid Razor directive conflict

metrics:
  duration_seconds: 413
  completed_date: "2026-02-26"
  tasks_completed: 2
  files_created: 3
  files_modified: 0
---

# Phase 02 Plan 03: Admin Event List UI Summary

**One-liner:** Admin event management page with sortable data table, color-coded status badges, pagination, and action buttons for publish/unpublish/duplicate/delete operations with confirmation dialogs.

## What Was Built

### EventStatusBadge Component (20 lines)

Reusable color-coded badge component for displaying event states:

- **NotPublished** → Gray badge "Entwurf" (bg-secondary)
- **Public** → Green badge "Öffentlich" (bg-success)
- **DeadlineReached** → Orange badge "Frist abgelaufen" (bg-warning text-dark)
- **Finished** → Blue badge "Beendet" (bg-primary)

Usage: `<EventStatusBadge State="@evt.GetCurrentState()" />`

### ConfirmDialog Component (29 lines)

Reusable Bootstrap 5 modal confirmation dialog:

**Parameters:**
- `Show` - controls visibility
- `Title` - modal title
- `Message` - body text
- `ConfirmText` - confirm button label (default "Bestätigen")
- `ConfirmButtonCss` - CSS class for confirm button (default "btn-primary")
- `OnConfirm` - callback when confirmed
- `OnCancel` - callback when cancelled

**Implementation:**
- Pure CSS show/hide (no JavaScript interop needed)
- Bootstrap `.modal` with conditional `show d-block` classes
- Modal backdrop overlay when shown
- "Abbrechen" button for cancel, customizable confirm button

### EventList Page (355 lines)

Comprehensive admin interface for event management at `/admin/events`:

**Header Section:**
- Page title "Veranstaltungen"
- "Neue Veranstaltung" button (navigates to `/admin/events/create`)
- Toggle checkbox: "Vergangene anzeigen" to show past/finished events

**Data Table:**
- **Columns:**
  1. **Titel** - sortable event title
  2. **Datum** - StartDateUtc formatted as CET (dd.MM.yyyy HH:mm)
  3. **Ort** - sortable location
  4. **Status** - EventStatusBadge component
  5. **Anmeldungen** - "{current}/{max}" registration count
  6. **Veröffentlicht** - Bootstrap check icon if published
  7. **Aktionen** - button group with Edit/Publish/Duplicate/Delete

- **Sorting:**
  - Clickable column headers for Title, StartDateUtc, Location
  - Arrow indicator (bi-arrow-up/down) on active column
  - Toggles ascending/descending on repeated clicks
  - Default: StartDateUtc descending (newest first)

- **Filtering:**
  - Default: shows only upcoming/active events (EndDateUtc >= now)
  - Toggle "Vergangene anzeigen" includes past events
  - Resets to page 1 when toggling

**Action Buttons (per row):**
1. **Bearbeiten** - Navigate to `/admin/events/edit/{id}`
2. **Veröffentlichen** (if not published) - Shows confirmation dialog, calls EventService.PublishEventAsync()
3. **Zurückziehen** (if published) - Shows confirmation dialog, calls EventService.UnpublishEventAsync(), handles registration error
4. **Duplizieren** - Calls EventService.DuplicateEventAsync(), navigates to edit page for duplicate
5. **Löschen** - Shows confirmation dialog, calls EventService.DeleteEventAsync(), handles registration error

**Pagination:**
- Page size: 15 events per page
- "Zurück" / page numbers / "Weiter" buttons
- Disabled state for first/last page boundaries
- Shows "Zeige {from}-{to} von {total} Veranstaltungen"

**Loading & Error States:**
- Loading spinner with "Lade Veranstaltungen..." message
- Empty state: "Keine Veranstaltungen gefunden."
- Error alert (dismissible) for service operation failures

**Error Handling:**
- Unpublish blocked by registrations: displays German error message from service
- Delete blocked by registrations: displays "Veranstaltung kann nicht gelöscht werden, da bereits Anmeldungen existieren."
- Duplicate errors: displays exception message

## Tasks Completed

### Task 1: Create EventStatusBadge and ConfirmDialog shared components
**Status:** ✅ Complete
**Commit:** 415a922

**Changes:**
- Created Components/Shared/ directory
- Implemented EventStatusBadge.razor with color-coded badges for all 4 EventState values
- Implemented ConfirmDialog.razor as reusable Bootstrap modal with customizable title, message, and button text
- Both components compile without errors

**Verification:**
- Build succeeded with 0 errors
- EventStatusBadge correctly maps all 4 states to German labels and Bootstrap colors
- ConfirmDialog shows/hides modal with proper Bootstrap classes

### Task 2: Create EventList admin page with data table, sorting, pagination, and actions
**Status:** ✅ Complete
**Commit:** b0859d3

**Changes:**
- Created EventList.razor at Components/Pages/Admin/Events/
- Route: @page "/admin/events" with [Authorize(Roles = "Admin")]
- Injected EventService and NavigationManager
- Implemented data table with all 7 columns
- Added sortable headers with arrow indicators
- Implemented pagination with 15 items per page
- Created action button group with Edit/Publish/Unpublish/Duplicate/Delete
- Integrated ConfirmDialog for publish/unpublish/delete operations
- Added loading state, empty state, and error handling
- All German labels per v1 scope

**Verification:**
- Build succeeded with 0 errors
- Full test suite passed (45/45 tests)
- EventList.razor contains route, authorization, service injection
- Status badges display correct colors per EventState
- All 4 EventState values referenced in EventStatusBadge

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Razor directive conflict with pagination variable**
- **Found during:** Task 2 - build after creating EventList.razor
- **Issue:** Variable named `page` in pagination loop caused Razor parser error "The 'page` directive must appear at the start of the line" because `@page` is interpreted as a directive
- **Fix:** Renamed variable from `page` to `pageNum` in pagination loop
- **Files modified:** EventCenter.Web/Components/Pages/Admin/Events/EventList.razor
- **Commit:** b0859d3
- **Rationale:** Fixes build error - variable name conflicted with Razor @page directive

**2. [Rule 2 - Missing Critical] Missing @using directive for Shared components**
- **Found during:** Task 2 - build showed warning about ConfirmDialog not found
- **Issue:** EventList.razor referenced ConfirmDialog but didn't have @using directive for EventCenter.Web.Components.Shared namespace
- **Fix:** Added `@using EventCenter.Web.Components.Shared` to EventList.razor
- **Files modified:** EventCenter.Web/Components/Pages/Admin/Events/EventList.razor
- **Commit:** b0859d3
- **Rationale:** Component references require explicit namespace imports in Blazor

None of these required user permission - both were build blockers (Rule 3) and missing critical functionality (Rule 2).

## Verification Results

✅ All success criteria met:
- EventList page accessible at /admin/events with Admin role requirement
- Data table displays Title, Date (CET), Location, Status badge, Registration count, Published indicator
- Default view shows upcoming events only; toggle shows past
- Column headers clickable for sorting with direction indicator
- Pagination with 15 items per page
- Status badges: gray (Entwurf), green (Öffentlich), orange (Frist abgelaufen), blue (Beendet)
- Publish/unpublish with confirmation dialog and error handling
- Duplicate action creates copy and navigates to edit
- Delete with confirmation and registration protection error display
- All labels in German

**Build verification:**
```bash
dotnet build EventCenter.Web/EventCenter.Web.csproj --no-restore
# Build succeeded (3 warnings, 0 errors)
```

**Test verification:**
```bash
dotnet test EventCenter.Tests --no-restore
# Passed: 45/45
```

**File verification:**
- ✅ EventCenter.Web/Components/Pages/Admin/Events/EventList.razor exists
- ✅ EventCenter.Web/Components/Shared/EventStatusBadge.razor exists
- ✅ EventCenter.Web/Components/Shared/ConfirmDialog.razor exists

**Route verification:**
```bash
grep '@page "/admin/events"' EventCenter.Web/Components/Pages/Admin/Events/EventList.razor
# Line 1: @page "/admin/events"
```

**Status badge verification:**
```bash
grep "NotPublished\|Public\|DeadlineReached\|Finished" EventCenter.Web/Components/Shared/EventStatusBadge.razor
# All 4 states present with correct German labels and colors
```

**EventService integration:**
```bash
grep "EventService" EventCenter.Web/Components/Pages/Admin/Events/EventList.razor
# Properly injected and used for GetEventsAsync, GetEventCountAsync, PublishEventAsync, UnpublishEventAsync, DeleteEventAsync, DuplicateEventAsync
```

## Key Implementation Details

**Sorting Logic:**
- Default sort: StartDateUtc descending (newest events first)
- Click on active column toggles ascending/descending
- Click on different column switches sort and starts ascending
- Arrow indicator (bi-arrow-up/bi-arrow-down) shows on active column only

**Pagination Calculations:**
- Total pages: `Math.Ceiling(totalCount / pageSize)`
- "Zeige X-Y von Z" display: `(currentPage-1)*pageSize+1` to `Min(currentPage*pageSize, totalCount)`
- First/last page buttons disabled at boundaries

**ConfirmDialog Integration:**
- State variables: `showConfirmDialog`, `confirmTitle`, `confirmMessage`, `confirmButtonText`, `confirmButtonCss`, `confirmAction`
- EventCallback.Factory.Create() for passing async methods to component
- Different button colors per action: green (publish), warning (unpublish), danger (delete)

**Error Handling:**
- UnpublishEventAsync returns `(bool Success, string? ErrorMessage)` tuple
- DeleteEventAsync returns bool, custom error message shown on false
- All error messages in German per v1 requirements

**Date Formatting:**
- Uses TimeZoneHelper.FormatDateTimeCet() for all date displays
- Format: "dd.MM.yyyy HH:mm" (German standard)
- Automatically converts UTC to CET with DST handling

**EventState Display:**
- Uses EventExtensions.GetCurrentState() for real-time state calculation
- EventStatusBadge renders color-coded badge
- No state stored in database - calculated on-the-fly per Phase 01 decision

## Must-Haves Verification

All must-haves from plan frontmatter satisfied:

**Truths:**
- ✅ Admin sees data table with: Title, Date, Location, Status badge, Registration count, Published indicator
- ✅ Default view shows upcoming/active events; toggle shows past/finished
- ✅ Events sortable by clicking column headers
- ✅ Status badges color-coded: Draft=gray, Public=green, DeadlineReached=orange, Finished=blue
- ✅ Admin can publish/unpublish with confirmation dialog
- ✅ Admin can duplicate an event
- ✅ Admin can navigate to create/edit event

**Artifacts:**
- ✅ EventCenter.Web/Components/Pages/Admin/Events/EventList.razor (355 lines, min 80)
- ✅ EventCenter.Web/Components/Shared/EventStatusBadge.razor (20 lines, min 15)
- ✅ EventCenter.Web/Components/Shared/ConfirmDialog.razor (29 lines, min 25)

**Key Links:**
- ✅ EventList → EventService via @inject EventService
- ✅ EventList → EventExtensions.GetCurrentState() for status display
- ✅ EventStatusBadge → EventState enum via switch expression

## Requirements Satisfied

Plan frontmatter listed these requirements:
- **EVNT-03**: Event list page with filtering and sorting
- **EVNT-04**: Publish/unpublish with protection

Both satisfied by EventList implementation.

## Impact on Downstream Plans

**Plan 02-04 (Event Create/Edit Forms):**
- EventList page links to `/admin/events/create` and `/admin/events/edit/{id}` (routes to be created in Plan 04)
- Duplicate action navigates to edit page for the new event
- EventStatusBadge and ConfirmDialog components available for reuse in forms

**Future Admin Features:**
- EventStatusBadge can be reused in any admin view showing event status
- ConfirmDialog can be reused for any destructive action requiring confirmation

## Files Changed Summary

**Created (3 files):**
- EventCenter.Web/Components/Shared/EventStatusBadge.razor (20 lines)
- EventCenter.Web/Components/Shared/ConfirmDialog.razor (29 lines)
- EventCenter.Web/Components/Pages/Admin/Events/EventList.razor (355 lines)

**Total:** 3 files, 404 lines added

## Commits

| Hash    | Type | Description                                          | Files |
|---------|------|------------------------------------------------------|-------|
| 415a922 | feat | Create EventStatusBadge and ConfirmDialog components | 2     |
| b0859d3 | feat | Create EventList admin page with data table          | 1     |

## Self-Check: PASSED

**Created files exist:**
```bash
[ -f "EventCenter.Web/Components/Shared/EventStatusBadge.razor" ] && echo "FOUND"
# FOUND
[ -f "EventCenter.Web/Components/Shared/ConfirmDialog.razor" ] && echo "FOUND"
# FOUND
[ -f "EventCenter.Web/Components/Pages/Admin/Events/EventList.razor" ] && echo "FOUND"
# FOUND
```

**Commits exist:**
```bash
git log --oneline --all | grep -q "415a922" && echo "FOUND: 415a922"
# FOUND: 415a922
git log --oneline --all | grep -q "b0859d3" && echo "FOUND: b0859d3"
# FOUND: b0859d3
```

**Build verification:**
```bash
dotnet build EventCenter.Web/EventCenter.Web.csproj --no-restore
# Build succeeded
```

**Test verification:**
```bash
dotnet test EventCenter.Tests --no-restore
# Passed: 45/45
```

All deliverables verified successfully.
