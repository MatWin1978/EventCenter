# Phase 9: Navigation & Makler-Registrierungsübersicht - Research

**Researched:** 2026-03-01
**Domain:** Blazor Server navigation, role-based redirect, EF Core query design
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

#### Login redirect & home page
- Unauthenticated user at `/` → redirect to `/auth/login` automatically (no public landing page)
- Authenticated Admin → redirect to `/admin/events`
- Authenticated Makler → redirect to `/portal/events`
- Authenticated user with no known role → show "Access Denied / contact admin" message on the page (not auto-logout)
- No return URL handling — always land on main list after login regardless of originally requested URL

#### Navigation structure
- Admin sidebar: one "Admin" link → `/admin` dashboard; navigation to sub-sections via dashboard cards (no sub-links in sidebar)
- Makler sidebar: one "Portal" link → `/portal` dashboard; navigation via dashboard cards (no sub-links in sidebar)
- Existing NavMenu.razor structure kept as-is — no additional links added to sidebar
- Branding text ("EventCenter.Web") not changed — out of scope

#### Registrations overview page (`/portal/registrations`)
- Layout: Card-based (same visual style as the portal event list)
- Cancelled registrations: Visible with a "Storniert" badge, visually de-emphasized (greyed out / reduced opacity)
- Guest registrations: Shown inline under the broker's registration card (not separate cards)
- Empty state: Message "Sie haben sich noch für keine Veranstaltung angemeldet" + button to `/portal/events`
- Each card shows: event name, event date, event location or "Webinar" label, registration date ("Angemeldet am"), booked agenda items, extra options (booked add-ons), total cost
- Extra options shown in card or as expandable detail (Claude's discretion on exact layout)
- Cards are purely informational — no action buttons on the overview; all actions (cancellation etc.) happen on EventDetail
- Clicking a registration card navigates to the existing `/portal/events/{eventId}` page

#### Registration detail — no new page
- Clicking a registration card navigates to the existing `/portal/events/{eventId}` page
- EventDetail already shows registration status, guests, and cancellation — no new detail page needed

### Claude's Discretion
- Exact card layout details (spacing, which elements in header vs body vs footer)
- Whether extra options collapse or are always visible on the card
- Loading skeleton or spinner pattern for the registrations page
- Service method design for fetching broker's registrations with agenda items, extras, and guests

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.
</user_constraints>

---

## Summary

Phase 9 has three deliverables: (1) smart role-based redirect on `Home.razor` at `/`, (2) NavMenu remains unchanged, (3) a new `/portal/registrations` page showing all broker registrations as cards. All work is pure Blazor Server — no new libraries needed, no DB schema changes, no migrations.

The redirect logic in `Home.razor` is straightforward: inject `AuthenticationStateProvider`, check auth state in `OnInitializedAsync`, and call `NavigationManager.NavigateTo` to the appropriate destination. The "no known role" path stays on the page and shows an inline message. The existing `RedirectToLogin` component already handles unauthenticated users via `AuthorizeRouteView`.

The registrations page requires one new service method on `RegistrationService` that queries broker registrations including their event, agenda items, selected options, and guest registrations. The card structure follows the `EventCard.razor` pattern (Bootstrap card with `card-body`, `card-footer`). Cancelled registrations use Bootstrap's `opacity-50` or similar utility class. Guest rows render inline within the broker's card.

**Primary recommendation:** Implement Home.razor redirect first (fastest win), then build the service method with full eager loading, then build the page component.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Blazor Server | .NET 8 | Component-based UI with SignalR | Already in use; all existing pages use it |
| Bootstrap 5 | bundled | Cards, badges, opacity utilities | Already in use; matches existing EventCard design |
| Microsoft.AspNetCore.Components.Authorization | .NET 8 | `AuthenticationStateProvider`, `AuthorizeView` | Established pattern throughout codebase |
| Microsoft.EntityFrameworkCore | .NET 8 | EF Core for DB query | Already registered and used in RegistrationService |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| TimeZoneHelper | project | Format UTC → CET for display | Used in every date display in portal |
| NavigationManager | .NET 8 | `NavigateTo()` for programmatic redirect | Used in Login.razor, EventDetail.razor |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Inline redirect in Home.razor | Separate RedirectToHome component | Home.razor redirect is simpler; dedicated component adds indirection without benefit |
| EF Core Include chains | Manual joins / multiple queries | Include chains maintain existing service layer pattern |

**Installation:** No new packages required. All dependencies already present.

---

## Architecture Patterns

### Recommended Project Structure

```
EventCenter.Web/
├── Components/Pages/
│   ├── Home.razor                         # MODIFY: smart role-based redirect
│   └── Portal/
│       └── Registrations/
│           └── RegistrationList.razor     # NEW: /portal/registrations overview
├── Services/
│   └── RegistrationService.cs             # MODIFY: add GetBrokerRegistrationsAsync
```

### Pattern 1: Role-Based Redirect in Home.razor

**What:** Replace the static Home content with `OnInitializedAsync` that reads auth state, inspects roles, and redirects accordingly.

**When to use:** Entry-point pages that need to route users to role-appropriate destinations.

**Example:**
```csharp
// Pattern from Login.razor (existing) + AuthenticationStateProvider pattern
@page "/"
@inject NavigationManager Navigation
@inject AuthenticationStateProvider AuthStateProvider

@code {
    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            Navigation.NavigateTo("/auth/login", forceLoad: false);
            return;
        }

        if (user.IsInRole("Admin"))
        {
            Navigation.NavigateTo("/admin/events", forceLoad: false);
            return;
        }

        if (user.IsInRole("Makler"))
        {
            Navigation.NavigateTo("/portal/events", forceLoad: false);
            return;
        }

        // No known role — fall through to inline "Access Denied" message
        // (render is shown below in the template)
    }
}
```

**Critical note on unauthenticated path:** The existing `Routes.razor` uses `<AuthorizeRouteView>` with `<NotAuthorized><RedirectToLogin /></NotAuthorized>`. This means unauthenticated users who hit `/` will be caught by `RedirectToLogin` and sent to `/auth/login` automatically — BEFORE `Home.razor`'s `OnInitializedAsync` can fire. The explicit check in `Home.razor` is a defensive belt-and-suspenders measure for the Blazor circuit where auth state may be initially uncertain.

**Rendering approach:** Home.razor needs a small render fragment for the "no known role" message. The component renders blank during the async check, then shows the message only if authenticated but role-less. Use a `bool showAccessDenied` flag set after the redirect checks.

### Pattern 2: Broker Registrations Query (EF Core)

**What:** A new `GetBrokerRegistrationsAsync` method on `RegistrationService` that fetches all broker-type registrations for a given email with full eager loading.

**When to use:** Any time the registrations overview page loads.

**Example:**
```csharp
// Source: follows existing GetGuestRegistrationsAsync / GetRegistrationWithDetailsAsync patterns
public async Task<List<Registration>> GetBrokerRegistrationsAsync(string brokerEmail)
{
    return await _context.Registrations
        .Include(r => r.Event)
        .Include(r => r.RegistrationAgendaItems)
            .ThenInclude(rai => rai.AgendaItem)
        .Include(r => r.SelectedOptions)      // EventOption navigation property
        .Include(r => r.GuestRegistrations)   // child guest registrations
            .ThenInclude(g => g.RegistrationAgendaItems)
                .ThenInclude(rai => rai.AgendaItem)
        .Where(r =>
            r.Email.Equals(brokerEmail) &&
            r.RegistrationType == RegistrationType.Makler &&
            r.ParentRegistrationId == null)
        .OrderByDescending(r => r.RegistrationDateUtc)
        .ToListAsync();
}
```

**Data model notes confirmed from codebase:**
- `Registration.RegistrationAgendaItems` → ICollection of `RegistrationAgendaItem` with `AgendaItem` nav prop
- `Registration.SelectedOptions` → ICollection of `EventOption` (many-to-many; existing join tracked by EF)
- `Registration.GuestRegistrations` → ICollection of child `Registration` (where `ParentRegistrationId` = broker's Id)
- `Registration.IsCancelled` — boolean, NOT removed from results (show with "Storniert" badge)
- `Registration.Event.EventType` — `EventType.InPerson` or `EventType.Webinar` (for "Webinar" vs location label)

### Pattern 3: Registration List Card Structure

**What:** Cards matching `EventCard.razor` visual style — Bootstrap `card h-100`, header/body/footer structure.

**Card content mapping (from CONTEXT.md decisions):**

| Field | Source | Display |
|-------|--------|---------|
| Event name | `r.Event.Title` | Card header / title |
| Event date | `r.Event.StartDateUtc` | `TimeZoneHelper.FormatDateTimeCet(...)` |
| Event location or "Webinar" | `r.Event.Location` / `r.Event.EventType` | `r.Event.EventType == EventType.Webinar ? "Webinar" : r.Event.Location` |
| Registration date | `r.RegistrationDateUtc` | "Angemeldet am dd.MM.yyyy" |
| Booked agenda items | `r.RegistrationAgendaItems` | List of `rai.AgendaItem.Title` + cost |
| Extra options | `r.SelectedOptions` | Names + prices (always visible or collapse) |
| Total cost | computed | Sum of agenda item costs + option prices |
| Cancelled status | `r.IsCancelled` | "Storniert" badge + opacity-50 wrapper |
| Guests (inline) | `r.GuestRegistrations` | Compact list inside broker's card |

**Cancelled card styling:**
```html
<div class="card h-100 @(registration.IsCancelled ? "opacity-50" : "")">
    @if (registration.IsCancelled)
    {
        <span class="badge bg-secondary">Storniert</span>
    }
    ...
</div>
```

Bootstrap 5 `opacity-50` utility is available in Bootstrap 5.1+. The project uses Bootstrap 5 (confirmed from `app.css` and existing badge usage). [HIGH confidence — visible in App.razor link: `bootstrap/bootstrap.min.css`]

### Pattern 4: Card Navigation (Informational Only)

**What:** The entire card is a clickable link to `/portal/events/{eventId}`, no action buttons.

**Example:**
```html
<div class="card-footer bg-transparent">
    <a href="/portal/events/@registration.EventId" class="btn btn-outline-primary btn-sm w-100">
        Zur Veranstaltung
    </a>
</div>
```

This matches the `EventCard.razor` pattern exactly (card-footer with a single CTA link).

### Pattern 5: RegistrationList Page Component Structure

**What:** Standard Makler portal page structure with spinner loading, empty state, and data grid.

```csharp
@page "/portal/registrations"
@attribute [Authorize(Roles = "Makler")]
@layout MainLayout
@inject RegistrationService RegistrationService
@inject AuthenticationStateProvider AuthStateProvider
@inject NavigationManager Navigation

// Loading: spinner-border (existing pattern from EventList.razor)
// Empty state: alert-info + link to /portal/events (per locked decision)
// Loaded: row of cards (row-cols-1 row-cols-md-2 row-cols-lg-3 g-4)
```

### Anti-Patterns to Avoid

- **Redirecting in OnAfterRenderAsync:** Always redirect in `OnInitializedAsync` to avoid flash of unwanted content. The existing `Login.razor` correctly uses `OnInitializedAsync`.
- **Using `forceLoad: true` for internal routes:** `forceLoad: false` (default) is sufficient for Blazor-internal navigation and avoids a full page reload.
- **Querying SelectedOptions without Include:** `Registration.SelectedOptions` is a many-to-many nav property. Without `.Include(r => r.SelectedOptions)`, the collection will be empty (lazy loading is not configured in this project).
- **Separate cards for guests:** CONTEXT.md is explicit — guests render inline within the broker's card, not as separate top-level cards.
- **Showing guests from cancelled broker registrations differently:** The data query should include cancelled broker registrations; guests under them should still display inline. Only filter by `RegistrationType.Makler` and `ParentRegistrationId == null`.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Role check | Custom role extraction logic | `user.IsInRole("Admin")` / `ClaimsPrincipal.IsInRole` | Roles are already mapped to `ClaimTypes.Role` in Program.cs `OnTokenValidated` |
| Date formatting | Custom CET formatter | `TimeZoneHelper.FormatDateTimeCet()` | Already handles DST, uses TZConvert, used in all portal pages |
| Total cost calculation | New calculator class | Inline LINQ sum in component | `r.RegistrationAgendaItems.Sum(rai => rai.AgendaItem.CostForMakler)` is the established pattern |
| Bootstrap spinner | Custom loading indicator | Bootstrap `spinner-border` pattern | Used verbatim in EventList.razor, EventDetail.razor |

**Key insight:** This phase adds no new domain concepts or libraries — it is pure UI wiring of existing data and navigation patterns already established in Phases 3–8.

---

## Common Pitfalls

### Pitfall 1: Home.razor Rendering Flash

**What goes wrong:** Home.razor briefly renders its template (the old "Hello, world!" content or a partial render) before `OnInitializedAsync` fires, causing a visible flash.
**Why it happens:** Blazor Server pre-renders the component synchronously before the async method completes.
**How to avoid:** Keep the template empty or show only a minimal spinner. The `@code` block handles all navigation; the template needs no content for the redirect cases.
**Warning signs:** Visible flicker of old Home content before redirect.

```razor
@* Show nothing during redirect; show message only if authenticated with no role *@
@if (showAccessDenied)
{
    <div class="alert alert-warning mt-4">
        <h4>Zugriff verweigert</h4>
        <p>Ihr Konto hat keine bekannte Rolle. Bitte wenden Sie sich an Ihren Administrator.</p>
    </div>
}
```

### Pitfall 2: N+1 Query in Registrations Page

**What goes wrong:** Loading registrations without Include chains causes N+1 queries — one query per registration to load Event, AgendaItems, Options.
**Why it happens:** EF Core does not lazy-load by default in this project (no `UseLazyLoadingProxies`).
**How to avoid:** Single `GetBrokerRegistrationsAsync` call with all necessary `.Include().ThenInclude()` chains.
**Warning signs:** Slow page load, many SQL queries in dev logs.

### Pitfall 3: EF Core String Comparison for Email

**What goes wrong:** `.Where(r => r.Email == brokerEmail)` may miss case differences in SQL Server depending on collation.
**Why it happens:** SQL Server collation is typically case-insensitive, but the C# side can be case-sensitive.
**How to avoid:** Use `.Where(r => r.Email.ToLower() == brokerEmail.ToLower())` or the existing project pattern of `string.Equals(..., StringComparison.OrdinalIgnoreCase)` for in-memory operations. The existing `RegistrationService` methods use `.Equals(userEmail, StringComparison.OrdinalIgnoreCase)` for in-memory checks; the EF query can use `r.Email == brokerEmail` since SQL Server collation handles case-insensitivity server-side.
**Warning signs:** Registrations not showing for users with mixed-case email addresses.

### Pitfall 4: Cancelled Guest Registrations Under Cancelled Broker

**What goes wrong:** Showing only `!r.IsCancelled` guests hides guests whose broker cancelled, but those guests may still be active individually.
**Why it happens:** Phase 7 decision: "cancelling broker does NOT cascade to guest registrations."
**How to avoid:** The query in `GetBrokerRegistrationsAsync` should load `GuestRegistrations` without filtering on `IsCancelled` — let the UI render the state. The broker's card already shows the "Storniert" badge; guests inside can be shown as-is.

### Pitfall 5: Guest Display Count vs Total

**What goes wrong:** Including cancelled guests in inline display under a cancelled broker card creates visual confusion.
**Why it happens:** No display design spec for this edge case.
**How to avoid:** Show all guests inline (cancelled guests with their own opacity/badge); the primary state signal is the broker registration status at the top of the card. Simple, consistent rendering beats complex conditional display.

### Pitfall 6: `NavigateTo` in SSR Pre-render Phase

**What goes wrong:** `NavigateTo` may throw or behave unexpectedly if called during static SSR pre-rendering before the SignalR circuit is established.
**Why it happens:** Blazor Server with interactive render mode — the first render pass is sometimes static.
**How to avoid:** The existing `Login.razor` uses `OnInitializedAsync` for `NavigateTo` with `forceLoad: true`. For `Home.razor`, since it uses `@rendermode InteractiveServer` (implicit from `AddInteractiveServerComponents()`), this is fine. Check that the page doesn't need `@rendermode` attribute explicitly — existing pages don't use it, so the global default applies.

---

## Code Examples

Verified patterns from existing codebase:

### Role Check Pattern (from Program.cs OnTokenValidated)
```csharp
// Source: EventCenter.Web/Program.cs
// Roles are mapped as ClaimTypes.Role in OnTokenValidated event
// Therefore user.IsInRole("Admin") works correctly
if (user.IsInRole("Admin"))
{
    Navigation.NavigateTo("/admin/events", forceLoad: false);
    return;
}
if (user.IsInRole("Makler"))
{
    Navigation.NavigateTo("/portal/events", forceLoad: false);
    return;
}
```

### Loading Spinner Pattern (from EventList.razor)
```razor
<!-- Source: EventCenter.Web/Components/Pages/Portal/Events/EventList.razor -->
@if (isLoading)
{
    <div class="text-center my-5">
        <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Lade...</span>
        </div>
        <p class="mt-2">Lade...</p>
    </div>
}
```

### Card Grid Pattern (from EventList.razor)
```razor
<!-- Source: EventCenter.Web/Components/Pages/Portal/Events/EventList.razor -->
<div class="row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4 mb-4">
    @foreach (var item in items)
    {
        <div class="col">
            <RegistrationCard Registration="@item" />
        </div>
    }
</div>
```

### EventCard Structure (from EventCard.razor — match this pattern)
```razor
<!-- Source: EventCenter.Web/Components/Shared/EventCard.razor -->
<div class="card h-100">
    <div class="card-body d-flex flex-column">
        <h5 class="card-title">@title</h5>
        <div class="text-muted small mb-2">
            <span class="me-3"><i class="bi bi-calendar"></i> date</span>
            <span><i class="bi bi-geo-alt"></i> location</span>
        </div>
        <p class="card-text flex-grow-1">content</p>
        <div class="mb-2">badge(es)</div>
    </div>
    <div class="card-footer bg-transparent">
        <a href="..." class="btn btn-primary btn-sm w-100">CTA</a>
    </div>
</div>
```

### Cost Calculation Pattern (from EventDetail.razor)
```csharp
// Source: EventCenter.Web/Components/Pages/Portal/Events/EventDetail.razor
var guestCost = guest.RegistrationAgendaItems.Sum(rai => rai.AgendaItem.CostForGuest);
// For broker registration total:
var totalCost = registration.RegistrationAgendaItems.Sum(rai => rai.AgendaItem.CostForMakler);
// For selected options:
var optionsCost = registration.SelectedOptions.Sum(o => o.Price);
```

### Location vs Webinar Label (from EventCard.razor)
```razor
<!-- Source: EventCenter.Web/Components/Shared/EventCard.razor -->
@if (Event.EventType == EventType.Webinar)
{
    <!-- Show "Webinar" label -->
    <span class="badge bg-info"><i class="bi bi-camera-video"></i> Webinar</span>
}
else
{
    <!-- Show location -->
    <span><i class="bi bi-geo-alt"></i> @Event.Location</span>
}
```

### Auth State in Component (from EventDetail.razor)
```csharp
// Source: EventCenter.Web/Components/Pages/Portal/Events/EventDetail.razor
var authState = await AuthStateProvider.GetAuthenticationStateAsync();
var user = authState.User;
userEmail = user.FindFirst("preferred_username")?.Value
    ?? user.FindFirst("email")?.Value
    ?? string.Empty;
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Static Home page | Role-based redirect | Phase 9 | Brokers and admins land directly on relevant list |
| No registrations overview | `/portal/registrations` card list | Phase 9 | Brokers can see all their bookings at a glance |

**Deprecated/outdated:**
- `Home.razor` "Hello, world!" content: replaced entirely with redirect logic in Phase 9.

---

## Open Questions

1. **SelectedOptions eager loading depth**
   - What we know: `Registration.SelectedOptions` is a `ICollection<EventOption>` — many-to-many relationship tracked by EF Core. No explicit join entity is visible in the codebase; EF Core manages the join table implicitly.
   - What's unclear: Whether the EF Core DbContext has the many-to-many configured with auto-join table or explicit join entity. If explicit join entity exists, Include syntax may differ.
   - Recommendation: Check `EventCenterDbContext` configuration or inspect the migration before writing the Include chain. Use `.Include(r => r.SelectedOptions)` first; if empty in tests, inspect the DB context for alternative configuration.

2. **Total cost including options**
   - What we know: `EventOption.Price` is a `decimal`. Options are in `Registration.SelectedOptions`. No "quantity" field on the join — it appears to be a simple selected/not-selected model.
   - What's unclear: Whether a broker can select the same option multiple times (quantity > 1). No quantity field is visible on `EventOption` or the join.
   - Recommendation: Assume one-option-per-selection; total = `r.SelectedOptions.Sum(o => o.Price)`. If quantities exist, they are not modeled in the current entities.

3. **Guest cancellation display within cancelled broker card**
   - What we know: Phase 7 decision says broker cancel does not cascade to guests. The context says "cancelled registrations visible with Storniert badge".
   - What's unclear: Whether "cancelled registrations" means only the broker card or also includes individually cancelled guest sub-rows.
   - Recommendation: Render all guest rows (cancelled and active) within the broker card. Use a small "Storniert" badge on cancelled guest rows too. Keep it consistent: every registration — broker or guest — shows its own cancel state.

---

## Sources

### Primary (HIGH confidence)
- Codebase: `EventCenter.Web/Components/Pages/Home.razor` — current state (trivial Hello World, needs replacement)
- Codebase: `EventCenter.Web/Components/Pages/Auth/Login.razor` — NavigateTo redirect pattern
- Codebase: `EventCenter.Web/Components/Pages/Portal/Events/EventList.razor` — spinner + card grid patterns
- Codebase: `EventCenter.Web/Components/Shared/EventCard.razor` — card structure to match
- Codebase: `EventCenter.Web/Services/RegistrationService.cs` — existing service method patterns
- Codebase: `EventCenter.Web/Domain/Entities/Registration.cs` — entity structure confirmed
- Codebase: `EventCenter.Web/Domain/Entities/EventOption.cs` — SelectedOptions entity
- Codebase: `EventCenter.Web/Program.cs` — role mapping (ClaimTypes.Role), auth config

### Secondary (MEDIUM confidence)
- Blazor Server `NavigationManager.NavigateTo` behavior during pre-render — based on established codebase patterns + .NET 8 Blazor Server docs knowledge

### Tertiary (LOW confidence)
- N/A — all findings verified from project codebase directly

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new libraries, all confirmed from existing codebase
- Architecture: HIGH — patterns directly copied/adapted from existing Pages and Services
- Pitfalls: HIGH — derived from actual code analysis plus established EF Core/Blazor Server behavior
- Open questions: MEDIUM — edge cases that require 5-minute code verification before implementation

**Research date:** 2026-03-01
**Valid until:** 2026-04-01 (stable Blazor Server / .NET 8 patterns; no fast-moving dependencies)
