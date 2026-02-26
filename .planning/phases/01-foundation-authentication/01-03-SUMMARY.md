---
phase: 01-foundation-authentication
plan: 03
subsystem: role-based-ui
tags: [blazor, authorization, role-based-access, timezone, navigation]
dependency_graph:
  requires: ["01-01"]
  provides: ["role-based-pages", "timezone-conversion"]
  affects: ["future-feature-pages"]
tech_stack:
  added: ["TimeZoneConverter"]
  patterns: ["AuthorizeView", "role-based-navigation", "UTC-CET-conversion"]
key_files:
  created:
    - "EventCenter.Web/Components/Pages/Admin/Index.razor"
    - "EventCenter.Web/Components/Pages/Admin/_Imports.razor"
    - "EventCenter.Web/Components/Pages/Portal/Index.razor"
    - "EventCenter.Web/Components/Pages/Portal/_Imports.razor"
    - "EventCenter.Web/Infrastructure/Helpers/TimeZoneHelper.cs"
  modified:
    - "EventCenter.Web/Components/Layout/MainLayout.razor"
    - "EventCenter.Web/Components/Layout/NavMenu.razor"
    - "EventCenter.Web/Program.cs"
decisions:
  - "Use Components/Pages/ structure instead of Pages/ for Blazor routing"
  - "Apply inclusive deadline interpretation: end-of-day in CET timezone"
  - "Use TimeZoneConverter package for cross-platform timezone handling"
  - "Implement role-based navigation with AuthorizeView components"
metrics:
  duration: 246
  completed: "2026-02-26T12:48:00Z"
  tasks_completed: 3
  files_created: 5
  files_modified: 3
---

# Phase 01 Plan 03: Role-Based Landing Pages & Navigation Summary

**One-liner:** Created Admin and Makler portal landing pages with role-based authorization and implemented UTC/CET timezone conversion utilities with inclusive deadline handling.

## Tasks Completed

| Task | Name                                            | Commit  | Files                                          |
| ---- | ----------------------------------------------- | ------- | ---------------------------------------------- |
| 1    | Create Admin area pages with role authorization| 58cc59c | Admin/Index.razor, Admin/_Imports.razor        |
| 2    | Create Makler Portal area pages                 | 9b8e35c | Portal/Index.razor, Portal/_Imports.razor      |
| 3    | Create shared layout with navigation & timezone | 51e80a3 | MainLayout, NavMenu, TimeZoneHelper, Program.cs|

## What Was Built

### Admin Area
- **Landing page:** `/admin` route with Admin role authorization
- **Authorization:** `[Authorize(Roles = "Admin")]` attribute enforcement
- **UI components:** Placeholder cards for events, company invitations, and participants management
- **Access control:** AuthorizeView component for conditional rendering

### Makler Portal Area
- **Landing page:** `/portal` route with Makler role authorization
- **Authorization:** `[Authorize(Roles = "Makler")]` attribute enforcement
- **UI components:** Placeholder cards for events overview and registration management
- **Access control:** AuthorizeView component for conditional rendering

### Shared Layout & Navigation
- **MainLayout updates:** Authentication state display showing logged-in user or login link
- **Role-based navigation:** NavMenu with AuthorizeView showing Admin/Portal links based on user role
- **Navigation icons:** Bootstrap icons for Admin (briefcase) and Portal (list) menu items

### TimeZone Conversion Utilities
- **UTC/CET conversion:** Bidirectional timezone conversion using TimeZoneConverter package
- **DST handling:** Automatic daylight saving time transitions via TimeZoneInfo
- **Deadline interpretation:** Inclusive end-of-day logic (deadline "15.03" = "15.03 23:59:59 CET")
- **Registration validation:** IsRegistrationOpen() method for deadline checking
- **Display formatting:** German date/time format helper (dd.MM.yyyy HH:mm)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Incorrect project structure assumption**
- **Found during:** Task 1
- **Issue:** Plan specified Pages/ directory, but Blazor Server uses Components/Pages/
- **Fix:** Moved Admin and Portal pages to Components/Pages/ directory structure
- **Files modified:** Admin/Index.razor, Portal/Index.razor moved to correct location
- **Commit:** 58cc59c

**2. [Rule 1 - Bug] Missing Layout namespace in Admin _Imports**
- **Found during:** Task 1 build verification
- **Issue:** MainLayout component not found due to missing using directive
- **Fix:** Added `@using EventCenter.Web.Components.Layout` to Admin/_Imports.razor
- **Files modified:** Components/Pages/Admin/_Imports.razor
- **Commit:** 58cc59c

