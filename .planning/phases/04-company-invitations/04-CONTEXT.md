# Phase 4: Company Invitations - Context

**Gathered:** 2026-02-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Admins can invite companies to events with custom pricing per agenda item and send invitation emails with secure GUID links. Admins manage invitation lifecycle (draft, send, track status, delete). The company-facing booking experience (what happens after clicking the GUID link) is a separate phase.

</domain>

<decisions>
## Implementation Decisions

### Pricing configuration
- Both percentage discount AND per-item price override available
- Percentage discount applies first, then admin can tweak individual items on top
- Base (default) event price displayed as reference next to each custom price field
- Pricing is always editable, even after the company has booked (affects future invoicing)

### Invitation creation flow
- Single page form on the event detail page: select company, configure pricing, optional personal message
- "Company Invitations" tab on the event detail page — invitations scoped to an event
- Two creation modes: single invite for custom pricing, batch invite for standard pricing
- Admin can choose "Save as draft" or "Create & Send" — draft option available

### Status view & management
- Sortable table layout: company name, contact, status, date sent, actions
- Four statuses: Draft, Sent, Booked, Cancelled
- Status-dependent actions: Draft (edit/send/delete), Sent (edit/resend/delete), Booked (edit/view booking), Cancelled (edit/re-invite)
- Edit action available in all statuses (pricing is always editable)
- Deletion requires confirmation dialog showing company name and status

### Email content & template
- HTML email with branding (logo, styled event details, call-to-action button)
- Content: event name, date, location, company-specific pricing summary per agenda item, secure GUID link
- Admin can add a personal message included alongside auto-generated content
- Optional email preview available before sending (Preview button + Send button)

### Claude's Discretion
- Email HTML template design and styling details
- Batch invitation UI specifics (company multi-select approach)
- Table sorting defaults and column ordering
- GUID generation implementation details
- Exact form layout and field grouping

</decisions>

<specifics>
## Specific Ideas

- Pricing form should show base price as read-only reference next to each editable custom price field
- Percentage discount field at the top that auto-calculates per-item prices, with ability to override individual items after
- Status-dependent action buttons that change based on invitation state — no disabled/grayed-out buttons for invalid actions

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 04-company-invitations*
*Context gathered: 2026-02-27*
