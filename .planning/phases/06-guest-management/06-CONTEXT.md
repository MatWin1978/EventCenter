# Phase 6: Guest Management - Context

**Gathered:** 2026-02-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Brokers can register companions (guests) for events they're attending, with limit enforcement and cost display. Guest cancellation is handled in Phase 7. This phase covers registration, data entry, listing, and limit enforcement only.

</domain>

<decisions>
## Implementation Decisions

### Registration flow
- Guest registration lives on the event detail page (not a separate page)
- "Begleitperson anmelden" button expands an inline form section below the broker's own registration
- One guest at a time — form collapses after submission, button reappears for next guest
- After successful registration: simple success message ("Begleitperson erfolgreich angemeldet"), form collapses, page refreshes to show updated state
- Only registered brokers see the guest registration section (prerequisite: broker must be registered themselves)

### Guest data entry
- Required fields: Anrede, Vorname, Nachname, E-Mail, Beziehungstyp
- Address replaced by E-Mail (deviation from GREG-03 — address not needed, email more useful for communication)
- Beziehungstyp is free text (not a dropdown)
- Guest selects their own agenda items from the available list (with CompanionParticipationCost pricing)
- Costs shown during registration as the broker selects agenda items — no surprises

### Guest listing & costs
- "Meine Begleitpersonen" section on event detail page, below the broker's own registration
- Each guest row shows their name, agenda item costs (CompanionParticipationCost)
- Total cost for all guests shown at bottom
- No remove/cancel button on guest list — cancellation handled in Phase 7

### Limit enforcement
- "Begleitpersonen: 1/2" counter visible near the registration button so broker always knows remaining slots
- When limit reached: button disabled (grayed out), text below: "Maximale Anzahl Begleitpersonen erreicht (2/2)"
- If MaxCompanions = 0: guest section hidden entirely — no guest-related UI shown
- If broker not registered: guest section hidden entirely

### Claude's Discretion
- Exact form layout and field ordering
- Success message styling (toast vs inline alert)
- Agenda item display format within the guest form
- Loading states during registration submission

</decisions>

<specifics>
## Specific Ideas

- GREG-03 modification: replace "Adresse" with "E-Mail" — update requirement accordingly
- Flow mirrors the existing event detail page patterns from Phase 3
- German labels throughout (consistent with rest of app)

</specifics>

<deferred>
## Deferred Ideas

- Guest cancellation/removal — Phase 7 (Cancellation & Participant Management)

</deferred>

---

*Phase: 06-guest-management*
*Context gathered: 2026-02-27*
