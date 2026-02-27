# Phase 7: Cancellation & Participant Management - Context

**Gathered:** 2026-02-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Users can cancel event registrations and admins can view/export participant data. Two capabilities: (1) Makler cancels own registration and guest registrations with permission checks and deadline enforcement; (2) Admin views participant lists per event and exports data to Excel.

</domain>

<decisions>
## Implementation Decisions

### Cancellation experience
- Cancel button on the event detail page (next to existing registration)
- Confirmation dialog with optional cancellation reason field before confirming
- Event-level cancellation deadline set by admin per event
- After deadline: cancel button disabled (greyed out) with text explaining the deadline date
- Same cancellation flow for both own registration and guest registrations

### Post-cancellation behavior
- Registration stays in database with status "Cancelled" (status change, not delete) — preserves full history
- Cancellation reason stored with the registration record
- Re-registration allowed after cancelling (Makler can register again if spots available)
- When Makler cancels their own registration, their guest registrations remain active (guests attend independently)
- Notification emails sent to both Makler (confirmation) and admin (informational)

### Participant list display
- "Participants" tab on admin event detail page
- Flat table with company as a filterable column (not grouped by company)
- Cancelled registrations shown in list with visual indicator (badge or strikethrough)
- Columns: Name, Company, Status (Active/Cancelled), Type (Makler/Guest/Company), Cancellation reason

### Export format & content
- Excel (.xlsx) format only
- Single "Export" dropdown button with 4 export types:
  1. Participant list (PART-01) — all registered participants for the event
  2. Contact data (PART-03) — participant contact information
  3. Non-participants (PART-04) — invited company members who did NOT register (delta between invitation list and registrations)
  4. Company list (PART-05) — companies with registrations for the event
- Exports contain only active registrations (cancelled excluded)

### Claude's Discretion
- Exact Excel column layout and formatting
- Cancellation confirmation dialog styling
- Email template content for cancellation notifications
- Filter/sort implementation on participant list
- How to compute the non-participants delta (invited members minus registered)

</decisions>

<specifics>
## Specific Ideas

- Cancellation deadline is a per-event field set by admin (not a system-wide setting)
- "Non-participants" export specifically means: for a company invitation, the Makler who were invited but did not register — useful for admin follow-up
- Guests remain independent after their registering Makler cancels — this is a deliberate choice

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 07-cancellation-participant-management*
*Context gathered: 2026-02-27*
