# Phase 9: Navigation & Makler-Registrierungsübersicht - Context

**Gathered:** 2026-03-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Improve the app's entry points and navigation flow for both roles, and implement the "Meine Anmeldungen" registration overview page for brokers.

Three deliverables:
1. Smart role-based redirect at `/` after login (Admin → `/admin/events`, Makler → `/portal/events`)
2. Navigation stays hub-based (existing sidebar structure preserved) — Home.razor becomes the smart redirect
3. New `/portal/registrations` page showing all broker registrations as cards, linking to existing EventDetail page

</domain>

<decisions>
## Implementation Decisions

### Login redirect & home page
- Unauthenticated user at `/` → redirect to `/auth/login` automatically (no public landing page)
- Authenticated Admin → redirect to `/admin/events`
- Authenticated Makler → redirect to `/portal/events`
- Authenticated user with no known role → show "Access Denied / contact admin" message on the page (not auto-logout)
- No return URL handling — always land on main list after login regardless of originally requested URL

### Navigation structure
- Admin sidebar: one "Admin" link → `/admin` dashboard; navigation to sub-sections via dashboard cards (no sub-links in sidebar)
- Makler sidebar: one "Portal" link → `/portal` dashboard; navigation via dashboard cards (no sub-links in sidebar)
- Existing NavMenu.razor structure kept as-is — no additional links added to sidebar
- Branding text ("EventCenter.Web") not changed — out of scope

### Registrations overview page (`/portal/registrations`)
- Layout: Card-based (same visual style as the portal event list)
- Cancelled registrations: Visible with a "Storniert" badge, visually de-emphasized (greyed out / reduced opacity)
- Guest registrations: Shown inline under the broker's registration card (not separate cards)
- Empty state: Message "Sie haben sich noch für keine Veranstaltung angemeldet" + button to `/portal/events`
- Each card shows: event name, event date, event location or "Webinar" label, registration date ("Angemeldet am"), booked agenda items, extra options (booked add-ons), total cost
- Extra options shown in card or as expandable detail (Claude's discretion on exact layout)
- Cards are purely informational — no action buttons on the overview; all actions (cancellation etc.) happen on EventDetail

### Registration detail — no new page
- Clicking a registration card navigates to the existing `/portal/events/{eventId}` page
- EventDetail already shows registration status, guests, and cancellation — no new detail page needed

### Claude's Discretion
- Exact card layout details (spacing, which elements in header vs body vs footer)
- Whether extra options collapse or are always visible on the card
- Loading skeleton or spinner pattern for the registrations page
- Service method design for fetching broker's registrations with agenda items, extras, and guests

</decisions>

<specifics>
## Specific Ideas

- The card style should match the portal event list cards (EventCard.razor pattern) for visual consistency
- Guest names should appear inline under the broker's registration (not a separate section of the page)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 09-navigation-makler-registrierungs-bersicht*
*Context gathered: 2026-03-01*
