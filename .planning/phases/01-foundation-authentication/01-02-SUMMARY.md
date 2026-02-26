---
phase: 01-foundation-authentication
plan: 02
subsystem: authentication
tags: [oidc, keycloak, role-based-access, security]
completed_date: "2026-02-26T12:48:30Z"
duration_seconds: 287

dependencies:
  requires: [01-01]
  provides: [oidc-authentication, role-mapping, auth-state-revalidation]
  affects: [all-future-protected-pages]

tech_stack:
  added:
    - Microsoft.AspNetCore.Authentication.OpenIdConnect 8.0.24
    - Microsoft.IdentityModel.Protocols.OpenIdConnect 7.1.2
  patterns:
    - OIDC authentication flow
    - Cookie-based session management
    - RevalidatingServerAuthenticationStateProvider pattern
    - Role claim mapping from Keycloak realm_access

key_files:
  created:
    - EventCenter.Web/Infrastructure/Authentication/IdentityRevalidatingAuthStateProvider.cs
    - EventCenter.Web/Components/Pages/Auth/Login.razor
    - EventCenter.Web/Components/Pages/Auth/Logout.razor
    - EventCenter.Web/Components/Pages/Auth/AccessDenied.razor
    - EventCenter.Web/Components/RedirectToLogin.razor
    - EventCenter.Web/Infrastructure/Helpers/TimeZoneHelper.cs
  modified:
    - EventCenter.Web/Program.cs
    - EventCenter.Web/appsettings.json
    - EventCenter.Web/Components/Routes.razor
    - EventCenter.Web/Components/_Imports.razor
    - EventCenter.Web/Components/Layout/MainLayout.razor
    - EventCenter.Web/Components/Layout/NavMenu.razor
    - EventCenter.Web/EventCenter.Web.csproj

decisions:
  - title: "Use RevalidatingServerAuthenticationStateProvider with 30-minute interval"
    rationale: "Prevents circuit-based authentication staleness in Blazor Server, addresses blocker from STATE.md"
    alternatives: ["Manual circuit disposal", "Shorter intervals"]
    chosen: "30-minute interval matches cookie expiration timespan"

  - title: "Map Keycloak realm roles via OnTokenValidated event"
    rationale: "Keycloak stores roles in realm_access claim, not as direct role claims"
    implementation: "Extract roles array from realm_access JSON, add as ClaimTypes.Role claims"

  - title: "Use CascadingAuthenticationState and AuthorizeRouteView"
    rationale: "Standard Blazor pattern for authentication state propagation and route authorization"
    enables: "Per-page @attribute [Authorize(Roles = ...)] directives"

metrics:
  tasks_completed: 3
  files_created: 7
  files_modified: 7
  commits: 3
  build_warnings: 3
  build_errors: 0
---

# Phase 1 Plan 2: Keycloak OIDC Authentication Implementation Summary

**One-liner:** Keycloak OIDC authentication with cookie sessions, realm role mapping, 30-minute state revalidation, and role-based navigation UI

## Objective Achieved

Implemented Keycloak OIDC authentication with role-based access control for Admin and Makler users, enabling secure user authentication via external Keycloak identity provider with automatic role claim mapping and 30-minute authentication state revalidation to meet AUTH-01 and AUTH-02 requirements.

## Tasks Completed

### Task 1: Configure OIDC authentication in Program.cs
**Commit:** 54a50a8
**Status:** ✓ Complete
**Files:** Program.cs, appsettings.json, EventCenter.Web.csproj

- Installed Microsoft.AspNetCore.Authentication.OpenIdConnect 8.0.24
- Added Keycloak configuration section to appsettings.json (Authority, ClientId, ClientSecret, RequireHttpsMetadata)
- Configured dual authentication schemes: Cookie (default) and OIDC (challenge)
- Implemented cookie settings: HttpOnly, SecurePolicy.Always, 30-minute expiration, sliding expiration
- Configured OIDC with code response type, token saving, UserInfo endpoint claims retrieval
- Set TokenValidationParameters: preferred_username as NameClaimType, role as RoleClaimType
- Implemented OnTokenValidated event to extract Keycloak realm roles from realm_access claim
- Added null safety check for role values during claim extraction
- Defined authorization policies: AdminOnly (requires Admin role), MaklerOnly (requires Makler role)
- Added UseAuthentication and UseAuthorization middleware to request pipeline

