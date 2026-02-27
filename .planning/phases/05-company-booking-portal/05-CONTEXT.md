# Phase 5: Company Booking Portal - Context

**Gathered:** 2026-02-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Anonymous booking experience for company representatives via GUID link. Representatives can view company-specific pricing, enter participants with agenda item selections, see cost breakdowns, submit bookings, and manage (cancel/report non-participation) their bookings. Depends on Phase 4 (Company Invitations) for invitation data and GUID links.

</domain>

<decisions>
## Implementation Decisions

### Booking page flow
- Single scrollable page — event info header, participant table, options, cost summary, submit button
- Compact event summary header at top: event title, date, location, company name (no full event details)
- Friendly error page for expired/invalid GUID links with message: "This link has expired or is invalid. Please contact [admin contact] for a new invitation."
- Simple success message after booking submission ("Buchung erfolgreich eingereicht") with email confirmation note — no detailed summary on the confirmation page

### Participant entry
- Inline editable table rows — each row is one participant
- "Add participant" button adds a new row; edit/delete inline
- Required fields per participant: Anrede (salutation), Vorname, Nachname, E-Mail
- Per-participant agenda item selection — each participant has checkboxes for available agenda items (different people can attend different sessions)
- Table starts with one empty row pre-filled, ready for input

### Cost display
- Sticky/fixed cost summary that updates live as participants and options change
- Per-participant cost breakdown showing who is booked for what
- Show base price (Fixpreis), participant costs, and extra option costs as separate line items
- Grand total clearly displayed
- "Alle Preise zzgl. MwSt." note displayed near pricing

### Cancel & non-participation
- Same GUID link serves as both booking page and management page — after submission, returning shows booking status with management options
- Single action button ("Buchung bearbeiten") that opens a dialog with two choices: cancel entirely (Buchung stornieren) or report non-participation (Nicht-Teilnahme melden)
- Optional text field for comment/reason when cancelling or reporting non-participation
- After cancellation: show status page with date and comment, but also offer re-booking option if deadline hasn't passed
- Admin receives email notification on both booking submission and cancellation (MAIL-04, MAIL-05)

### Claude's Discretion
- GUID expiration timing and rate limiting implementation
- Exact table layout and responsive behavior for participant entry
- Sticky summary positioning (bottom bar vs sidebar)
- Form validation UX (inline vs on submit)
- Loading states and error handling patterns

</decisions>

<specifics>
## Specific Ideas

- The GUID link should work as a single entry point for the entire company booking lifecycle: first visit = booking form, return visit after booking = status/management page
- Cancellation and non-participation are presented through a single "edit booking" action rather than two separate buttons — cleaner UX
- Cost summary should feel responsive — updates immediately as participants/options change, not on submit

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 05-company-booking-portal*
*Context gathered: 2026-02-27*
