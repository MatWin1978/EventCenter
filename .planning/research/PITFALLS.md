# Pitfalls Research

**Domain:** Event Management System (Veranstaltungscenter)
**Researched:** 2026-02-26
**Confidence:** HIGH

## Critical Pitfalls

### Pitfall 1: Circuit-Based Authentication State Becoming Stale

**What goes wrong:**
In Blazor Server, authentication happens once at circuit creation and persists for the circuit lifetime. When an admin revokes a user's access or changes their roles, the user maintains their old authentication state until they refresh their browser. This allows unauthorized users to continue registering for events, viewing restricted content, or performing actions they should no longer have access to.

**Why it happens:**
Blazor Server maintains a stateful SignalR connection (circuit) where authentication is captured at connection time. Unlike traditional request/response models where auth is checked per request, Blazor Server caches the authentication state in memory. The framework assumes authentication is "fixed" for the circuit lifetime by default.

**How to avoid:**
- Implement `IdentityRevalidatingAuthenticationStateProvider` that periodically revalidates the security stamp (every 30 minutes is standard)
- For highly sensitive operations (like company invitation management), explicitly re-check authorization policies at the service layer, not just UI level
- Add a "Force Logout" admin feature that can terminate circuits for specific users
- Never rely solely on `[Authorize]` attributes for authorization—combine with runtime policy checks in business logic

**Warning signs:**
- Users reporting they can still access features after admin says they removed permissions
- Role changes requiring browser refresh to take effect
- Anonymous company representatives accessing events after invitations are revoked

**Phase to address:**
Phase 1 (Authentication & Authorization Infrastructure) - Must implement revalidation from the start, as retrofitting is complex.

---

### Pitfall 2: Race Conditions in Concurrent Event Registration

**What goes wrong:**
Multiple users simultaneously registering for an event with limited seats can cause overbooking. When two users both see "1 seat remaining" and submit registrations at the same time, both requests may succeed, exceeding the `ParticipantsLimit`. Same issue applies to company bookings with `NumberMaxParticipants` limits on agenda items.

**Why it happens:**
Without proper concurrency control, the sequence is:
1. User A reads: "50 registrations, limit is 51 → seat available"
2. User B reads: "50 registrations, limit is 51 → seat available"
3. User A writes: registration #51
4. User B writes: registration #52 ← OVERBOOKING

Entity Framework's default behavior doesn't prevent this without explicit locking or optimistic concurrency tokens.

**How to avoid:**
- Use **optimistic locking** with EF Core's `[ConcurrencyCheck]` or `[Timestamp]` on `RegistrationCount` field
- Add database constraint: `CHECK (RegistrationCount <= ParticipantsLimit)` as final safety net
- For high-traffic events, implement distributed locking (Redis) around registration logic
- Return meaningful error message: "Diese Veranstaltung ist leider ausgebucht. Der letzte Platz wurde gerade vergeben."
- Consider reservation pattern: reserve seat for 5 minutes during form completion, then confirm or release

**Warning signs:**
- `RegistrationCount` exceeding `ParticipantsLimit` in database queries
- User complaints about seeing "available seats" but getting rejection after submitting
- Data integrity violations on registration inserts during high-traffic periods

**Phase to address:**
Phase 2 (Event Registration Core) - Critical for registration MVP. Must implement before first production event.

---

### Pitfall 3: Timezone Handling for Registration Deadlines

**What goes wrong:**
`RegistrationDeadline` stored as `DateTime` (instead of `DateTimeOffset`) causes ambiguity. A deadline of "2026-03-15" means different moments for users in different timezones. When the server (in CET) closes registration at midnight, a user in EST sees the deadline pass 6 hours early. Even worse: if the system migrates to cloud hosting in a different timezone, all deadline logic breaks.

**Why it happens:**
The domain model uses `DateTime` for all date fields (`Start`, `End`, `RegistrationDeadline`). Without timezone context, the system makes implicit assumptions (server local time? UTC? user local time?). User Story US-03 states "Anmeldefrist + 1 Tag < jetzt" but doesn't specify which timezone's "jetzt".

