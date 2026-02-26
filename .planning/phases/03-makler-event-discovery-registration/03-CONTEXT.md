# Phase 3: Makler Event Discovery & Registration - Context

**Gathered:** 2026-02-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Broker-facing event discovery and registration. Makler can browse published events, view full event details with documents, and self-register for events with agenda item selection. Includes confirmation email after registration and iCalendar export. Guest registration, cancellation, and company bookings are separate phases.

</domain>

<decisions>
## Implementation Decisions

### Event list page
- Card grid layout (responsive, 2-3 columns)
- Each card shows: title, date, location, short description excerpt, status badge, cost indication
- Colored status badges: green (Plätze frei), blue (Angemeldet), red (Ausgebucht), gray (Verpasst)
- Active events displayed first, past/full events in a separate collapsible section below

### Registration flow
- Single page flow: agenda item selection, cost summary, and submit all on one page
- Confirmation dialog (modal) before final submission — summarizes selections and total costs
- After successful registration: full summary page with registration details, selected agenda items, total cost, iCal download button, and "Zurück zur Übersicht" link

### Event detail page
- Sidebar layout: main content on left (description, agenda, documents), sidebar on right with key info (date, location, contact, register button)
- Documents shown as file cards (name, type, size, download button)
- Register button and iCal export in sticky sidebar — always visible while scrolling
- Agenda items with times and costs visible on detail page before registration (full program preview)

### Search & filtering
- Instant text filter: filters event list as user types (no submit button)
- Date filter via quick presets: "Diesen Monat", "Nächste 3 Monate", "Dieses Jahr" plus optional custom range
- Search bar and filter controls in a horizontal top bar above the event grid
- Default sort: nearest upcoming events first

### Claude's Discretion
- Agenda item presentation style during registration (checklist vs cards vs other)
- Loading states and skeleton designs
- Exact spacing, typography, and color palette
- Error state handling and validation message styling
- Empty search results state design
- Mobile responsive breakpoint behavior

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 03-makler-event-discovery-registration*
*Context gathered: 2026-02-26*
