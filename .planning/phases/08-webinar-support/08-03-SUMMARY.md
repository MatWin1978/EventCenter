---
phase: 08-webinar-support
plan: 03
subsystem: portal-ui
tags: [blazor, webinar, eventtype, portal, eventcard, eventdetail, eventregistration, eventlist]

# Dependency graph
requires:
  - phase: 08-webinar-support
    plan: 01
    provides: EventType enum and ExternalRegistrationUrl on Event entity

provides:
  - Portal EventList type tabs (Alle/Präsenzveranstaltung/Webinar) filtering in-memory event list
  - IsEventActive webinar fix: webinars always active unless Finished (no capacity/deadline constraints)
  - EventCard webinar badge via GetStatusBadge returning bi-camera-video badge for EventType.Webinar
  - EventDetail webinar callout banner (alert-info) before description section
  - EventDetail CTA button 'Zur Webinar-Anmeldung' opening ExternalRegistrationUrl in new tab
  - EventRegistration redirect guard: webinar events redirect to /portal/events/{id}

affects:
  - Portal user experience for webinar events end-to-end

# Tech tracking
tech-stack:
  added: []
  patterns:
    - EventType enum guard at top of conditional render methods (GetStatusBadge, IsEventActive)
    - Bootstrap nav-tabs for client-side LINQ filter (no DB call) — Pattern 3 from 08-RESEARCH.md
    - @if (evt.EventType == EventType.Webinar) { CTA } else { existing form } pattern for sidebar actions

key-files:
  created: []
  modified:
    - EventCenter.Web/Components/Pages/Portal/Events/EventList.razor
    - EventCenter.Web/Components/Shared/EventCard.razor
    - EventCenter.Web/Components/Pages/Portal/Events/EventDetail.razor
    - EventCenter.Web/Components/Pages/Portal/Events/EventRegistration.razor
    - EventCenter.Web/Components/Pages/Admin/Events/EventList.razor
    - EventCenter.Web/Components/Pages/Admin/Events/EventForm.razor

key-decisions:
  - "Webinar type tabs are pure client-side filter (no DB call) — all events already loaded in memory via GetPublicEventsAsync"
  - "GetStatusBadge returns webinar badge early, before capacity/deadline checks — prevents false Ausgebucht for MaxCapacity=0 webinars"
  - "IsEventActive webinar guard checks only GetCurrentState() != Finished — webinars never capacity-blocked"
  - "iCal export button placed outside webinar/else block — visible for all event types"
  - "Admin EventList type filter uses EventType? nullable (null = All) matching GetEventsAsync typeFilter parameter"

requirements-completed: [WBNR-01, WBNR-02]

# Metrics
duration: 8min
completed: 2026-02-27
---

# Phase 8 Plan 03: Portal Webinar UI Summary

**Webinar portal UI: type tabs with in-memory filter, IsEventActive fix, EventCard badge, EventDetail callout + CTA button, EventRegistration redirect guard**

## Performance

- **Duration:** 8 min
- **Started:** 2026-02-27T20:19:07Z
- **Completed:** 2026-02-27T20:27:25Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- Added Alle/Präsenzveranstaltung/Webinar nav-tabs to portal EventList with client-side LINQ filter (no DB call)
- Fixed IsEventActive bug: webinars with MaxCapacity=0 were always considered "full" (0 >= 0); now webinars are always active unless Finished
- Fixed GetStatusBadge bug in EventCard: webinars showed "Ausgebucht" due to MaxCapacity=0; now returns a Webinar badge with bi-camera-video icon
- Added webinar callout banner (alert-info with bi-camera-video) before description on EventDetail page
- Added "Zur Webinar-Anmeldung" CTA button that opens ExternalRegistrationUrl in new tab for webinar event detail sidebar
- In-person registration/cancel UI wrapped in else block — completely unaffected for in-person events
- iCal export button remains outside conditional — visible for both webinar and in-person events
- EventRegistration page redirects webinar events to event detail page instead of showing registration form

## Task Commits

Each task was committed atomically:

1. **Task 1: Portal EventList type tabs + IsEventActive webinar fix** - `9cfcef4` (feat)
2. **Task 2: EventCard badge + EventDetail callout/CTA + EventRegistration redirect guard** - `05bad56` (feat)

**Plan metadata:** _(created in final commit)_