**How to avoid:**
- Store all timestamps as UTC in database (use `DateTime.UtcNow`, not `DateTime.Now`)
- Display deadlines in user's local timezone: "Anmeldefrist: 15.03.2026 23:59 CET"
- Business rule for deadline: use **inclusive** end-of-day logic: `RegistrationDeadline.AddDays(1).Date > DateTime.UtcNow.Date`
- Add timezone selector for event creation: "Diese Veranstaltung findet in Zeitzone [CET ▼] statt"
- Document timezone assumptions prominently in admin UI: "Alle Zeiten werden als Mitteleuropäische Zeit (CET/CEST) gespeichert"
- Use `DateTimeOffset` for new timestamp fields to avoid future issues

**Warning signs:**
- Users reporting deadlines passing "early"
- Registration rejections with error message "Deadline reached" when deadline date hasn't arrived
- Timestamp fields stored without timezone context (looking at database shows inconsistent values)
- Confusion during daylight saving time transitions

**Phase to address:**
Phase 1 (Core Domain Model) - Fix before any production data is stored, as migration is painful.

---

### Pitfall 4: Anonymous Company Access via Predictable GUIDs

**What goes wrong:**
Company invitations use GUID-based links for anonymous access (`EventCompany` with a `BusinessId` GUID). If GUIDs are generated predictably or sequentially (e.g., using `Guid.NewGuid()` in a tight loop with poor randomness), attackers can enumerate valid links and access/modify company registrations they weren't invited to. This exposes participant data and allows unauthorized bookings.

**Why it happens:**
Developers assume GUIDs are "secure enough" without validating randomness quality. Sequential GUIDs (common in SQL Server `NEWSEQUENTIALID()`) are even worse. There's no rate limiting on GUID endpoint attempts, no expiration, and no IP-based access restrictions.

**How to avoid:**
- Use cryptographically strong GUID generation: `Guid.NewGuid()` is acceptable, but consider `System.Security.Cryptography.RandomNumberGenerator` for token generation
- Add GUID expiration: `EventCompany.InvitationExpiresAt` - reject access after deadline or event end
- Implement rate limiting on anonymous endpoints: max 10 failed GUID attempts per IP per hour
- Add "one-time use" flag: `EventCompany.InvitationUsed` - after first booking, require email verification for changes
- Log all anonymous access attempts with IP, timestamp, GUID (valid/invalid) for security monitoring
- Consider time-based tokens (JWT with expiration) instead of permanent GUIDs
- Never send GUID in query string if avoidable (appears in logs); use path parameter or POST body

**Warning signs:**
- Unusual patterns in anonymous access logs (sequential GUID attempts, same IP)
- Company representatives reporting they can see/edit other companies' registrations
- Registrations from companies that weren't invited
- GUID tokens appearing in public search engine results (if links were shared improperly)

**Phase to address:**
Phase 3 (Company Invitation & Anonymous Booking) - Critical security requirement before enabling company invitations.

---

### Pitfall 5: Memory Leaks from Event Handlers and Circuit State

**What goes wrong:**
Blazor Server components subscribe to events (`OnRegistrationChanged`, `OnEventUpdated`) but don't dispose properly. Each browser connection creates a circuit that stays in memory. If components don't implement `IDisposable` and unsubscribe from events, circuits never get garbage collected. After days of operation, server memory fills up with dead circuits, causing crashes or container restarts.

**Why it happens:**
Root causes:
- Components subscribe to notification services (e.g., `EventNotificationService.OnEventChanged += HandleUpdate`) without unsubscribing
- Singleton services hold references to scoped components via event delegates
- Background timers for "auto-refresh event list" keep running after component is destroyed
- `IJSRuntime` interop creates `DotNetObjectReference` without disposal

**How to avoid:**
**Mandatory disposal checklist:**
- Every component using events must implement `IAsyncDisposable` or `IDisposable`
- Unsubscribe in `Dispose()`: `service.OnEventChanged -= HandleUpdate;`
- For timers: `Timer.Dispose()` or `CancellationTokenSource.Cancel()` in Dispose
- For JS interop: call `DotNetObjectReference.Dispose()`
- Use scoped services for per-user state, never singletons storing circuit-specific data
- Avoid static fields/caches holding component references

