# Phase 5: Company Booking Portal - Research

**Researched:** 2026-02-27
**Domain:** Anonymous web portal, GUID-based authentication, dynamic form validation, real-time cost calculation
**Confidence:** HIGH

## Summary

Phase 5 implements an anonymous booking portal for company representatives accessed via GUID links sent in Phase 4 invitations. This phase requires handling anonymous access in Blazor Server (with known WebSocket challenges), implementing GUID expiration and rate limiting for security, building a dynamic participant entry form with live cost calculation, and supporting booking lifecycle management (submit, cancel, report non-participation) with admin email notifications.

The research identifies ASP.NET Core 8's built-in rate limiting middleware as the standard solution for GUID enumeration protection, constant-time comparison via `CryptographicOperations.FixedTimeEquals` for timing attack prevention, and Blazor Server's `@attribute [AllowAnonymous]` for anonymous page access (with WebSocket disconnection pitfalls documented). The existing codebase provides proven patterns: fire-and-forget email sending with MailKit, service layer pattern for business logic, FluentValidation with `RuleForEach` for dynamic list validation, and transaction-based data consistency.

**Primary recommendation:** Use ASP.NET Core rate limiting middleware with fixed window policy per GUID, implement 72-hour expiration on invitation codes (stored in ExpiresAtUtc), leverage existing FluentValidation patterns for dynamic participant list validation, build reactive cost summary with Blazor two-way binding (no SignalR needed for same-circuit updates), and extend IEmailSender interface for admin notification methods.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Single scrollable page — event info header, participant table, options, cost summary, submit button
- Compact event summary header at top: event title, date, location, company name (no full event details)
- Friendly error page for expired/invalid GUID links with message: "This link has expired or is invalid. Please contact [admin contact] for a new invitation."
- Simple success message after booking submission ("Buchung erfolgreich eingereicht") with email confirmation note — no detailed summary on the confirmation page
- Inline editable table rows — each row is one participant
- "Add participant" button adds a new row; edit/delete inline
- Required fields per participant: Anrede (salutation), Vorname, Nachname, E-Mail
- Per-participant agenda item selection — each participant has checkboxes for available agenda items (different people can attend different sessions)
- Table starts with one empty row pre-filled, ready for input
- Sticky/fixed cost summary that updates live as participants and options change
- Per-participant cost breakdown showing who is booked for what
- Show base price (Fixpreis), participant costs, and extra option costs as separate line items
- Grand total clearly displayed
- "Alle Preise zzgl. MwSt." note displayed near pricing
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

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| AUTH-03 | Unternehmensvertreter kann per GUID-Link ohne Login auf Firmenbuchung zugreifen | [AllowAnonymous] attribute pattern, rate limiting for enumeration prevention, GUID expiration via ExpiresAtUtc field |
| CBOK-01 | Unternehmensvertreter sieht Buchungsseite per GUID-Link | Anonymous Blazor page with GUID route parameter, EventCompany lookup by InvitationCode, include Event and AgendaItemPrices navigation properties |
| CBOK-02 | Unternehmensvertreter sieht firmenspezifische Preise und Agendapunkte | EventCompanyAgendaItemPrice join table with CustomPrice field, display pricing from company invitation |
| CBOK-03 | Unternehmensvertreter kann beliebig viele Teilnehmer eintragen | Dynamic list with FluentValidation RuleForEach, inline editable table pattern, add/remove row functionality |
| CBOK-04 | Unternehmensvertreter kann Zusatzoptionen auswählen | EventOption entity already exists, checkbox selection UI, many-to-many relationship via Registration.SelectedOptions |
| CBOK-05 | System berechnet Kosten automatisch (Fixpreis + Zusatzteilnehmer) | Reactive calculation pattern with Blazor two-way binding, computed property updates on participant/option changes |
| CBOK-06 | Unternehmensvertreter kann Buchung absenden und erhält Bestätigung | Service layer method for transaction-based booking creation, transition EventCompany.Status to Booked, create Registration entities, fire-and-forget confirmation email |
| CBOK-07 | Unternehmensvertreter kann Buchung stornieren | Update EventCompany.Status to Cancelled, add cancellation timestamp and comment, send admin notification via MAIL-05 |
| CBOK-08 | Unternehmensvertreter kann Nicht-Teilnahme melden | Store non-participation status (potentially via IsCancelled on Registration or new field), send admin notification |
| MAIL-04 | System sendet Benachrichtigung an Admin nach Firmenbuchung | Extend IEmailSender with SendAdminBookingNotificationAsync method, use fire-and-forget pattern from Phase 3/4 |
| MAIL-05 | System sendet Benachrichtigung an Admin nach Firmenstorno | Extend IEmailSender with SendAdminCancellationNotificationAsync method, include cancellation comment in email |

</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core | 8.0.* | Rate limiting middleware | Built-in since .NET 7, replaces third-party libraries like AspNetCoreRateLimit |
| Blazor Server | 8.0.* | Anonymous page rendering | Already project-wide, handles interactive UI with WebSocket circuit |
| FluentValidation | 11.* | Dynamic list validation | Already in project, RuleForEach pattern proven in Phase 4 for agenda item pricing |
| MailKit | 4.* | Email notifications to admin | Already in project, proven fire-and-forget pattern from Phase 3/4 |
| Entity Framework Core | 9.0.* | Transaction-based booking persistence | Already in project, optimistic locking pattern from Phase 3 |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Security.Cryptography | Built-in (.NET 8) | Constant-time GUID comparison | Prevent timing attacks on GUID validation |
| TimeZoneConverter | 6.* | CET timezone display | Already in project, consistent with Phase 1-4 patterns |
| Blazored.FluentValidation | 2.* | EditForm validation integration | Already in project, used in Phase 3 registration form |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Built-in rate limiting | AspNetCoreRateLimit (third-party) | Third-party library more feature-rich but unnecessary for simple fixed-window limiting |
| [AllowAnonymous] Blazor page | Razor Pages (.cshtml) | Razor Pages avoid WebSocket issues but lose Blazor interactivity benefits (live cost updates) |
| Reactive binding | SignalR hub for updates | SignalR unnecessary - Blazor Server already uses WebSocket circuit, same-page updates work natively |