## Files Created/Modified
- `EventCenter.Web/Components/Pages/Portal/Events/EventList.razor` - Added typeFilter state, SetTypeFilter method, ApplyTypeFilter helper, nav-tabs markup, fixed IsEventActive for webinars, GetActiveEvents/GetPastOrFullEvents apply type filter
- `EventCenter.Web/Components/Shared/EventCard.razor` - GetStatusBadge returns Webinar badge (bi-camera-video) for EventType.Webinar, preventing capacity/deadline logic from running
- `EventCenter.Web/Components/Pages/Portal/Events/EventDetail.razor` - Webinar callout alert-info before description; CTA "Zur Webinar-Anmeldung" in sidebar for webinars; in-person actions in else block; iCal export remains for all types
- `EventCenter.Web/Components/Pages/Portal/Events/EventRegistration.razor` - Redirect guard after null/IsPublished check redirects webinar events to /portal/events/{EventId}
- `EventCenter.Web/Components/Pages/Admin/Events/EventList.razor` - Added missing SetTypeFilter method and typeFilter field (fixing pre-existing blocking build error from plan 08-02)
- `EventCenter.Web/Components/Pages/Admin/Events/EventForm.razor` - Added missing OnEventTypeChanged method (fixing pre-existing blocking build error from plan 08-02)

## Decisions Made
- Client-side type filter approach (Pattern 3): all events already in-memory via GetPublicEventsAsync — no additional DB calls needed for tab filtering
- GetStatusBadge webinar guard returns early before any capacity/deadline checks to prevent false "Ausgebucht" badges
- IsEventActive uses GetCurrentState() != Finished as the only guard for webinars (no capacity check)
- iCal export button intentionally placed outside the webinar/else conditional to serve both event types

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added missing OnEventTypeChanged method in Admin EventForm.razor**
- **Found during:** Task 1 (initial build check)
- **Issue:** EventForm.razor markup had `@bind-Value:after="OnEventTypeChanged"` but the method was not defined in @code — build error CS0103
- **Fix:** Added `private void OnEventTypeChanged() { StateHasChanged(); }` to trigger re-render when EventType changes
- **Note:** Linter subsequently added a more complete implementation clearing agenda items/options when switching to Webinar type
- **Files modified:** EventCenter.Web/Components/Pages/Admin/Events/EventForm.razor
- **Committed in:** 9cfcef4 (Task 1 commit)

**2. [Rule 3 - Blocking] Added missing typeFilter field and SetTypeFilter method in Admin EventList.razor**
- **Found during:** Task 2 (test run triggering build check)
- **Issue:** Admin EventList.razor markup referenced `typeFilter` and `SetTypeFilter` (added by linter during plan 08-02) but @code section had no field or method — build errors CS0103
- **Fix:** Added `private EventType? typeFilter = null;` and `private async Task SetTypeFilter(EventType? filter)` with page reset and LoadEvents() call
- **Note:** The linter completed parts of this fix; I completed the remaining `SetTypeFilter` method
- **Files modified:** EventCenter.Web/Components/Pages/Admin/Events/EventList.razor
- **Committed in:** 05bad56 (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (Rule 3 - blocking build errors from incomplete plan 08-02 linter changes)
**Impact on plan:** Both fixes required for build to succeed. Admin EventList and EventForm now fully functional with type filter support.

## Deferred Issues

**Pre-existing test failures (out of scope):**
- `RegistrationServiceTests.RegisterGuestAsync_ValidGuest_ReturnsSuccess` - Failing before and after this plan
- `RegistrationServiceTests.RegisterGuestAsync_Success_SendsEmail` - Failing before and after this plan
- These 2 failures were pre-existing (confirmed: 3 failures before plan start, 2 after — my changes did not introduce them)
- Root cause: unrelated to webinar UI changes, likely in RegistrationService guest registration business logic

## Issues Encountered
None outside of the auto-fixed deviations above.

## User Setup Required
None.

## Next Phase Readiness
- All webinar portal UI features complete (plan 08-01 domain + plan 08-02 admin form + plan 08-03 portal UI)
- Phase 8 Webinar Support is complete
- 2 pre-existing test failures in RegistrationServiceTests remain unresolved (deferred)

## Self-Check

**Result: PASSED**

All files verified present:
- FOUND: EventCenter.Web/Components/Pages/Portal/Events/EventList.razor
- FOUND: EventCenter.Web/Components/Shared/EventCard.razor
- FOUND: EventCenter.Web/Components/Pages/Portal/Events/EventDetail.razor
- FOUND: EventCenter.Web/Components/Pages/Portal/Events/EventRegistration.razor
- FOUND: .planning/phases/08-webinar-support/08-03-SUMMARY.md

All commits verified:
- FOUND: 9cfcef4 (Task 1 - Portal EventList type tabs + IsEventActive fix)
- FOUND: 05bad56 (Task 2 - EventCard badge + EventDetail callout/CTA + EventRegistration redirect)