**Example pattern:**
```csharp
@implements IAsyncDisposable

@code {
    protected override async Task OnInitializedAsync() {
        EventService.OnEventChanged += RefreshList;
    }

    public async ValueTask DisposeAsync() {
        EventService.OnEventChanged -= RefreshList;
        // Dispose timers, cancellation tokens, JS references
    }
}
```

**Warning signs:**
- Server process memory (`dotnet` working set) grows continuously without plateauing during idle periods
- GC Gen 2 heap size steadily increasing over hours/days
- Container/pod restarts due to OOM (Out Of Memory) errors
- Slow page loads as server struggles with memory pressure
- Monitoring shows "circuits" count increasing without corresponding user count

**Phase to address:**
Phase 1 (Blazor Infrastructure & Patterns) - Establish disposal patterns early. Phase 5+ (Real-time Updates) if implementing SignalR notifications.

---

### Pitfall 6: Insufficient Validation Leading to Data Corruption

**What goes wrong:**
Client-side validation in Blazor forms can be bypassed (browser dev tools, disabled JS). Without server-side validation, users can submit:
- Event end date before start date
- Negative participant limits
- Registration deadline after event end
- Agenda items with `MembersParticipationCost = -100€`
- Company invitations with `NumberMaxParticipants > Event.ParticipantsLimit`

This corrupts business logic, breaks sorting/filtering, and causes runtime exceptions when calculating costs.

**Why it happens:**
Over-reliance on Blazor's `<DataAnnotationsValidator>` component without corresponding validation in API/service layer. Domain models use basic data types (`int`, `decimal`, `DateTime`) without value object validation. No database constraints beyond NOT NULL.

**How to avoid:**
**Three-layer defense:**

1. **Domain model validation** (FluentValidation recommended):
```csharp
public class EventValidator : AbstractValidator<Event> {
    RuleFor(e => e.End).GreaterThan(e => e.Start)
        .WithMessage("Enddatum muss nach Startdatum liegen");
    RuleFor(e => e.RegistrationDeadline).LessThan(e => e.Start)
        .WithMessage("Anmeldefrist muss vor Veranstaltungsbeginn liegen");
    RuleFor(e => e.ParticipantsLimit).GreaterThan(0);
}
```

2. **Service layer enforcement** (never trust input):
```csharp
public async Task<Result> CreateEvent(Event evt) {
    var validationResult = await _validator.ValidateAsync(evt);
    if (!validationResult.IsValid) return Result.Fail(validationResult.Errors);
    // ... persist
}
```

3. **Database constraints** (last line of defense):
```sql
ALTER TABLE tblEvents ADD CONSTRAINT CK_Event_Dates
    CHECK ([End] > [Start]);
ALTER TABLE tblEvents ADD CONSTRAINT CK_Event_ParticipantsLimit
    CHECK (ParticipantsLimit > 0);
```

**Warning signs:**
- Runtime exceptions with "Start date cannot be after End date" in logs
- Events appearing in reverse chronological order due to invalid dates
- Negative amounts in cost calculations
- Database queries failing with "Arithmetic overflow" on decimal operations

**Phase to address:**
Phase 1 (Domain Model & Validation) - Non-negotiable foundation. Implement before any CRUD operations.

---

### Pitfall 7: Prerendering Breaking Authentication and State

**What goes wrong:**
Blazor Server's prerendering causes components to render twice: once on server (static HTML), then again when circuit connects. During prerender, `AuthenticationStateProvider` may return "not authenticated" even for logged-in users. Components fetching user-specific data (e.g., "my registrations") show empty state, then flash with real data after hydration. This creates poor UX and potential security issues if sensitive data briefly displays before auth check completes.

**Why it happens:**
Prerendering happens before SignalR circuit establishes. HttpContext exists during prerender but circuit-scoped services don't. Components using `[CascadingParameter] Task<AuthenticationState>` get incomplete state during first render. JS interop (`IJSRuntime`) is unavailable during prerender, causing exceptions.

**How to avoid:**
- **Option 1: Disable prerendering** for authenticated pages:
```csharp
<component type="typeof(EventList)" render-mode="Server" />
```
Drawback: slower initial load, no SEO benefit.

