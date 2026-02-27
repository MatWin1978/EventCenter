---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: unknown
last_updated: "2026-02-27T15:38:08.360Z"
progress:
  total_phases: 7
  completed_phases: 6
  total_plans: 25
  completed_plans: 22
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-26)

**Core value:** Makler und eingeladene Firmen können sich reibungslos für Veranstaltungen anmelden, Agendapunkte auswählen und ihre Teilnahme verwalten.
**Current focus:** Phase 7 - Cancellation & Participant Management

## Current Position

Phase: 7 of 8 (Cancellation & Participant Management)
Plan: 1 of 4
Status: In Progress
Last activity: 2026-02-27 - Completed plan 07-01 (Cancellation Service Logic)

Progress: [██████████] 88.0% (6/8 phases, 22/25 plans completed)

## Performance Metrics

**Velocity:**
- Total plans completed: 21
- Average duration: 7.5 minutes
- Total execution time: 3.05 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01 | 4 | 1303s | 325.8s |
| 02 | 4 | 2011s | 502.8s |
| 03 | 5 | 1402s | 280.4s |
| 04 | 3 | 1094s | 364.7s |
| 05 | 3 | 4087s | 1362.3s |
| 06 | 2 | 1351s | 675.5s |

**Recent Plans:**

| Phase-Plan | Duration | Tasks | Files | Date |
|------------|----------|-------|-------|------|
| 07-01 | 241s (4.0m) | 2 | 7 | 2026-02-27 |
| 06-02 | 446s (7.4m) | 2 | 3 | 2026-02-27 |
| 06-01 | 905s (15.1m) | 3 | 10 | 2026-02-27 |
| 05-03 | 3478s (57.9m) | 3 | 1 | 2026-02-27 |
| 05-02 | 430s (7.2m) | 3 | 4 | 2026-02-27 |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- Blazor Server chosen over WASM for simpler development and direct DB access
- Keycloak for centralized identity management with OIDC
- SQL Server for enterprise-grade data persistence
- Standalone application architecture (no Umbraco integration)
- [Phase 01-foundation-authentication]: Use Components/Pages/ structure for Blazor routing (not Pages/)
- [Phase 01-foundation-authentication]: Apply inclusive deadline interpretation: end-of-day in CET timezone
- [Phase 01-foundation-authentication]: Use TimeZoneConverter package for cross-platform timezone handling
- [Phase 01-foundation-authentication]: Use Components/Pages/ structure for Blazor routing (not Pages/)
- [Phase 01-foundation-authentication]: Apply inclusive deadline interpretation: end-of-day in CET timezone
- [Phase 01-foundation-authentication]: Use TimeZoneConverter package for cross-platform timezone handling
- [Phase 01-foundation-authentication]: Use RevalidatingServerAuthenticationStateProvider with 30-minute interval to prevent circuit-based authentication staleness
- [Phase 01-foundation-authentication]: Map Keycloak realm roles via OnTokenValidated event to extract roles from realm_access claim
- [Phase 01-foundation-authentication]: Use SQLite in-memory for integration tests to validate FK constraints (unlike EF Core InMemory provider)
- [Phase 01-foundation-authentication]: Install FluentValidation.DependencyInjectionExtensions for automatic validator discovery and DI integration
- [Phase 02]: Remove provider-specific HasColumnType calls for SQLite test compatibility
- [Phase 02]: EventState calculation uses TimeZoneHelper for consistent timezone handling
- [Phase 02]: Nested validation for collections using RuleForEach with SetValidator
- [Phase 02-02]: Use service layer pattern to separate business logic from UI components
- [Phase 02-02]: Store uploaded documents in wwwroot/uploads/events/{eventId}/ with GUID-prefixed filenames
- [Phase 02-02]: Block unpublish/delete operations when registrations exist (German error messages)
- [Phase 02-03]: Use page size of 15 items per page for event list pagination
- [Phase 02-03]: Use Bootstrap button groups for action buttons (Edit, Publish, Duplicate, Delete)
- [Phase 02-04]: Use helper class (AgendaItemDates) instead of tuples for CET date tracking to enable two-way binding
- [Phase 03-01]: Use RowVersion on Event entity for optimistic concurrency during registration
- [Phase 03-01]: Create explicit join table entity (RegistrationAgendaItem) for many-to-many relationship
- [Phase 03-01]: Add IsCancelled and CancellationDateUtc fields to Registration entity for future Phase 7 use
- [Phase 03-01]: Use MailKit for SMTP email sending (industry standard, cross-platform)
- [Phase 03-01]: Use Ical.Net for RFC 5545-compliant iCalendar export
- [Phase 03-01]: Create DTO (RegistrationFormModel) for form validation instead of validating entity directly
- [Phase 03-03]: Use MailKit SmtpClient with async/await for email sending
- [Phase 03]: Use Database.BeginTransactionAsync for atomic registration creation
- [Phase 03-03]: Use fire-and-forget pattern for email sending after registration (non-blocking)
- [Phase 03]: Fire-and-forget email sending with try-catch logging to prevent blocking user flow
- [Phase 03-03]: Calendar events use UTC timezone per Ical.Net recommendation
- [Phase 03]: No pagination in GetPublicEventsAsync (expected < 500 events total)
- [Phase 03-03]: Path traversal protection via Path.GetFileName() sanitization for document downloads
- [Phase 03-04]: Use broker-specific status badges (different from admin EventStatusBadge)
- [Phase 03-04]: 300ms debounce for instant search to reduce query load
- [Phase 03-04]: Active events include user's registered upcoming events
- [Phase 03-05]: Sidebar layout with sticky positioning for EventDetail page (main content left, key info right)
- [Phase 03-05]: Single-page registration flow with all sections visible (no wizard steps)
- [Phase 03-05]: Pre-select and disable mandatory agenda items (cannot be unchecked)
- [Phase 04-01]: Store InvitationStatus as string in database for readability (following existing enum pattern)
- [Phase 04-01]: Use composite primary key (EventCompanyId, AgendaItemId) for EventCompanyAgendaItemPrice join entity
- [Phase 04-01]: CustomPrice nullable - null indicates use base price from EventAgendaItem.CostForMakler
- [Phase 04-01]: Add unique filtered index on InvitationCode to prevent duplicate codes (when not null)
- [Phase 04-01]: Include ExpiresAtUtc field now for Phase 5 GUID expiration (leave null until needed)
- [Phase 04-01]: Email template shows base price vs custom price in table format for transparency
- [Phase 04]: Pricing always editable in all invitation statuses (affects future invoicing)
- [Phase 04]: Always store agenda item prices even if equal to base price (simplifies UI queries)
- [Phase 04]: Fire-and-forget email pattern with Task.Run + try-catch logging (non-blocking UX)
- [Phase 04-03]: Status-dependent action buttons show only valid actions (no disabled buttons)
- [Phase 04-03]: Base price displayed as reference in pricing table (text-muted) with separate discount/override columns
- [Phase 04-03]: Percentage discount shows calculated preview without auto-populating ManualOverride fields
- [Phase 04-03]: Email preview section collapsible with iframe/sanitized HTML rendering
- [Phase 04-03]: Save as draft vs Create & Send action buttons for flexible workflow
- [Phase 04-03]: Batch mode uses standard pricing with optional shared percentage discount
- [Phase 05]: Added CancellationComment, BookingDateUtc, and IsNonParticipation fields to EventCompany for booking management
- [Phase 05]: Rate limit of 10 requests per minute with zero queue for company booking endpoint
- [Phase 05]: Use constant-time comparison for GUID validation to prevent timing attacks
- [Phase 05]: Fire-and-forget email with Task.Run for non-blocking booking submission
- [Phase 05]: Allow Booked/Cancelled invitations in ValidateInvitationCodeAsync for status viewing
- [Phase 05-03]: Single page component handling all booking lifecycle states
- [Phase 05-03]: Compact event summary header per user decision
- [Phase 05-03]: Inline editable participant table with per-participant agenda item selection
- [Phase 05-03]: Sticky cost summary sidebar with live reactive updates
- [Phase 05-03]: Simple success confirmation page without detailed summary
- [Phase 05-03]: Management modal with cancel and non-participation options
- [Phase 05-03]: Re-booking option shown only if deadline not passed
- [Phase 06-01]: Email confirmation sent to broker (not guest) per MAIL-02 requirement
- [Phase 06-01]: Self-referencing FK uses DeleteBehavior.Restrict to prevent cascade deletion of guests
- [Phase 06-02]: Inline form pattern for guest registration (collapse/expand on button click)
- [Phase 06-02]: Manual checkbox state management using HashSet and @onchange handlers (not InputCheckbox with bind)
- [Phase 06-02]: Pre-select mandatory agenda items when guest form opens
- [Phase 06-02]: One-guest-at-a-time registration flow (form collapses after successful submission)
- [Phase 07-01]: Cancelling broker registration does NOT cascade to guest registrations (per locked user decision)
- [Phase 07-01]: CancellationReason stored as nullable string for audit trail
- [Phase 07-01]: Deadline check uses GetCurrentState() == EventState.Public (consistent with registration state machine)

### Pending Todos

None yet.

### Blockers/Concerns

**Research identified critical pitfalls to address:**
- Phase 1: Circuit-based authentication state staleness (requires IdentityRevalidatingAuthenticationStateProvider)
- Phase 1: Memory leaks from event handlers (requires IAsyncDisposable patterns)
- Phase 1: Timezone handling for deadlines (store UTC, display CET)
- Phase 3: Race conditions in concurrent registration (requires EF Core optimistic locking)
- Phase 4: Email deliverability (requires SPF/DKIM/DMARC configuration)
- Phase 5: GUID security for anonymous access (requires expiration and rate limiting)

## Session Continuity

Last session: 2026-02-27
Stopped at: Completed 07-01-PLAN.md execution
Resume file: .planning/phases/07-cancellation-participant-management/07-01-SUMMARY.md