**Installation:**
```bash
# All dependencies already installed in EventCenter.Web.csproj
# No new packages required
```

## Architecture Patterns

### Recommended Project Structure
```
EventCenter.Web/
├── Components/Pages/Company/        # New: Anonymous company portal pages
│   ├── CompanyBooking.razor         # Main booking page (GUID route)
│   └── BookingSuccess.razor         # Success confirmation page
├── Services/                        # Extend existing services
│   └── CompanyBookingService.cs     # New: Booking submission logic
├── Models/                          # DTOs for form validation
│   └── CompanyBookingFormModel.cs   # New: Participant list + options
├── Validators/                      # FluentValidation rules
│   └── CompanyBookingValidator.cs   # New: Dynamic participant validation
├── Infrastructure/Email/            # Extend email sender
│   └── MailKitEmailSender.cs        # Add admin notification methods
└── Middleware/                      # Rate limiting configuration
    └── (configure in Program.cs)    # No separate file needed
```

### Pattern 1: Anonymous Access with AllowAnonymous Attribute
**What:** Blazor Server pages can bypass authentication using `@attribute [AllowAnonymous]`
**When to use:** GUID-based access where no user login exists
**Example:**
```csharp
@page "/company/booking/{InvitationCode}"
@attribute [AllowAnonymous]
@inject CompanyBookingService BookingService

// CRITICAL: If app has global [Authorize] policy in _Imports.razor,
// this [AllowAnonymous] must be MORE SPECIFIC to override it
```
**Source:** [Microsoft Q&A - Allow Anonymous user on a Blazor page](https://learn.microsoft.com/en-us/answers/questions/248835/allow-anonymous-user-on-a-blazor-page)

**Known pitfall:** WebSocket disconnection can occur if authorization state conflicts with anonymous access. Mitigation: Ensure global authorization is set in `_Imports.razor` (not `_Host.cshtml`) so page-level `[AllowAnonymous]` can override it.

### Pattern 2: GUID Expiration and Validation
**What:** Time-based expiration with constant-time comparison
**When to use:** Prevent enumeration attacks and limit link validity
**Example:**
```csharp
// Service layer validation
public async Task<(bool IsValid, EventCompany? Company, string? ErrorMessage)>
    ValidateInvitationCodeAsync(string invitationCode)
{
    var invitation = await _context.EventCompanies
        .Include(ec => ec.Event)
        .Include(ec => ec.AgendaItemPrices)
            .ThenInclude(aip => aip.AgendaItem)
        .FirstOrDefaultAsync(ec => ec.InvitationCode != null &&
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(ec.InvitationCode),
                Encoding.UTF8.GetBytes(invitationCode)));

    if (invitation == null)
        return (false, null, "Ungültiger Einladungscode.");

    // Check expiration (if ExpiresAtUtc is set)
    if (invitation.ExpiresAtUtc.HasValue &&
        DateTime.UtcNow > invitation.ExpiresAtUtc.Value)
        return (false, null, "Dieser Link ist abgelaufen.");

    return (true, invitation, null);
}
```
**Source:** [FixedTimeEquals in .NET Core](https://vcsjones.dev/fixed-time-equals-dotnet-core/) - Built-in since .NET Core 2.1

**Security note:** Use `CryptographicOperations.FixedTimeEquals` for GUID comparison to prevent timing attacks where attackers measure response time to deduce valid characters.

### Pattern 3: Rate Limiting Middleware (Fixed Window)
**What:** ASP.NET Core 8 built-in rate limiting prevents brute-force GUID enumeration
**When to use:** Protect anonymous endpoints from abuse
**Example:**
```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("CompanyBooking", opt =>
    {
        opt.PermitLimit = 10;              // 10 requests
        opt.Window = TimeSpan.FromMinutes(1); // per minute
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;                // No queueing, reject immediately
    });
});

app.UseRateLimiter();

// Component routing
@attribute [EnableRateLimiting("CompanyBooking")]
```
**Source:** [Rate limiting middleware in ASP.NET Core | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)

**Configuration decision (Claude's discretion):** Recommend 10 requests per minute per IP for GUID endpoint. Too restrictive breaks legitimate retries; too permissive enables enumeration. Fixed window simpler than sliding window for this use case.

### Pattern 4: Dynamic List Validation with RuleForEach
**What:** FluentValidation validates collections with item-level rules
**When to use:** Participant list where each row needs validation
**Example:**
```csharp
public class CompanyBookingValidator : AbstractValidator<CompanyBookingFormModel>
{
    public CompanyBookingValidator()
    {
        RuleFor(x => x.Participants)
            .NotEmpty().WithMessage("Mindestens ein Teilnehmer erforderlich");

        RuleForEach(x => x.Participants)
            .SetValidator(new ParticipantValidator());
    }
}

public class ParticipantValidator : AbstractValidator<ParticipantModel>
{
    public ParticipantValidator()
    {
        RuleFor(x => x.Salutation)
            .NotEmpty().WithMessage("Anrede ist erforderlich");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Vorname ist erforderlich")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Nachname ist erforderlich")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-Mail ist erforderlich")
            .EmailAddress().WithMessage("Ungültige E-Mail-Adresse")
            .MaximumLength(200);

        RuleFor(x => x.SelectedAgendaItemIds)
            .NotEmpty().WithMessage("Mindestens ein Agendapunkt muss ausgewählt werden");
    }
}
```
**Source:** Phase 4 CompanyInvitationValidator.cs (existing codebase pattern)

### Pattern 5: Reactive Cost Calculation with Blazor Binding
**What:** Computed properties update automatically when bound values change
**When to use:** Live cost summary without manual refresh triggers
**Example:**
```csharp
// In CompanyBooking.razor.cs
private List<ParticipantModel> Participants { get; set; } = new();
private List<int> SelectedExtraOptionIds { get; set; } = new();

private decimal TotalCost
{
    get
    {
        decimal total = 0;

        // Calculate per-participant costs
        foreach (var participant in Participants)
        {
            foreach (var agendaItemId in participant.SelectedAgendaItemIds)
            {
                var price = GetAgendaItemPrice(agendaItemId);
                total += price;
            }
        }

        // Add extra option costs
        foreach (var optionId in SelectedExtraOptionIds)
        {
            var option = eventOptions.FirstOrDefault(o => o.Id == optionId);
            total += option?.Price ?? 0;
        }

        return total;
    }
}

// UI binds to TotalCost - Blazor re-renders when Participants or SelectedExtraOptionIds change
<div class="sticky-summary">
    <h4>Gesamtkosten</h4>
    <p>@TotalCost.ToString("C")</p>
</div>
```
**Source:** Blazor two-way binding pattern (standard Blazor Server behavior)

**Note:** No SignalR needed - Blazor Server's existing WebSocket circuit handles same-page updates automatically via StateHasChanged() calls triggered by input bindings.

### Pattern 6: Transaction-Based Booking Submission
**What:** EF Core transaction ensures atomicity when creating multiple related entities
**When to use:** Booking submission creates EventCompany status update + multiple Registration records
**Example:**
```csharp
public async Task<(bool Success, string? ErrorMessage)> SubmitBookingAsync(
    int eventCompanyId,
    CompanyBookingFormModel formModel)
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        var invitation = await _context.EventCompanies
            .FirstOrDefaultAsync(ec => ec.Id == eventCompanyId);

        if (invitation == null)
            return (false, "Einladung nicht gefunden.");

        // Update invitation status
        invitation.Status = InvitationStatus.Booked;

        // Create registration for each participant
        foreach (var participant in formModel.Participants)
        {
            var registration = new Registration
            {
                EventId = invitation.EventId,
                EventCompanyId = invitation.Id,
                RegistrationType = RegistrationType.Company,
                FirstName = participant.FirstName,
                LastName = participant.LastName,
                Email = participant.Email,
                RegistrationDateUtc = DateTime.UtcNow,
                IsConfirmed = true
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync(); // Get registration ID

            // Link agenda items
            foreach (var agendaItemId in participant.SelectedAgendaItemIds)
            {
                _context.RegistrationAgendaItems.Add(new RegistrationAgendaItem
                {
                    RegistrationId = registration.Id,
                    AgendaItemId = agendaItemId
                });
            }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Fire-and-forget admin notification
        _ = Task.Run(() => SendAdminNotification(invitation));

        return (true, null);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Failed to submit booking for EventCompany {Id}", eventCompanyId);
        throw;
    }
}
```
**Source:** Phase 3 RegistrationService.cs, Phase 4 CompanyInvitationService.cs (existing transaction patterns)

### Pattern 7: Fire-and-Forget Email Notifications
**What:** Non-blocking email sending after successful database commit
**When to use:** Admin notifications shouldn't delay user response
**Example:**
```csharp
// After booking submission succeeds
_ = Task.Run(async () =>
{
    try
    {
        await _emailSender.SendAdminBookingNotificationAsync(
            invitation,
            evt,
            participants);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Failed to send admin notification for booking {EventCompanyId}",
            invitation.Id);
    }
});
```
**Source:** Phase 3 RegistrationService.cs, Phase 4 CompanyInvitationService.cs (existing fire-and-forget pattern)

**Rationale:** Email delivery failures shouldn't roll back successful database transactions. Log errors but don't block user flow.

### Anti-Patterns to Avoid
- **Using == for GUID comparison:** Vulnerable to timing attacks. Always use `CryptographicOperations.FixedTimeEquals`.
- **Storing expiration as relative offset:** Store absolute UTC timestamp (`ExpiresAtUtc`) not "expires in 72 hours". Clock skew and runtime changes break relative calculations.
- **Global [Authorize] in _Host.cshtml:** Breaks page-level `[AllowAnonymous]`. Set authorization in `_Imports.razor` instead.
- **Validating on submit only:** User experience suffers. Show inline validation errors as user types (Blazored.FluentValidation handles this).
- **Rebuilding cost summary manually:** Use computed properties and Blazor's reactive binding. Manual `StateHasChanged()` calls are error-prone.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Rate limiting | Custom request tracking with IP lists | ASP.NET Core RateLimiter middleware | Built-in, tested, supports distributed scenarios with Redis, handles edge cases (IP spoofing, partition key attacks) |
| Constant-time comparison | Custom byte-by-byte loop | CryptographicOperations.FixedTimeEquals | Compiler optimizations can break custom implementations; built-in method resistant to optimization-based timing leaks |
| GUID generation | Random().Next() or DateTime.Ticks | System.Security.Cryptography.RandomNumberGenerator | Cryptographically secure randomness; Phase 4 CompanyInvitationService.GenerateSecureInvitationCode() already implements RFC 4122 GUID v4 |
| Email HTML templates | String concatenation | MailKit MimeMessage with TextPart("html") | Phase 3/4 patterns proven; HTML injection protection via proper escaping |
| Transaction management | Manual try-catch with commit/rollback | EF Core BeginTransactionAsync pattern | Existing pattern in Phase 3/4; handles nested transactions, connection pooling |

**Key insight:** Security primitives (rate limiting, constant-time comparison, crypto RNG) have subtle edge cases. Use battle-tested implementations.

## Common Pitfalls

### Pitfall 1: Blazor Server WebSocket Disconnection with [AllowAnonymous]
**What goes wrong:** When applying `[AllowAnonymous]` to a Blazor page in an app with global authorization policy, the WebSocket circuit disconnects after initial page load. User interactions (`@onclick`, `@bind`) no longer work.
**Why it happens:** Blazor Server validates authorization when establishing the SignalR WebSocket connection. If global authorization is set in `_Host.cshtml` or via fallback policy, the circuit sees "user not authenticated" and disconnects, even though the page allows anonymous access.
**How to avoid:** Set global `[Authorize]` in `Components/Pages/_Imports.razor` instead of `_Host.cshtml`. This allows page-level `[AllowAnonymous]` to override the policy correctly.
**Warning signs:** Page loads initially but buttons/inputs don't respond; browser console shows SignalR connection errors.
**Source:** [Blazor-server app uses a fallback authorization strategy | GitHub Issue #58505](https://github.com/dotnet/aspnetcore/issues/58505)

### Pitfall 2: GUID Enumeration via Timing Attacks
**What goes wrong:** Using standard string comparison (`==` or `string.Equals()`) for GUID validation leaks information via response time. Attackers measure microsecond differences to determine which characters in a GUID are correct, enabling brute-force enumeration.
**Why it happens:** Standard comparison functions short-circuit on first mismatch. Comparing "a000..." vs "b000..." returns faster than "a000..." vs "a999..." because the first byte differs.
**How to avoid:** Use `CryptographicOperations.FixedTimeEquals(byte[], byte[])` which takes constant time regardless of input values. Convert strings to UTF8 bytes before comparison.
**Warning signs:** Security audit tools flag GUID comparison code; no rate limiting on GUID endpoint.
**Source:** [FixedTimeEquals in .NET Core](https://vcsjones.dev/fixed-time-equals-dotnet-core/), [Duende Software - Time-Constant String Comparison](https://docs.duendesoftware.com/identitymodel/utils/time-constant-comparison/)

### Pitfall 3: Rate Limiting with User-Controlled Partition Keys
**What goes wrong:** Creating rate limit partitions based on user input (e.g., GUID from URL) makes the app vulnerable to DoS attacks. Attacker generates thousands of unique GUIDs, each creating a new rate limit partition, exhausting server memory.
**Why it happens:** Rate limiter tracks state per partition. User-controlled keys = unbounded partition count = memory exhaustion.
**How to avoid:** Partition by IP address (with awareness of proxy/NAT scenarios) or apply global rate limiting to the anonymous endpoint. For GUID endpoint: combine IP-based partitioning with conservative limits (e.g., 10 requests/minute per IP).
**Warning signs:** Memory usage grows unbounded under load testing; rate limiter state storage consumes gigabytes.
**Source:** [Rate limiting middleware in ASP.NET Core | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0) - DoS attack warning

### Pitfall 4: Token Expiration Without Re-Authentication Path
**What goes wrong:** Setting GUID expiration but not providing a way for company representatives to request a new link. User sees "link expired" error with no recourse except emailing admin manually.
**Why it happens:** Security best practice (short-lived tokens) conflicts with UX requirement (easy access).
**How to avoid:** User decision specifies friendly error page with admin contact. Implement as: expired GUID shows message "This link has expired. Please contact [admin email] for a new invitation." Admin can resend invitation via Phase 4 UI (ResendInvitationAsync method already exists).
**Warning signs:** Helpdesk tickets spike after 72 hours; users can't access booking page.
**Source:** [Token Expiry Best Practices - DEV Community](https://dev.to/zuplo/token-expiry-best-practices-3feo) - Balance security with UX

### Pitfall 5: Reactive Binding Without Explicit StateHasChanged
**What goes wrong:** Cost summary doesn't update when participant list changes, despite using computed properties.
**Why it happens:** Blazor's change detection doesn't automatically track deep changes in collections. Adding/removing participants or changing nested properties (like `SelectedAgendaItemIds`) may not trigger re-render.
**How to avoid:** Call `StateHasChanged()` explicitly after modifying collection or nested properties. Alternative: Use `ObservableCollection<T>` which triggers notifications automatically.
**Warning signs:** UI only updates after clicking submit; cost summary shows stale values.
**Source:** Standard Blazor Server behavior - collection mutation detection requires explicit notification

### Pitfall 6: Email Sending Blocking User Response
**What goes wrong:** Booking submission takes 5-10 seconds because code awaits SMTP email delivery before returning success message to user.
**Why it happens:** Developer follows synchronous pattern: save to DB → send email → return response.
**How to avoid:** Use fire-and-forget pattern from Phase 3/4: commit transaction first, return success to user immediately, then send email in background Task.Run. If email fails, log error but don't roll back database transaction.
**Warning signs:** User sees loading spinner for multiple seconds; timeout errors on slow SMTP servers.
**Source:** Phase 3 RegistrationService.cs, Phase 4 CompanyInvitationService.cs (existing fire-and-forget pattern)

## Code Examples

Verified patterns from official sources and existing codebase:

### Anonymous Page with GUID Route
```csharp
@page "/company/booking/{InvitationCode}"
@using EventCenter.Web.Services
@using EventCenter.Web.Models
@attribute [AllowAnonymous]
@inject CompanyBookingService BookingService
@inject NavigationManager Navigation

<PageTitle>Firmenbuchung - @(eventCompany?.Event.Title ?? "Veranstaltungscenter")</PageTitle>

@if (isLoading)
{
    <div class="text-center py-5">
        <div class="spinner-border" role="status"></div>
    </div>
}
else if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="container py-4">
        <div class="alert alert-danger">
            <h4>@errorMessage</h4>
            <p>Bitte kontaktieren Sie uns unter info@example.com für eine neue Einladung.</p>
        </div>
    </div>
}
else
{
    <!-- Booking form content -->
}

@code {
    [Parameter]
    public string InvitationCode { get; set; } = string.Empty;

    private EventCompany? eventCompany;
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        var (isValid, company, error) = await BookingService.ValidateInvitationCodeAsync(InvitationCode);

        if (!isValid)
        {
            errorMessage = error ?? "Ungültiger Einladungscode.";
        }
        else
        {
            eventCompany = company;
        }

        isLoading = false;
    }
}
```
**Source:** Existing codebase pattern from Phase 3 EventRegistration.razor

### Constant-Time GUID Validation
```csharp
using System.Security.Cryptography;
using System.Text;

public async Task<(bool IsValid, EventCompany? Company, string? ErrorMessage)>
    ValidateInvitationCodeAsync(string invitationCode)
{
    // Normalize input (trim, lowercase if case-insensitive)
    var normalizedCode = invitationCode?.Trim() ?? string.Empty;

    if (string.IsNullOrEmpty(normalizedCode))
        return (false, null, "Einladungscode fehlt.");

    // Convert to bytes for constant-time comparison
    var inputBytes = Encoding.UTF8.GetBytes(normalizedCode);

    // Query all potential matches (database query is not constant-time,
    // but prevents timing leak of "valid vs invalid" comparison)
    var invitation = await _context.EventCompanies
        .Include(ec => ec.Event)
            .ThenInclude(e => e.AgendaItems)
        .Include(ec => ec.AgendaItemPrices)
            .ThenInclude(aip => aip.AgendaItem)
        .Where(ec => ec.InvitationCode != null)
        .ToListAsync(); // Load all codes (filter in-memory for constant-time comparison)

    EventCompany? match = null;
    foreach (var candidate in invitation)
    {
        var candidateBytes = Encoding.UTF8.GetBytes(candidate.InvitationCode!);

        // Only compare if lengths match (FixedTimeEquals requires equal lengths)
        if (inputBytes.Length == candidateBytes.Length &&
            CryptographicOperations.FixedTimeEquals(inputBytes, candidateBytes))
        {
            match = candidate;
            break;
        }
    }

    if (match == null)
        return (false, null, "Dieser Link ist ungültig oder abgelaufen.");

    // Check expiration
    if (match.ExpiresAtUtc.HasValue && DateTime.UtcNow > match.ExpiresAtUtc.Value)
        return (false, null, "Dieser Link ist abgelaufen.");

    return (true, match, null);
}
```
**Source:** [FixedTimeEquals in .NET Core](https://vcsjones.dev/fixed-time-equals-dotnet-core/)

**Note:** Loading all invitation codes into memory is acceptable for small datasets (< 10,000 companies per event). For larger scale, use database-level filtering with indexed lookup, then apply constant-time comparison only to matching records.

### Rate Limiting Configuration
```csharp
// Program.cs
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "CompanyBooking", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0; // Reject immediately, no queueing
    });

    // Global fallback policy for anonymous endpoints
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Partition by IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Zu viele Anfragen. Bitte versuchen Sie es später erneut.",
            cancellationToken);
    };
});

var app = builder.Build();

app.UseRateLimiter(); // Must be before UseAuthorization

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireRateLimiting("CompanyBooking"); // Apply to Blazor endpoints
```
**Source:** [Rate limiting middleware in ASP.NET Core | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)

### Dynamic Participant List Validation
```csharp
// Models/CompanyBookingFormModel.cs
public class CompanyBookingFormModel
{
    public int EventCompanyId { get; set; }
    public List<ParticipantModel> Participants { get; set; } = new();
    public List<int> SelectedExtraOptionIds { get; set; } = new();
}

public class ParticipantModel
{
    public string Salutation { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<int> SelectedAgendaItemIds { get; set; } = new();
}

// Validators/CompanyBookingValidator.cs
public class CompanyBookingValidator : AbstractValidator<CompanyBookingFormModel>
{
    public CompanyBookingValidator()
    {
        RuleFor(x => x.Participants)
            .NotEmpty().WithMessage("Mindestens ein Teilnehmer erforderlich");

        RuleForEach(x => x.Participants)
            .SetValidator(new ParticipantValidator());
    }
}

public class ParticipantValidator : AbstractValidator<ParticipantModel>
{
    public ParticipantValidator()
    {
        RuleFor(x => x.Salutation)
            .NotEmpty().WithMessage("Anrede ist erforderlich")
            .Must(s => new[] { "Herr", "Frau", "Divers" }.Contains(s))
            .WithMessage("Ungültige Anrede");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Vorname ist erforderlich")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Nachname ist erforderlich")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-Mail ist erforderlich")
            .EmailAddress().WithMessage("Ungültige E-Mail-Adresse")
            .MaximumLength(200);

        RuleFor(x => x.SelectedAgendaItemIds)
            .NotEmpty().WithMessage("Mindestens ein Agendapunkt muss ausgewählt werden");
    }
}
```
**Source:** Phase 4 CompanyInvitationValidator.cs (RuleForEach pattern), [FluentValidation Blazor documentation](https://docs.fluentvalidation.net/en/latest/blazor.html)

### Inline Editable Participant Table
```html
<!-- CompanyBooking.razor -->
<div class="card mb-4">
    <div class="card-header">
        <h4>Teilnehmer</h4>
    </div>
    <div class="card-body">
        <div class="table-responsive">
            <table class="table">
                <thead>
                    <tr>
                        <th>Anrede</th>
                        <th>Vorname</th>
                        <th>Nachname</th>
                        <th>E-Mail</th>
                        <th>Agendapunkte</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody>
                    @for (int i = 0; i < model.Participants.Count; i++)
                    {
                        var index = i; // Capture for lambda
                        var participant = model.Participants[index];

                        <tr>
                            <td>
                                <InputSelect @bind-Value="participant.Salutation" class="form-select">
                                    <option value="">Bitte wählen</option>
                                    <option value="Herr">Herr</option>
                                    <option value="Frau">Frau</option>
                                    <option value="Divers">Divers</option>
                                </InputSelect>
                                <ValidationMessage For="@(() => participant.Salutation)" />
                            </td>
                            <td>
                                <InputText @bind-Value="participant.FirstName"
                                          class="form-control"
                                          placeholder="Vorname" />
                                <ValidationMessage For="@(() => participant.FirstName)" />
                            </td>
                            <td>
                                <InputText @bind-Value="participant.LastName"
                                          class="form-control"
                                          placeholder="Nachname" />
                                <ValidationMessage For="@(() => participant.LastName)" />
                            </td>
                            <td>
                                <InputText @bind-Value="participant.Email"
                                          class="form-control"
                                          type="email"
                                          placeholder="email@example.com" />
                                <ValidationMessage For="@(() => participant.Email)" />
                            </td>
                            <td>
                                @foreach (var agendaItem in availableAgendaItems)
                                {
                                    <div class="form-check">
                                        <input type="checkbox"
                                              class="form-check-input"
                                              checked="@participant.SelectedAgendaItemIds.Contains(agendaItem.Id)"
                                              @onchange="e => OnAgendaItemToggle(index, agendaItem.Id, (bool)e.Value!)" />
                                        <label class="form-check-label">
                                            @agendaItem.Title (@GetAgendaItemPrice(agendaItem.Id).ToString("C"))
                                        </label>
                                    </div>
                                }
                            </td>
                            <td>
                                <button type="button"
                                        class="btn btn-sm btn-danger"
                                        @onclick="() => RemoveParticipant(index)">
                                    <i class="bi bi-trash"></i>
                                </button>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>

        <button type="button" class="btn btn-primary" @onclick="AddParticipant">
            <i class="bi bi-plus-circle"></i> Teilnehmer hinzufügen
        </button>
    </div>
</div>

@code {
    private void AddParticipant()
    {
        model.Participants.Add(new ParticipantModel());
        StateHasChanged(); // Trigger re-render
    }

    private void RemoveParticipant(int index)
    {
        model.Participants.RemoveAt(index);
        StateHasChanged(); // Trigger cost summary update
    }

    private void OnAgendaItemToggle(int participantIndex, int agendaItemId, bool isSelected)
    {
        var participant = model.Participants[participantIndex];

        if (isSelected && !participant.SelectedAgendaItemIds.Contains(agendaItemId))
        {
            participant.SelectedAgendaItemIds.Add(agendaItemId);
        }
        else if (!isSelected && participant.SelectedAgendaItemIds.Contains(agendaItemId))
        {
            participant.SelectedAgendaItemIds.Remove(agendaItemId);
        }

        StateHasChanged(); // Trigger cost summary update
    }
}
```
**Source:** Blazor two-way binding pattern, [Implementing Inline Table Cell Editing in Blazor | Medium](https://medium.com/@sabbiryan/implementing-inline-table-cell-editing-in-blazor-16f4d9e30de8)

### Sticky Cost Summary with Reactive Updates
```html
<!-- CompanyBooking.razor -->
<div class="cost-summary-sticky">
    <div class="card">
        <div class="card-header bg-success text-white">
            <h5 class="mb-0">Kostenübersicht</h5>
        </div>
        <div class="card-body">
            <h6>Teilnehmerkosten:</h6>
            <ul class="list-unstyled">
                @foreach (var participant in model.Participants)
                {
                    if (participant.SelectedAgendaItemIds.Any())
                    {
                        var participantCost = CalculateParticipantCost(participant);
                        <li>
                            @participant.FirstName @participant.LastName:
                            <strong>@participantCost.ToString("C")</strong>
                        </li>
                    }
                }
            </ul>

            @if (model.SelectedExtraOptionIds.Any())
            {
                <hr />
                <h6>Zusatzoptionen:</h6>
                <ul class="list-unstyled">
                    @foreach (var optionId in model.SelectedExtraOptionIds)
                    {
                        var option = availableOptions.FirstOrDefault(o => o.Id == optionId);
                        if (option != null)
                        {
                            <li>@option.Name: <strong>@option.Price.ToString("C")</strong></li>
                        }
                    }
                </ul>
            }

            <hr />
            <div class="d-flex justify-content-between align-items-center">
                <h5>Gesamtkosten:</h5>
                <h4 class="text-success">@TotalCost.ToString("C")</h4>
            </div>
            <small class="text-muted">Alle Preise zzgl. MwSt.</small>
        </div>
    </div>
</div>

<style>
    .cost-summary-sticky {
        position: sticky;
        top: 20px;
        z-index: 100;
    }
</style>

@code {
    private decimal TotalCost
    {
        get
        {
            decimal total = 0;

            // Sum participant costs
            foreach (var participant in model.Participants)
            {
                total += CalculateParticipantCost(participant);
            }

            // Sum extra option costs
            foreach (var optionId in model.SelectedExtraOptionIds)
            {
                var option = availableOptions.FirstOrDefault(o => o.Id == optionId);
                total += option?.Price ?? 0;
            }

            return total;
        }
    }

    private decimal CalculateParticipantCost(ParticipantModel participant)
    {
        decimal cost = 0;

        foreach (var agendaItemId in participant.SelectedAgendaItemIds)
        {
            cost += GetAgendaItemPrice(agendaItemId);
        }

        return cost;
    }

    private decimal GetAgendaItemPrice(int agendaItemId)
    {
        // Check company-specific pricing first
        var customPrice = eventCompany?.AgendaItemPrices
            .FirstOrDefault(aip => aip.AgendaItemId == agendaItemId);

        if (customPrice != null)
            return customPrice.CustomPrice ?? 0;

        // Fallback to base price
        var agendaItem = availableAgendaItems.FirstOrDefault(ai => ai.Id == agendaItemId);
        return agendaItem?.CostForMakler ?? 0;
    }
}
```
**Source:** CSS sticky positioning (standard), Blazor computed properties (standard Blazor pattern)

### Admin Email Notification
```csharp
// Extend IEmailSender interface
public interface IEmailSender
{
    Task SendRegistrationConfirmationAsync(Registration registration);
    Task SendCompanyInvitationAsync(EventCompany invitation, Event evt, string personalMessage, string invitationLink);

    // New methods for Phase 5
    Task SendAdminBookingNotificationAsync(EventCompany company, Event evt, List<ParticipantModel> participants);
    Task SendAdminCancellationNotificationAsync(EventCompany company, Event evt, string cancellationComment);
}

// Implementation in MailKitEmailSender.cs
public async Task SendAdminBookingNotificationAsync(
    EventCompany company,
    Event evt,
    List<ParticipantModel> participants)
{
    try
    {
        var adminEmail = _configuration["AdminNotificationEmail"] ?? "admin@example.com";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(new MailboxAddress("Admin", adminEmail));
        message.Subject = $"Neue Firmenbuchung: {company.CompanyName} - {evt.Title}";

        var htmlBody = BuildAdminBookingNotificationHtml(company, evt, participants);
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl);

        if (!string.IsNullOrEmpty(_settings.Username))
        {
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation(
            "Successfully sent admin booking notification for company {CompanyId}",
            company.Id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Failed to send admin booking notification for company {CompanyId}",
            company.Id);
        throw;
    }
}

private string BuildAdminBookingNotificationHtml(
    EventCompany company,
    Event evt,
    List<ParticipantModel> participants)
{
    var participantListHtml = string.Join("", participants.Select(p =>
        $"<li>{p.Salutation} {p.FirstName} {p.LastName} ({p.Email})</li>"));

    return $@"
<!DOCTYPE html>
<html lang=""de"">
<head>
    <meta charset=""UTF-8"">
    <title>Neue Firmenbuchung</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""background-color: #007bff; color: white; padding: 20px; text-align: center;"">
        <h1>Neue Firmenbuchung eingegangen</h1>
    </div>

    <div style=""padding: 20px;"">
        <h2>Veranstaltung: {evt.Title}</h2>
        <p><strong>Firma:</strong> {company.CompanyName}</p>
        <p><strong>Kontakt:</strong> {company.ContactEmail}</p>
        <p><strong>Anzahl Teilnehmer:</strong> {participants.Count}</p>

        <h3>Teilnehmerliste:</h3>
        <ul>{participantListHtml}</ul>

        <p style=""margin-top: 20px;"">
            <a href=""{_configuration["BaseUrl"]}/admin/events/{evt.Id}/companies""
               style=""background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">
                Zur Verwaltung
            </a>
        </p>
    </div>
</body>
</html>";
}
```
**Source:** Phase 3/4 MailKitEmailSender.cs patterns, [Email Management with .NET 9 and C# using MailKit - DEV Community](https://dev.to/adrianbailador/email-management-with-net-9-and-c-using-mailkit-cjf)

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| AspNetCoreRateLimit (third-party) | Built-in ASP.NET Core RateLimiter | .NET 7 (2022) | No external dependency, native performance, better integration with endpoints |
| Manual byte comparison loops | CryptographicOperations.FixedTimeEquals | .NET Core 2.1 (2018) | Compiler-resistant constant-time comparison, prevents timing attacks |
| Blazor WASM for anonymous pages | Blazor Server with [AllowAnonymous] | .NET 6+ (2021) | Reduced complexity, server-side validation, no API layer needed |
| Manual StateHasChanged calls | Automatic via @bind directive | Blazor since inception | Less boilerplate, fewer bugs from missed updates |
| SmtpClient (obsolete) | MailKit | .NET Core 2.0+ (2017) | Cross-platform, actively maintained, OAuth2 support |

**Deprecated/outdated:**
- `SmtpClient` from System.Net.Mail: Obsolete since .NET Core 2.0, replaced by MailKit
- Third-party rate limiting libraries (AspNetCoreRateLimit): Superseded by built-in middleware in .NET 7+
- Blazor preview-era authorization workarounds (custom AuthenticationStateProvider for anonymous): Solved by `[AllowAnonymous]` attribute support

## Open Questions

1. **GUID Expiration Duration**
   - What we know: Token best practices recommend 30 minutes to 72 hours depending on sensitivity
   - What's unclear: User hasn't specified exact expiration window; Phase 4 added `ExpiresAtUtc` field but left null
   - Recommendation: Implement 72-hour expiration (3 days) as default - balances security (prevents indefinite link validity) with UX (company representatives have reasonable time to respond). Make configurable via appsettings.json for production tuning. User decision allows admin to resend invitation if expired.

2. **Non-Participation vs Cancellation Storage**
   - What we know: User decision specifies two actions (cancel booking vs report non-participation); existing Registration entity has `IsCancelled` field
   - What's unclear: Should non-participation be stored differently than full cancellation? Do they need separate status tracking?
   - Recommendation: Add `CancellationReason` enum field to Registration entity with values: `None`, `FullCancellation`, `NonParticipation`. Store cancellation comment in existing `SpecialRequirements` field (repurposed) or add new `CancellationComment` field. This preserves audit trail while distinguishing cancellation types.

3. **Rate Limiting Partition Strategy**
   - What we know: IP-based partitioning vulnerable to NAT/proxy scenarios where many legitimate users share one IP; GUID-based partitioning vulnerable to DoS via unlimited partition creation
   - What's unclear: Best balance for this specific use case (company booking portal, expected low traffic volume)
   - Recommendation: Use IP-based partitioning with conservative global limit (10 requests/minute per IP). Acceptable false positives (multiple users behind corporate proxy) because: (1) booking is async, no real-time requirement, (2) company representatives can retry after 1 minute, (3) low expected traffic volume makes collision unlikely. Monitor rate limit rejections in production; adjust if legitimate traffic blocked.

4. **Sticky Summary Positioning**
   - What we know: User decision specifies sticky/fixed cost summary; common patterns are sidebar (desktop) vs bottom bar (mobile)
   - What's unclear: User hasn't specified exact positioning
   - Recommendation (Claude's discretion): Use right sidebar with `position: sticky` on desktop (viewport width > 768px), bottom bar with `position: fixed` on mobile. Rationale: sidebar keeps form and summary visible simultaneously on large screens; bottom bar doesn't obscure form inputs on mobile. Implement with CSS media queries and Bootstrap responsive utilities.

5. **Validation Timing (Inline vs On-Submit)**
   - What we know: User decision leaves validation UX to Claude's discretion; Blazored.FluentValidation supports both inline and on-submit validation
   - What's unclear: Best UX balance for multi-participant form
   - Recommendation: Inline validation for individual fields (show error on blur), on-submit validation for cross-field rules (e.g., "at least one participant required"). Rationale: immediate feedback reduces user frustration; defer expensive validation (duplicate email checks) until submit to minimize server load. Blazored.FluentValidation default behavior already implements this pattern.

## Sources

### Primary (HIGH confidence)
- Microsoft Learn: [Rate limiting middleware in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0) - Official documentation for built-in rate limiting
- Microsoft Learn: [Allow Anonymous user on a Blazor page](https://learn.microsoft.com/en-us/answers/questions/248835/allow-anonymous-user-on-a-blazor-page) - Official guidance on [AllowAnonymous] attribute
- FixedTimeEquals in .NET Core: [https://vcsjones.dev/fixed-time-equals-dotnet-core/](https://vcsjones.dev/fixed-time-equals-dotnet-core/) - Detailed explanation of constant-time comparison API
- FluentValidation Blazor documentation: [https://docs.fluentvalidation.net/en/latest/blazor.html](https://docs.fluentvalidation.net/en/latest/blazor.html) - Official integration guide
- Existing codebase: EventCenter.Web (Phases 1-4) - Proven patterns for services, validation, email, transactions

### Secondary (MEDIUM confidence)
- Duende Software: [Time-Constant String Comparison](https://docs.duendesoftware.com/identitymodel/utils/time-constant-comparison/) - Industry standard identity library guidance
- C# Corner: [How to Implement Rate Limiting in ASP.NET Core 8?](https://www.c-sharpcorner.com/article/how-to-implement-rate-limiting-in-asp-net-core-8/) - Practical implementation examples
- DEV Community: [Token Expiry Best Practices](https://dev.to/zuplo/token-expiry-best-practices-3feo) - Security best practices for token lifetimes
- DEV Community: [Email Management with .NET 9 and C# using MailKit](https://dev.to/adrianbailador/email-management-with-net-9-and-c-using-mailkit-cjf) - MailKit implementation patterns
- Medium: [Implementing Inline Table Cell Editing in Blazor](https://medium.com/@sabbiryan/implementing-inline-table-cell-editing-in-blazor-16f4d9e30de8) - Inline editing UI patterns

### Tertiary (LOW confidence)
- GitHub Issue #58505: [Blazor-server app uses a fallback authorization strategy](https://github.com/dotnet/aspnetcore/issues/58505) - User-reported WebSocket disconnection issue
- GitHub Issue #51101: [.NET 8 "Blazor Web App" Authentication and AllowAnonymous](https://github.com/dotnet/aspnetcore/discussions/51101) - Community discussion on anonymous access patterns

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All libraries already in project (ASP.NET Core 8, Blazor Server, FluentValidation, MailKit, EF Core), official Microsoft documentation, proven in production
- Architecture patterns: HIGH - Existing codebase provides direct examples (fire-and-forget email, service layer, transaction pattern, RuleForEach validation), official Microsoft guidance on rate limiting and anonymous access
- Security (rate limiting, constant-time comparison): HIGH - Official Microsoft documentation, built-in APIs since .NET Core 2.1/.NET 7, verified with authoritative sources
- UI patterns (sticky summary, inline editing): MEDIUM - Verified with community articles and Blazor documentation, but exact implementation details require testing for responsive behavior

**Research date:** 2026-02-27
**Valid until:** ~2026-03-27 (30 days) - ASP.NET Core 8 is stable LTS release, libraries mature, patterns unlikely to change significantly in next month
