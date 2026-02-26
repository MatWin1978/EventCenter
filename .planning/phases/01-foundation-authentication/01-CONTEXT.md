# Phase 1: Foundation & Authentication - Context

**Gathered:** 2026-02-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Establish Blazor Server application with authenticated access for admins and brokers via Keycloak/OIDC. Create domain entities (Event, EventAgendaItem, EventCompany, Registrations) with EF Core configurations and SQL Server schema. This phase delivers infrastructure — no UI features beyond login/logout and basic navigation.

</domain>

<decisions>
## Implementation Decisions

### Auth structure
- Two roles only: Admin and Makler (broker)
- Admin manages all backoffice functionality; Makler views events and registers
- Authentication state revalidation every 30 minutes (IdentityRevalidatingAuthenticationStateProvider)
- Anonymous company access via GUID links only — no additional email verification step
- Rate limiting + expiration on GUID endpoints for security
- URL paths for area separation: `/admin/*` and `/portal/*` with shared layout (single Blazor app)

### DateTime handling
- Store all timestamps as UTC in database (`DateTime.UtcNow`)
- Display in CET/CEST for users
- Registration deadline interpretation: inclusive end-of-day (deadline "15.03" allows registration until 15.03 23:59:59 CET)
- Use `DateTime` type with UTC convention (not DateTimeOffset) for simpler EF Core mapping
- Admin UI: timezone hidden, CET assumed — no timezone picker

### Project layout
- Single project structure: EventCenter.Web with folders
- Blazor pages organized by feature: Pages/Events/, Pages/Registrations/, Pages/Companies/
- Shared components use feature prefix: EventCard, EventModal, RegistrationForm
- Domain entities in Domain/ folder: Domain/Entities/, Domain/EventCenterDbContext.cs

### Validation strategy
- FluentValidation for all domain validation rules
- Error messages in German only (no resource files)
- Database CHECK constraints as defense in depth for critical fields (dates, limits)

### Claude's Discretion
- Whether validation runs at UI level, service level, or both (Claude to decide based on UX vs. complexity tradeoff)
- Exact folder structure within Domain/, Services/
- Naming conventions for validators (e.g., EventValidator.cs)
- Implementation of IAsyncDisposable patterns for components

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard ASP.NET Core / Blazor Server patterns.

Research already identified critical pitfalls to address in this phase:
- IdentityRevalidatingAuthenticationStateProvider for circuit auth staleness
- IAsyncDisposable patterns for memory leak prevention
- UTC storage with CET display for timezone correctness

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 01-foundation-authentication*
*Context gathered: 2026-02-26*