**3. [Rule 1 - Bug] Missing Authentication namespace in Program.cs**
- **Found during:** Task 3 build verification
- **Issue:** ChallengeAsync and SignOutAsync extension methods not found
- **Fix:** Added `using Microsoft.AspNetCore.Authentication;` to Program.cs
- **Files modified:** EventCenter.Web/Program.cs
- **Commit:** 51e80a3

## Technical Implementation Details

### Authorization Pattern
```csharp
@page "/admin"
@attribute [Authorize(Roles = "Admin")]
@layout MainLayout

<AuthorizeView Roles="Admin">
    <Authorized>
        <!-- Content for Admin users -->
    </Authorized>
    <NotAuthorized>
        <p>Zugriff verweigert. Sie benötigen Admin-Rechte.</p>
    </NotAuthorized>
</AuthorizeView>
```

### TimeZone Conversion Example
```csharp
// UTC to CET display
var cet = TimeZoneHelper.ConvertUtcToCet(utcDateTime);

// Deadline interpretation (inclusive)
var deadlineEndUtc = TimeZoneHelper.GetEndOfDayCetAsUtc(deadlineDate);
bool isOpen = TimeZoneHelper.IsRegistrationOpen(registrationDeadlineUtc);
```

### Role-Based Navigation
```razor
<AuthorizeView Roles="Admin">
    <div class="nav-item px-3">
        <NavLink class="nav-link" href="admin">
            <span class="bi bi-briefcase-fill-nav-menu"></span> Admin
        </NavLink>
    </div>
</AuthorizeView>
```

## Verification Results

All verification criteria passed:
- ✅ `dotnet build` succeeds with 0 errors (3 pre-existing warnings in auth code)
- ✅ Admin/Index.razor has `[Authorize(Roles = "Admin")]` attribute
- ✅ Portal/Index.razor has `[Authorize(Roles = "Makler")]` attribute
- ✅ NavMenu.razor uses AuthorizeView for role-based menu items
- ✅ TimeZoneHelper.cs contains all required methods:
  - ConvertUtcToCet
  - ConvertCetToUtc
  - GetEndOfDayCetAsUtc
  - IsRegistrationOpen
  - FormatDateTimeCet

## Success Criteria Met

- ✅ Admin area pages created with Admin role authorization
- ✅ Makler portal pages created with Makler role authorization
- ✅ Shared layout displays authentication state (logged in user or login link)
- ✅ Navigation menu shows role-specific options using AuthorizeView
- ✅ TimeZone helper implemented for UTC/CET conversion with DST handling
- ✅ Deadline interpretation logic implemented (inclusive end-of-day)
- ✅ Ready for EF Core migrations and database initialization in next plan

## Files Created/Modified

**Created (5 files):**
- EventCenter.Web/Components/Pages/Admin/Index.razor
- EventCenter.Web/Components/Pages/Admin/_Imports.razor
- EventCenter.Web/Components/Pages/Portal/Index.razor
- EventCenter.Web/Components/Pages/Portal/_Imports.razor
- EventCenter.Web/Infrastructure/Helpers/TimeZoneHelper.cs

**Modified (3 files):**
- EventCenter.Web/Components/Layout/MainLayout.razor (added auth state display)
- EventCenter.Web/Components/Layout/NavMenu.razor (added role-based navigation)
- EventCenter.Web/Program.cs (added missing Authentication namespace)

## Next Steps

This plan establishes the foundation for:
1. **Database migrations** (Plan 04): Initialize SQL Server with schema and seed data
2. **Event management features**: Admin can create/edit events using Admin area
3. **Registration features**: Makler can view and register for events using Portal area
4. **Deadline validation**: Use TimeZoneHelper for accurate registration deadline checks
5. **Role-based routing**: Extend authorization to future feature pages

## Self-Check: PASSED

**Files created verification:**
```
FOUND: EventCenter.Web/Components/Pages/Admin/Index.razor
FOUND: EventCenter.Web/Components/Pages/Admin/_Imports.razor
FOUND: EventCenter.Web/Components/Pages/Portal/Index.razor
FOUND: EventCenter.Web/Components/Pages/Portal/_Imports.razor
FOUND: EventCenter.Web/Infrastructure/Helpers/TimeZoneHelper.cs
```

**Commits verification:**
```
FOUND: 58cc59c (Task 1: Admin area pages)
FOUND: 9b8e35c (Task 2: Makler Portal pages)
FOUND: 51e80a3 (Task 3: Layout, navigation, timezone helper)
```

All files and commits verified successfully.
