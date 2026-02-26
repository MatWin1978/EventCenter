# Phase 2: Admin Event Management - Context

**Gathered:** 2026-02-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Enable admins to create, configure, and publish in-person events with agenda items and extra options. Covers EVNT-01 through EVNT-04, AGND-01 through AGND-03, and XOPT-01/XOPT-02. Does not include webinars (Phase 8), company invitations (Phase 4), or broker-facing views (Phase 3).

</domain>

<decisions>
## Implementation Decisions

### Event form & workflow
- Single-page form with clear sections (basic info, dates, agenda, options) — no multi-step wizard
- Admin enters all dates in CET; system converts to UTC behind the scenes using existing TimeZoneHelper
- Include simple file upload for event documents (PDFs, flyers) — brokers will download these in Phase 3 (MDET-02)
- Include explicit contact person fields (name, email, phone) — not derived from creating admin

### Event list & overview
- Data table layout with sortable columns: Title, Date, Location, Status badge, Registration count (x/max), Published indicator
- Default view shows upcoming/active events only; toggle/tab to see past/finished events
- Event duplication action available — copy event with all agenda items and options, admin adjusts dates/details

### Agenda item management
- Agenda items managed inline on the event form as a sub-section
- Sorted by StartDateTime (chronological) — no manual drag-to-reorder
- Each agenda item has two toggles: "Makler can participate" and "Guests can participate" (both on by default, per AGND-03)
- Extra options (Zusatzoptionen) follow the same inline pattern, as a separate sub-section below agenda items

### Publishing & state lifecycle
- Publish action requires confirmation dialog ("This event will be visible to all brokers. Continue?")
- EventState (Public, DeadlineReached, Finished) is fully automatic — calculated from current date vs event dates. Admin cannot override state, only controls IsPublished
- Color-coded status badges: Draft=gray, Public=green, DeadlineReached=orange, Finished=blue
- Unpublishing blocked if registrations exist — admin must cancel all registrations first before unpublishing

### Claude's Discretion
- Exact form field layout, spacing, and section ordering
- Validation error message placement and styling
- File upload implementation details (storage location, size limits)
- Table pagination strategy and page size
- Confirmation dialog design

</decisions>

<specifics>
## Specific Ideas

- Event form should feel like a standard admin backoffice — functional, not fancy
- Registration count (e.g., "12/50") visible directly in the event list table for quick overview
- German language for all UI labels (consistent with Deutsch-only v1 scope)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 02-admin-event-management*
*Context gathered: 2026-02-26*
