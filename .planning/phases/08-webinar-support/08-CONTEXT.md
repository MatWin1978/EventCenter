# Phase 8: Webinar Support - Context

**Gathered:** 2026-02-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Add webinar as a second event type. Admins create webinars with external registration URLs (no internal registration form). Brokers see webinars alongside in-person events with clear visual distinction and can filter by type. Webinar detail page shows an external CTA button instead of the registration form.

</domain>

<decisions>
## Implementation Decisions

### Admin creation flow
- Type selector at the top of the create/edit form: "In-Person / Webinar" — switching type shows/hides relevant fields
- External registration URL field is required to publish (not to save as draft); can't publish a webinar without a URL
- Hide registration deadline and capacity fields for webinars — not applicable
- No agenda section for webinars — hidden entirely

### Visual differentiation
- Webinar events display a Bootstrap badge with `bi-camera-video` icon and text "Webinar" (e.g., `badge bg-info`)
- Badge appears in both the admin event list and the broker portal event list
- On the webinar event detail page (broker view): show a prominent webinar banner/header callout at the top of the page (in addition to replacing the registration form)

### Filtering behavior
- Tab bar above event list: **All / In-Person / Webinar**
- Additive with other filters — if search is active, the tab narrows within those results
- Default tab is **All**
- Both admin event management list and broker portal event list get the filter tabs

### External link UX
- Webinar detail page shows a prominent CTA button ("Zur Webinar-Anmeldung") opening the external URL in a new tab
- Button only — no explanatory text about external registration
- iCal calendar export button remains on webinar event detail page
- If a broker navigates to `/portal/events/{id}/register` for a webinar: redirect to the event detail page

### Claude's Discretion
- Exact banner/callout styling on the webinar detail page header
- Badge color choice (bg-info vs other Bootstrap color)
- Which fields beyond deadline and capacity are hidden/shown for webinars

</decisions>

<specifics>
## Specific Ideas

- No specific references mentioned — standard Bootstrap + Bootstrap Icons patterns throughout

</specifics>

<deferred>
## Deferred Ideas

- None — discussion stayed within phase scope

</deferred>

---

*Phase: 08-webinar-support*
*Context gathered: 2026-02-27*