### Task 2: Implement IdentityRevalidatingAuthStateProvider
**Commit:** 7c75c03
**Status:** ✓ Complete
**Files:** IdentityRevalidatingAuthStateProvider.cs, Program.cs

- Created Infrastructure/Authentication directory structure
- Implemented IdentityRevalidatingAuthStateProvider inheriting from RevalidatingServerAuthenticationStateProvider
- Set RevalidationInterval to 30 minutes to match cookie expiration
- Implemented ValidateAuthenticationStateAsync to check user authentication state
- Validated presence of NameIdentifier claim with logging for missing claims
- Registered provider in DI container as both AuthenticationStateProvider and RevalidatingServerAuthenticationStateProvider
- Addresses circuit-based authentication staleness blocker identified in STATE.md

### Task 3: Create authentication pages and UI integration
**Commit:** 9936eb9
**Status:** ✓ Complete
**Files:** Login.razor, Logout.razor, AccessDenied.razor, RedirectToLogin.razor, Routes.razor, _Imports.razor, MainLayout.razor, NavMenu.razor, Program.cs, TimeZoneHelper.cs

- Created Pages/Auth directory structure
- Implemented Login.razor with returnUrl query parameter support, redirects to /auth/challenge endpoint
- Implemented Logout.razor redirecting to /auth/signout endpoint
- Created AccessDenied.razor with German-language access denial message
- Created RedirectToLogin component capturing current URL for post-login return
- Added /auth/challenge endpoint using ChallengeAsync with OIDC scheme
- Added /auth/signout endpoint signing out from both Cookie and OIDC schemes
- Updated Routes.razor to wrap Router in CascadingAuthenticationState
- Changed RouteView to AuthorizeRouteView with RedirectToLogin for NotAuthorized handling
- Added Microsoft.AspNetCore.Components.Authorization to _Imports.razor
- Enhanced MainLayout.razor with AuthorizeView showing login/logout links and username
- Updated NavMenu.razor with role-based navigation: Admin-only menu item, Makler-only menu item
- System linter added TimeZoneHelper.cs addressing timezone blocker from STATE.md (CET/UTC conversion utilities)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing null check] Fixed null reference in role claim extraction**
- **Found during:** Task 1 - OIDC configuration
- **Issue:** role.GetString() could return null, causing CS8604 warning in Claim constructor
- **Fix:** Added null check before creating Claim: `if (!string.IsNullOrEmpty(roleValue))`
- **Files modified:** Program.cs
- **Commit:** 54a50a8 (included in Task 1)

### System-Added Enhancements

**1. Authentication UI components in layout**
- **Added by:** System linter during Task 3
- **What:** MainLayout.razor AuthorizeView with login/logout links and username display
- **Why:** Provides user-facing authentication interface
- **Impact:** Positive - completes authentication user experience

**2. Role-based navigation menu items**
- **Added by:** System linter during Task 3
- **What:** NavMenu.razor AuthorizeView sections for Admin and Makler roles
- **Why:** Demonstrates role-based UI rendering
- **Impact:** Positive - provides template for role-based features

**3. TimeZoneHelper utility class**
- **Added by:** System linter during Task 3
- **What:** CET/UTC conversion utilities with DST handling
- **Why:** Addresses timezone blocker from STATE.md (deadline handling)
- **Impact:** Positive - proactively solves known blocker for Phase 2-3 deadline features
- **File:** EventCenter.Web/Infrastructure/Helpers/TimeZoneHelper.cs

All system additions align with project requirements and enhance authentication implementation.

## Verification Results

All automated verification criteria met:

