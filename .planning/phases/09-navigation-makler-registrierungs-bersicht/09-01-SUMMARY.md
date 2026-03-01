---
phase: 09-navigation-makler-registrierungs-bersicht
plan: "01"
subsystem: ui

tags: [blazor, role-based-redirect, navigation, ef-core, registration]

# Dependency graph
requires:
  - phase: 03-makler-registration
    provides: RegistrationService foundation and Registration entity with RegistrationType enum
  - phase: 01-foundation-authentication
    provides: Keycloak OIDC auth with ClaimTypes.Role mapped, AuthenticationStateProvider available in Blazor
provides:
  - Smart role-based redirect at / (Home.razor replaces Hello World placeholder)
  - GetBrokerRegistrationsAsync service method with full eager-load chain for Plan 02
affects: [09-02-PLAN, portal-registrations-overview]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "OnInitializedAsync auth-check pattern: GetAuthenticationStateAsync → role checks → NavigateTo (no forceLoad)"
    - "Null-safe identity check: !user.Identity?.IsAuthenticated ?? true"
    - "EF Core multi-level ThenInclude chain: GuestRegistrations → RegistrationAgendaItems → AgendaItem"

key-files:
  created: []
  modified:
    - EventCenter.Web/Components/Pages/Home.razor
    - EventCenter.Web/Services/RegistrationService.cs

key-decisions:
  - "No [Authorize] on Home.razor: page must remain accessible to unauthenticated users so they can be redirected to /auth/login"
  - "forceLoad: false for all NavigateTo calls: internal Blazor navigation (circuit stays alive)"
  - "showAccessDenied flag pattern: nothing renders during async check preventing content flash"
  - "Cancelled broker registrations included in GetBrokerRegistrationsAsync: UI shows 'Storniert' badge"
  - "No IsCancelled filter on GuestRegistrations in eager-load: all guest states visible inline"

patterns-established:
  - "Home.razor redirect pattern: inject AuthStateProvider + NavigationManager, check in OnInitializedAsync"

requirements-completed: [NAV-01, NAV-02]

# Metrics
duration: 2min
completed: 2026-03-01
---

# Phase 9 Plan 01: Navigation & Role-Based Redirect Summary

**Role-based redirect at / via OnInitializedAsync auth check plus GetBrokerRegistrationsAsync with 5-level EF Core eager-load chain for Plan 02**

## Performance

- **Duration:** 2 min (138 seconds)
- **Started:** 2026-03-01T14:53:55Z
- **Completed:** 2026-03-01T14:56:13Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Home.razor no longer shows "Hello, world!" — it now silently redirects based on authenticated role
- Unauthenticated users land on /auth/login, Admins on /admin/events, Makler on /portal/events
- Authenticated users with no known role see an inline German "Zugriff verweigert" alert (no auto-logout)
- GetBrokerRegistrationsAsync added to RegistrationService with full Include chain covering Event, RegistrationAgendaItems + AgendaItem, SelectedOptions, and GuestRegistrations + their RegistrationAgendaItems + AgendaItem

## Task Commits

Each task was committed atomically:

1. **Task 1: Replace Home.razor with smart role-based redirect** - `6a37014` (feat)
2. **Task 2: Add GetBrokerRegistrationsAsync to RegistrationService** - `0479db9` (feat)

**Plan metadata:** (docs commit — see final commit hash below)

## Files Created/Modified

- `EventCenter.Web/Components/Pages/Home.razor` - Replaced Hello World placeholder with role-based redirect component using OnInitializedAsync
- `EventCenter.Web/Services/RegistrationService.cs` - Added GetBrokerRegistrationsAsync with 5-level Include/ThenInclude chain

## Decisions Made

- No `[Authorize]` attribute on Home.razor: the page must be reachable by unauthenticated users so they can be redirected to `/auth/login`. If `[Authorize]` were applied, the AuthorizeRouteView's RedirectToLogin would handle it, but the plan specifies the belt-and-suspenders explicit check.
- `forceLoad: false` on all `NavigateTo` calls: keeps the Blazor circuit alive and avoids full page reload.
- `showAccessDenied` boolean flag ensures no content renders during the async auth check (prevents flash of "Zugriff verweigert" before role is known).
- Cancelled registrations are deliberately included in GetBrokerRegistrationsAsync — the UI layer (Plan 02) renders them with a "Storniert" badge per locked design decision.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

MSB3492 "Could not read existing file ... AssemblyInfoInputs.cache" appears during build on WSL2. This is a known .NET SDK tooling issue on WSL2 where the build tool tries to overwrite a file created in the same build invocation. It does NOT prevent compilation — the DLL was produced correctly (size increased after Task 2 addition, timestamp updated). All C# and Razor code compiled without errors.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- GetBrokerRegistrationsAsync is ready for Plan 02 to use directly in the registrations overview page
- Home.razor redirect is live — no user will ever see the Hello World placeholder again
- No blockers for Plan 02

---
*Phase: 09-navigation-makler-registrierungs-bersicht*
*Completed: 2026-03-01*