- **Option 2: Handle double-render gracefully** (recommended for public pages):
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
        await LoadUserData(); // Only load after circuit is established
        StateHasChanged();
    }
}
```

- **Option 3: Use PersistentComponentState** (ASP.NET Core 8+):
```csharp
[PersistentState] private List<Event>? Events { get; set; }
```
Serializes data during prerender, rehydrates on client without refetching.

- **Never access IJSRuntime in OnInitialized** - use OnAfterRenderAsync with firstRender check
- Show loading skeleton during prerender instead of "not authenticated" message

**Warning signs:**
- "Cannot access JSRuntime before circuit is initialized" exceptions in logs
- Users seeing flash of "please log in" message on authenticated pages
- API called twice for same data (once during prerender, once during hydration)
- CSS layout shift (CLS) as content pops in after hydration

**Phase to address:**
Phase 1 (Blazor Fundamentals) - Set rendering strategy early. Phase 2+ (UI implementation) must respect prerender limitations.

---

### Pitfall 8: Email Deliverability Failures for Invitations

**What goes wrong:**
Event invitation emails (`EventCompany.SentOn` tracking suggests email feature) fail to reach recipients. Emails bounce, land in spam, or are rejected due to:
- Missing SPF/DKIM/DMARC authentication
- High bounce rate flagging sender IP as spam
- Generic "noreply@" sender addresses users can't reply to
- Email content triggers spam filters (too many links, suspicious keywords)

Company representatives never receive GUID invitation links, never book, and admins have no visibility into delivery failures.

**Why it happens:**
Email deliverability is treated as "just configure SMTP and send." Authentication setup (SPF, DKIM, DMARC) is overlooked. No bounce handling, no delivery status tracking, no retry logic. Emails sent synchronously from web requests cause timeout issues.

**How to avoid:**
**Authentication (mandatory for 2026):**
- SPF record authorizing sending IP
- DKIM signature on all emails
- DMARC policy (start with `p=none`, move to `p=quarantine` after monitoring)

**Deliverability best practices:**
- Use transactional email service (SendGrid, Postmark, AWS SES) with built-in authentication
- Implement double opt-in for company invitations: send confirmation link before GUID access link
- Track delivery status: delivered, bounced (soft/hard), opened, clicked
- Maintain bounce rate below 0.3% (2026 standard, stricter than legacy 2%)
- Use real sender address: `events@[company].de`, not `noreply@`
- Implement retry logic with exponential backoff for soft bounces
- Send test emails to Mail-Tester.com before going live

**Email content:**
- Personalize: "Hallo [CompanyName]" instead of generic greeting
- Plain text alternative alongside HTML
- Minimal links (GUID link + support contact only)
- Clear unsubscribe option (even for transactional emails, for user trust)

**Warning signs:**
- `EventCompany.SentOn` is set but company reports never receiving email
- Bounce rate above 2% (check email service dashboard)
- Gmail/Outlook marking emails as spam (test with seed addresses)
- No tracking of delivery failures in application logs

**Phase to address:**
Phase 3 (Company Invitations) - Email infrastructure must be production-ready before first company invitation is sent.

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Storing `DateTime` as server local time instead of UTC | No timezone conversion complexity | Breaks during DST transitions, server migration, multi-region deployments | **Never acceptable** - always use UTC |
| Using `[Authorize]` attribute only, skipping service-layer auth checks | Faster development, less code | Security bypass via direct service calls, API endpoints | MVP only if no API endpoints exist; refactor in Phase 2 |
| Singleton services holding per-user state | Easy shared state across components | Memory leaks, data leakage between users, circuit bloat | **Never acceptable** - use scoped services |
| Skip optimistic locking on registration count | Simpler code, no concurrency handling | Overbooking, data corruption, angry users | Only if event capacity > 1000 and registration rate < 1/min |
| Client-side validation only (no server-side) | Faster development, better UX | Data corruption, security vulnerabilities, broken business rules | **Never acceptable** - always validate server-side |
| Sending emails synchronously in HTTP request | Simple code, no queue infrastructure | Timeout errors, poor UX (slow page loads), scalability issues | MVP only; must refactor to background jobs before 100+ events |
| Hardcoded German strings instead of resource files | No localization overhead | Cannot expand to other languages/regions | Acceptable if scope is explicitly "German-only forever" (verify with stakeholders) |
| No audit logging for admin actions | Less database writes, simpler schema | No compliance trail, impossible to debug "who deleted this event?" | Never acceptable for production; add in Phase 1 |

---

## Integration Gotchas

Common mistakes when connecting to external services.

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Keycloak/OIDC | Trusting JWT tokens without signature verification | Always validate signature, issuer, audience; use built-in middleware validation |
| Keycloak/OIDC | Not handling token refresh (access tokens expire after 5-15 min) | Implement refresh token flow; redirect to login if refresh fails |
| Keycloak/OIDC | Storing roles/claims in app database (duplicating identity data) | Use claims from JWT; only store user reference ID (`sub` claim) |
| SQL Server | Using `DbContext` as singleton | Register DbContext as **scoped** per circuit; never singleton (causes "already closed reader" errors) |
| SQL Server | Assuming transactions are auto-committed | Explicitly use `TransactionScope` or `DbContext.Database.BeginTransaction()` for multi-step operations |
| Email service | No retry logic for transient failures | Implement exponential backoff: 1min, 5min, 15min, give up |
| Email service | Sending emails in synchronous request pipeline | Use background job queue (Hangfire, MassTransit) to send asynchronously |
| File upload (Media Picker) | Storing file paths in DB without validation | Validate file exists at path before rendering; handle missing files gracefully |
| iCalendar export | Generating `.ics` with wrong timezone format | Use `VTIMEZONE` component; test with Outlook, Google Calendar, Apple Calendar |

---

## Performance Traps

Patterns that work at small scale but fail as usage grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Loading all events in `OnInitializedAsync` without pagination | "Event list slow to load" complaints | Implement virtual scrolling or pagination (20 events per page); load on-demand | >200 events or >50 concurrent users |
| N+1 query loading agenda items for event list | Page loads slow with multiple DB round-trips | Use `.Include(e => e.AgendaItems)` eager loading or projection (`.Select(e => new {...})`) | >50 events with >5 agenda items each |
| Executing `COUNT(*)` on registrations table for every event card | List rendering takes >2 seconds | Materialize registration count in `Event.RegistrationCount` field; update on insert/delete | >1000 registrations total |
| Real-time updates broadcasting to all connected users | Server CPU spikes during event updates | Use SignalR groups: only broadcast to users viewing specific event | >100 concurrent users |
| Fetching full `Event` entity when only title/date needed | High memory usage, slow serialization | Use DTO projection: `events.Select(e => new EventListItem { Title = e.Title, ... })` | >500 events in database |
| No caching of published events list | Database hit on every page load | Cache published events for 5 minutes (invalidate on publish/unpublish) | >200 requests/minute |
| Rendering large HTML descriptions without sanitization/truncation | DOM bloat, slow rendering, XSS risk | Sanitize with HtmlSanitizer library; truncate to 500 chars in list view | Descriptions >10KB HTML |

---

## Security Mistakes

Domain-specific security issues beyond general web security.

| Mistake | Risk | Prevention |
|---------|------|------------|
| Not validating `eventId` parameter ownership before edit | User A can edit/delete User B's events via direct API calls | Check `Event.CreatedBy == currentUser` or use authorization handler checking ownership |
| Allowing member to register other members without consent | Privacy violation; user registers friend without permission | Only allow self-registration (`MemberEventRegistration.MemberId == currentUser.Id`) |
| Exposing internal IDs in company invitation URLs | Predictable URLs; enumeration attacks | Use GUID for `EventCompany.BusinessId`, never expose `EventCompany.Id` |
| No rate limiting on registration endpoints | DoS attack; attacker registers 1000 times, blocking real users | Implement rate limit: max 5 registrations per user per hour, 100 per IP per hour |
| Trusting `ResponsibleMemberId` from client form | User A can create guest registration claiming User B is responsible | Set `ResponsibleMemberId` from authenticated user's claims, never from form input |
| Storing GUID tokens in application logs | Token leakage in log files/monitoring systems | Mask GUIDs in logs: log `...Guid={guid[0..8]}***` instead of full GUID |
| No CSRF protection on state-changing operations | Cross-site request forgery; attacker tricks user into unwanted registration | Use antiforgery tokens (built into Blazor forms); verify on all POST/PUT/DELETE |
| Allowing company representative to modify registration after event started | Data tampering; changing participant names post-event for fraud | Block modifications when `Event.Start < DateTime.UtcNow` |

---

## UX Pitfalls

Common user experience mistakes in this domain.

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| No loading indicator during registration submit | Users click "Submit" multiple times, creating duplicate registrations | Show spinner, disable button, display "Ihre Anmeldung wird verarbeitet..." |
| Registration form doesn't preserve data on validation error | User fills 15 fields, one error, all data lost | Use Blazor's EditForm with model binding; preserve state on validation failure |
| Generic error: "Registration failed" | User doesn't know if event is full, deadline passed, or server error | Specific messages: "Anmeldefrist abgelaufen" vs "Alle Plätze belegt" vs "Serverfehler, bitte erneut versuchen" |
| Event deadline shows only date, no time | "Deadline is March 15" - can I register at 11:59 PM or midnight? | Display full datetime: "Anmeldefrist: 15.03.2026, 23:59 Uhr (CET)" |
| No confirmation before cancellation | User accidentally clicks "Stornieren", registration deleted, no undo | Add confirmation dialog: "Möchten Sie Ihre Anmeldung wirklich stornieren? Diese Aktion kann nicht rückgängig gemacht werden." |
| Company invitation email lacks context | Generic "You're invited" - to what? when? where? | Include event title, date, location in email subject and body |
| iCalendar export missing reminder | User adds to calendar but forgets event | Add VALARM component: remind 1 day before event |
| No visual distinction between registered/full/past events | User wastes time clicking on unavailable events | Use badges: "Angemeldet ✓", "Ausgebucht", "Abgelaufen" with color coding |
| Registration success but no next steps | "Registration successful" - now what? Will I get email? Calendar? | Show confirmation page: "Bestätigungs-E-Mail gesendet an [email]. Termin zum Kalender hinzufügen [iCal-Link]." |

---

## "Looks Done But Isn't" Checklist

Things that appear complete but are missing critical pieces.

- [ ] **Event registration:** Often missing concurrency handling — verify optimistic locking is enabled and tested with concurrent requests
- [ ] **Authentication:** Often missing circuit revalidation — verify `IdentityRevalidatingAuthenticationStateProvider` is registered and runs periodically
- [ ] **Date/time fields:** Often missing timezone context — verify all timestamps stored as UTC, displayed in CET with timezone label
- [ ] **Company invitations:** Often missing GUID expiration — verify invitation links expire after deadline or event end
- [ ] **Email sending:** Often missing deliverability monitoring — verify SPF/DKIM/DMARC configured, bounce rate tracked below 0.3%
- [ ] **Admin actions:** Often missing audit trail — verify every create/update/delete logs user, timestamp, and changed fields
- [ ] **Authorization checks:** Often missing service-layer enforcement — verify auth policies checked in services, not just `[Authorize]` attributes
- [ ] **Component lifecycle:** Often missing disposal — verify every component with event subscriptions implements `IAsyncDisposable`
- [ ] **Validation:** Often missing server-side validation — verify business rules enforced in service layer, not just client-side
- [ ] **Error handling:** Often missing user-friendly messages — verify exceptions caught and translated to German business-friendly text
- [ ] **Registration cancellation:** Often missing business rule enforcement — verify stornieren only allowed before deadline, only by original creator
- [ ] **Participant limits:** Often missing enforcement — verify database constraints + application logic prevent exceeding `ParticipantsLimit`
- [ ] **Form state:** Often missing preservation on errors — verify EditModel retains user input when validation fails
- [ ] **iCalendar export:** Often missing timezone/reminders — verify VTIMEZONE component and VALARM present in `.ics` file

---

## Recovery Strategies

When pitfalls occur despite prevention, how to recover.

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Overbooking due to race condition | MEDIUM | 1. Contact affected users via email to explain; 2. Increase `ParticipantsLimit` if venue allows; 3. Offer alternative event slot; 4. Implement optimistic locking immediately |
| Memory leak causing production crash | HIGH | 1. Restart app pool/container; 2. Capture memory dump for analysis; 3. Review components for missing `Dispose()`; 4. Deploy hotfix; 5. Monitor memory metrics closely |
| Timezone bug causing wrong deadline | MEDIUM | 1. Manual database update to correct deadline; 2. Notify affected users who were incorrectly blocked; 3. Extend deadline by X hours to compensate; 4. Deploy timezone fix |
| GUID enumeration exposing company data | HIGH | 1. Rotate all GUID tokens immediately; 2. Audit logs for suspicious access patterns; 3. Notify affected companies; 4. Add rate limiting + expiration; 5. Security review |
| Email deliverability failure | MEDIUM | 1. Configure SPF/DKIM/DMARC immediately; 2. Resend failed invitations; 3. Contact ESP to remove IP from blocklist; 4. Implement bounce tracking |
| Prerendering breaking authenticated pages | LOW | 1. Disable prerendering for affected pages: `render-mode="Server"`; 2. Add loading skeleton during hydration; 3. Implement PersistentComponentState for performance |
| Validation bypass causing data corruption | HIGH | 1. Database query to identify corrupt records; 2. Manual data cleanup; 3. Add database constraints to prevent recurrence; 4. Implement service-layer validation; 5. Audit all endpoints |
| Stale authentication state | MEDIUM | 1. Force logout affected users (terminate circuits); 2. Implement revalidation provider; 3. Document manual refresh requirement temporarily; 4. Deploy fix |

---

## Pitfall-to-Phase Mapping

How roadmap phases should address these pitfalls.

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Circuit auth state staleness | Phase 1: Auth Infrastructure | Deploy test app, revoke user role, verify access denied within 30 min without browser refresh |
| Race conditions in registration | Phase 2: Event Registration | Load test with 50 concurrent users registering for event with 1 seat; verify only 1 succeeds |
| Timezone handling | Phase 1: Domain Model | Create event in CET, verify deadline displayed correctly for UTC+0 test user |
| Predictable GUID enumeration | Phase 3: Company Invitations | Attempt to access 1000 sequential GUIDs from same IP; verify rate limiting blocks after 10 attempts |
| Memory leaks from events | Phase 1: Blazor Patterns + Phase 5: Real-time Updates | Run load test for 4 hours with 100 users; verify memory plateaus, doesn't grow continuously |
| Validation bypass | Phase 1: Domain Model & Validation | Attempt to POST event with `End < Start` via API; verify rejection with clear error message |
| Prerendering issues | Phase 1: Blazor Fundamentals | Load authenticated page, check network tab for duplicate API calls; verify no JS errors on hydration |
| Email deliverability | Phase 3: Company Invitations | Send 100 test invitations, check Mail-Tester.com score >8/10, bounce rate <0.3% |
| N+1 query performance | Phase 2: Event List UI | Load event list page, check DB profiler; verify single query with JOIN, not N queries |
| Authorization bypass | Phase 2-4: All CRUD Operations | Attempt to edit Event.Id=123 as User B when created by User A; verify 403 Forbidden |
| No audit logging | Phase 1: Infrastructure | Delete event as admin, verify log entry with username, timestamp, event details |
| CSRF vulnerability | Phase 1: Blazor Security | Craft CSRF attack from external site; verify antiforgery token validation blocks request |

---

## Sources

### Blazor Server Issues
- [Common Mistakes in Blazor Development and How to Solve Them | Medium](https://medium.com/@yusufeminirki/common-mistakes-in-blazor-development-and-how-to-solve-them-55ded7e5d338)
- [10 Blazor Coding Mistakes I See in Real Projects – Chandradev's Blog](https://chandradev819.wordpress.com/2025/12/17/10-blazor-coding-mistakes-i-see-in-real-projects-and-how-to-avoid-them/)
- [Exception Handling in Blazor Server: Best Practices for Resilient Web Apps](https://embarkingonvoyage.com/blog/technologies/exception-handling-in-blazor-server-best-practices-for-resilient-web-apps/)
- [ASP.NET Core Blazor authentication and authorization | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0)

### Authentication & Circuit Management
- [Blazor Server On Connection Authentication · Issue #44820 · dotnet/aspnetcore](https://github.com/dotnet/aspnetcore/issues/44820)
- [Blazor authentication and authorization · GitHub Gist](https://gist.github.com/SteveSandersonMS/175a08dcdccb384a52ba760122cd2eda)
- [ASP.NET Core server-side and Blazor Web App additional security scenarios | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/additional-scenarios?view=aspnetcore-10.0)

### Memory Leaks & Performance
- [Blazor Server Memory Management: Stop Circuit Leaks](https://amarozka.dev/blazor-server-memory-management-circuit-leaks/)
- [Manage memory in deployed ASP.NET Core server-side Blazor apps | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/server/memory-management?view=aspnetcore-9.0)
- [Severe blazor server memory leak · Issue #43221 · dotnet/aspnetcore](https://github.com/dotnet/aspnetcore/issues/43221)

### Concurrency & Race Conditions
- [Building a Ticketing System: Concurrency, Locks, and Race Conditions | Medium](https://codefarm0.medium.com/building-a-ticketing-system-concurrency-locks-and-race-conditions-182e0932d962)
- [How to Solve Race Conditions in a Booking System | HackerNoon](https://hackernoon.com/how-to-solve-race-conditions-in-a-booking-system)
- [Solving Race Conditions With EF Core Optimistic Locking](https://www.milanjovanovic.tech/blog/solving-race-conditions-with-ef-core-optimistic-locking)
- [How Ticket Booking Systems Handle 50,000 People Fighting for One Seat](https://singhajit.com/ticket-booking-system-design/)

### Event Management Pitfalls
- [15 Critical Challenges In Event Registration - Swoogo](https://swoogo.events/blog/challenges-in-event-registration/)
- [12 Event Planning Problems and Solutions (and How to Implement Them in 2026)](https://www.eventtia.com/en/6-common-event-management-mistakes-and-what-to-do-instead/)
- [Common Event Registration Problems and Their Solutions | ClearEvent](https://clearevent.com/blog/event-management-blog/common-event-registration-problems-and-their-solutions/)
- [Top Event App Problems in 2026 & How to Fix Them Fast](https://www.grupio.com/blog/event-app-problems-2026/)

### Timezone Handling
- [How to Handle Date and Time Correctly to Avoid Timezone Bugs - DEV Community](https://dev.to/kcsujeet/how-to-handle-date-and-time-correctly-to-avoid-timezone-bugs-4o03)

### Security & Token Management
- [JWT Vulnerabilities List: 2026 Security Risks & Mitigation Guide - Red Sentry](https://redsentry.com/resources/blog/jwt-vulnerabilities-list-2026-security-risks-mitigation-guide)
- [Identity Security Predictions for 2026: What Threat Actors are Targeting Next](https://www.appgovscore.com/blog/identity-security-predictions-for-2026-threat-actor-targets)
- [The credential crisis: How trusted access became the biggest enterprise risk | SC Media](https://www.scworld.com/perspective/the-credential-crisis-how-trusted-access-became-the-biggest-enterprise-risk)

### Email Deliverability
- [Email Deliverability Best Practices 2026: The Operator Playbook for Inbox Placement](https://www.leadgen-economy.com/blog/deliverability-best-practices-2026/)
- [Acceptable Email Bounce Rate Standards in 2026: A Technical Guide for Marketers](https://www.mailmarketer.in/blog/2026/02/17/acceptable-email-bounce-rate-standards-in-2026-a-technical-guide-for-marketers/)
- [Cold Email Benchmark Report 2026: Reply Rates, Deliverability and Trends](https://instantly.ai/cold-email-benchmark-report-2026)
- [Email Bounce Rate: 5 Proven Ways to Reduce It in 2026](https://www.mailreach.co/blog/email-bounce-rate)

### Prerendering & State Hydration
- [Blazor Prerendering is Finally SOLVED in .NET 10!](https://dotnetwebacademy.substack.com/p/net-10-finally-fixes-prerendering)
- [Handle Pre-rendering Right in Blazor: Use Persistent State - devInstance LLC](https://devinstance.net/blog/handle-prerendering-right-in-blazor)
- [Understanding Blazor's Pre-rendering Behavior: Why Your Service Injection Might Fail](https://devinstance.net/blog/understanding-blazors-pre-rendering)
- [What I Learned: Blazor Auth with Server Side Pre-Rendering - Keith Wagner](https://kpwags.com/posts/2024/03/22/what-i-learned-blazor-auth-server-prerendering/)

---
*Pitfalls research for: Event Management System (Veranstaltungscenter) with Blazor Server*
*Researched: 2026-02-26*