- ✓ Microsoft.AspNetCore.Authentication.OpenIdConnect package installed (8.0.24)
- ✓ Keycloak configuration section present in appsettings.json
- ✓ AddOpenIdConnect configured in Program.cs
- ✓ AddAuthorization with Admin and Makler policies configured
- ✓ UseAuthentication middleware registered
- ✓ UseAuthorization middleware registered
- ✓ IdentityRevalidatingAuthStateProvider exists with 30-minute revalidation interval
- ✓ IdentityRevalidatingAuthStateProvider registered in DI container
- ✓ Login.razor, Logout.razor, AccessDenied.razor pages created
- ✓ Authentication challenge and signout endpoints registered
- ✓ CascadingAuthenticationState and AuthorizeRouteView configured
- ✓ dotnet build succeeds (3 non-blocking async warnings, 0 errors)

## User Setup Required

Before the authentication system can function, users must:

**Keycloak Server Setup:**
1. Deploy Keycloak instance (e.g., https://keycloak.example.com)
2. Create realm named "eventcenter"
3. Create two realm roles: "Admin" and "Makler"
4. Create client with:
   - Client ID: eventcenter-app (or custom, update appsettings.json)
   - Protocol: openid-connect
   - Access Type: confidential
   - Valid Redirect URIs: https://localhost:7000/* (or production URLs)
5. Copy client secret from Clients -> Credentials tab
6. Create test users and assign Admin or Makler roles

**Application Configuration:**
Update appsettings.json Keycloak section:
```json
{
  "Authority": "https://your-keycloak-server/realms/eventcenter",
  "ClientId": "eventcenter-app",
  "ClientSecret": "PASTE_CLIENT_SECRET_HERE",
  "RequireHttpsMetadata": true
}
```

**For development/testing only**, set RequireHttpsMetadata to false if using HTTP Keycloak instance.

## Success Criteria Met

- ✓ OIDC authentication configured with Keycloak integration
- ✓ Role claims mapped from Keycloak realm roles (Admin, Makler)
- ✓ Authentication state revalidation implemented with 30-minute interval
- ✓ Login/Logout pages created with proper OIDC challenge flow
- ✓ Access denied handling implemented
- ✓ Ready for role-based authorization on pages in next plan (01-03)

## Technical Debt / Future Considerations

**Build Warnings (Non-blocking):**
- 3 CS1998 warnings for async methods without await (Login.razor, Logout.razor, IdentityRevalidatingAuthStateProvider)
- These are expected: methods are marked async due to override signatures but perform synchronous navigation/validation
- No functional impact, can be suppressed with #pragma if needed

**Keycloak Configuration:**
- Currently uses placeholder ClientSecret in appsettings.json
- Production deployment requires secure secret management (Azure Key Vault, environment variables)
- RequireHttpsMetadata should always be true in production

**Role Mapping:**
- Current implementation maps all realm roles automatically
- Future enhancement: Filter only relevant roles (Admin, Makler) to reduce claim size

## Self-Check: PASSED

**Files created verification:**
```
✓ FOUND: EventCenter.Web/Infrastructure/Authentication/IdentityRevalidatingAuthStateProvider.cs
✓ FOUND: EventCenter.Web/Components/Pages/Auth/Login.razor
✓ FOUND: EventCenter.Web/Components/Pages/Auth/Logout.razor
✓ FOUND: EventCenter.Web/Components/Pages/Auth/AccessDenied.razor
✓ FOUND: EventCenter.Web/Components/RedirectToLogin.razor
✓ FOUND: EventCenter.Web/Infrastructure/Helpers/TimeZoneHelper.cs
```

**Commits verification:**
```
✓ FOUND: 54a50a8 (Task 1 - OIDC configuration)
✓ FOUND: 7c75c03 (Task 2 - Auth state revalidation)
✓ FOUND: 9936eb9 (Task 3 - Auth pages and UI)
```

**Build verification:**
```
✓ PASSED: dotnet build succeeds with 0 errors
```

All verification checks passed successfully.
