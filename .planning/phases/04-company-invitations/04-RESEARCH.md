# Phase 4: Company Invitations - Research

**Researched:** 2026-02-27
**Domain:** Company invitation management with custom per-item pricing, GUID-based secure access links, and email notifications
**Confidence:** HIGH

## Summary

Phase 4 enables admins to invite companies to events with customizable pricing per agenda item. The phase involves extending the existing EventCompany entity with a join table for per-item pricing overrides, implementing GUID-based invitation links using cryptographically secure random generation, and creating an admin UI for managing invitation lifecycle (draft, send, track status, delete). Email templates follow the established MailKit pattern from Phase 3, with HTML templates for professional company invitations. The architecture leverages existing patterns: FluentValidation for input validation, service layer pattern for business logic, and Blazor Server components with Bootstrap 5 for UI.

**Primary recommendation:** Use explicit join entity (EventCompanyAgendaItemPrice) for per-item pricing overrides, generate invitation codes with RandomNumberGenerator.GetBytes() for cryptographic security, and extend IEmailSender interface with company invitation method following existing email template patterns.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Pricing configuration:**
- Both percentage discount AND per-item price override available
- Percentage discount applies first, then admin can tweak individual items on top
- Base (default) event price displayed as reference next to each custom price field
- Pricing is always editable, even after the company has booked (affects future invoicing)

**Invitation creation flow:**
- Single page form on the event detail page: select company, configure pricing, optional personal message
- "Company Invitations" tab on the event detail page — invitations scoped to an event
- Two creation modes: single invite for custom pricing, batch invite for standard pricing
- Admin can choose "Save as draft" or "Create & Send" — draft option available

**Status view & management:**
- Sortable table layout: company name, contact, status, date sent, actions
- Four statuses: Draft, Sent, Booked, Cancelled
- Status-dependent actions: Draft (edit/send/delete), Sent (edit/resend/delete), Booked (edit/view booking), Cancelled (edit/re-invite)
- Edit action available in all statuses (pricing is always editable)
- Deletion requires confirmation dialog showing company name and status

**Email content & template:**
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

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| COMP-01 | Admin kann Firma zu Veranstaltung einladen | Standard Stack: Blazor Server form components, EventCompany entity extension; Architecture: Service layer pattern for invitation creation |
| COMP-02 | Admin kann firmenspezifische Konditionen pro Agendapunkt festlegen | Architecture: Join entity pattern (EventCompanyAgendaItemPrice) for per-item pricing with percentage discount + override fields; Don't Hand-Roll: Use EF Core many-to-many with custom properties |
| COMP-03 | Admin kann Einladungsmail an Firma versenden | Standard Stack: MailKit for SMTP (existing); Architecture: Extend IEmailSender interface with SendCompanyInvitationAsync; Code Examples: HTML email template pattern from Phase 3 |
| COMP-04 | Admin kann Firmeneinladung löschen | Architecture: Service layer business rules (prevent deletion if booked); UI: Confirmation dialog component |
| COMP-05 | Admin kann Einladungs- und Buchungsstatus einer Firma einsehen | Architecture: Status enum (Draft, Sent, Booked, Cancelled); UI: Status badge component pattern; Don't Hand-Roll: Use existing EventStatusBadge pattern |
| MAIL-03 | System sendet Einladung an Firma mit GUID-Link | Standard Stack: RandomNumberGenerator for cryptographic GUID; Don't Hand-Roll: Never use Guid.NewGuid() for security-sensitive tokens; Pitfall: GUID enumeration attacks require sufficient entropy |

</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FluentValidation | 11.x | Input validation for invitation forms and pricing | Already used in Phase 1-3; automatic validator discovery via DI |
| FluentValidation.DependencyInjectionExtensions | 11.x | Auto-registration of validators | Project standard from Phase 1 |
| Blazored.FluentValidation | 2.x | Blazor integration for real-time validation | Project standard for form validation |
| MailKit | 4.x | SMTP email sending for invitation emails | Already used in Phase 3 (MAIL-01); industry standard, cross-platform |
| Bootstrap 5 | (via CDN) | UI framework for admin invitation pages | Project standard; consistent with existing admin pages |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Security.Cryptography | Built-in .NET 8 | Cryptographically secure GUID generation | For generating InvitationCode on EventCompany entity |
| xUnit | 2.4.2 | Unit testing framework | Existing test infrastructure from Phase 1 |
| Moq | 4.x | Mocking framework for service tests | Existing test infrastructure |
| SQLite (in-memory) | 9.x | Test database | Existing test infrastructure from Phase 1 |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| RandomNumberGenerator | Guid.NewGuid() | Guid.NewGuid() uses predictable timestamps and MAC addresses; not suitable for security tokens |
| MailKit | System.Net.Mail.SmtpClient | SmtpClient is legacy and not cross-platform; MailKit is industry standard |
| Explicit join entity | EF Core skip navigation | Skip navigation cannot store custom properties (pricing overrides); explicit join entity required |

**Installation:**
No new packages required — all dependencies already installed in project.

## Architecture Patterns

### Recommended Project Structure
```
EventCenter.Web/
├── Domain/
│   ├── Entities/
│   │   ├── EventCompany.cs                    # Extend with status enum, invitation fields
│   │   └── EventCompanyAgendaItemPrice.cs      # NEW: Join entity for per-item pricing
│   └── Enums/
│       └── InvitationStatus.cs                # NEW: Draft, Sent, Booked, Cancelled
├── Data/
│   └── Configurations/
│       ├── EventCompanyConfiguration.cs        # Update with new fields
│       └── EventCompanyAgendaItemPriceConfiguration.cs  # NEW
├── Services/
│   └── CompanyInvitationService.cs            # NEW: Business logic for invitations
├── Infrastructure/
│   └── Email/
│       ├── IEmailSender.cs                    # Extend interface
│       └── MailKitEmailSender.cs              # Add SendCompanyInvitationAsync
├── Models/
│   ├── CompanyInvitationFormModel.cs          # NEW: DTO for form validation
│   └── CompanyAgendaItemPriceModel.cs         # NEW: Nested DTO for pricing
├── Validators/
│   └── CompanyInvitationValidator.cs          # NEW: FluentValidation rules
└── Components/
    └── Pages/
        └── Admin/
            └── Events/
                └── CompanyInvitations.razor    # NEW: Invitations tab on event detail
```

### Pattern 1: Join Entity for Per-Item Pricing Overrides

**What:** Explicit join entity with custom properties for many-to-many relationship between EventCompany and EventAgendaItem with pricing overrides

**When to use:** When a many-to-many relationship requires additional data beyond the foreign keys (e.g., custom pricing, discounts, metadata)

**Example:**
```csharp
// Domain/Entities/EventCompanyAgendaItemPrice.cs
public class EventCompanyAgendaItemPrice
{
    public int EventCompanyId { get; set; }
    public int AgendaItemId { get; set; }
    public decimal? CustomPrice { get; set; }  // Nullable: null means use default price

    // Navigation properties
    public EventCompany EventCompany { get; set; } = null!;
    public EventAgendaItem AgendaItem { get; set; } = null!;
}

// Data/Configurations/EventCompanyAgendaItemPriceConfiguration.cs
public class EventCompanyAgendaItemPriceConfiguration : IEntityTypeConfiguration<EventCompanyAgendaItemPrice>
{
    public void Configure(EntityTypeBuilder<EventCompanyAgendaItemPrice> builder)
    {
        builder.ToTable("EventCompanyAgendaItemPrices");

        // Composite primary key
        builder.HasKey(p => new { p.EventCompanyId, p.AgendaItemId });

        builder.Property(p => p.CustomPrice)
            .HasPrecision(18, 2);

        // Relationships
        builder.HasOne(p => p.EventCompany)
            .WithMany(ec => ec.AgendaItemPrices)
            .HasForeignKey(p => p.EventCompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.AgendaItem)
            .WithMany()
            .HasForeignKey(p => p.AgendaItemId)
            .OnDelete(DeleteBehavior.NoAction); // Prevent cascade cycle
    }
}
```
**Source:** [Many-to-many relationships - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many)

### Pattern 2: Cryptographically Secure GUID Generation

**What:** Generate invitation codes using RandomNumberGenerator for cryptographic security instead of Guid.NewGuid()

**When to use:** When GUIDs are used for security-sensitive purposes (access tokens, invitation links, password reset tokens)

**Example:**
```csharp
// Services/CompanyInvitationService.cs
using System.Security.Cryptography;

public class CompanyInvitationService
{
    public string GenerateInvitationCode()
    {
        // Generate 16 cryptographically secure random bytes
        byte[] data = new byte[16];
        RandomNumberGenerator.Fill(data);

        // Create GUID from random bytes with RFC 4122 compliance
        // Set version bits (version 4) and variant bits
        data[7] = (byte)((data[7] & 0x0F) | 0x40); // Version 4
        data[8] = (byte)((data[8] & 0x3F) | 0x80); // Variant RFC 4122

        var guid = new Guid(data);
        return guid.ToString("N"); // 32 hex chars without dashes
    }
}
```
**Source:** [Create a cryptographically secure random GUID in .NET](https://iditect.com/faq/csharp/create-a-cryptographically-secure-random-guid-in-net.html)

### Pattern 3: Status-Dependent Action Buttons (Existing Pattern)

**What:** Conditional rendering of action buttons based on entity status using Blazor conditional rendering

**When to use:** When different statuses enable different operations (Draft allows edit/send, Sent allows resend, etc.)

**Example:**
```razor
@* Components/Pages/Admin/Events/CompanyInvitations.razor *@
@if (invitation.Status == InvitationStatus.Draft)
{
    <div class="btn-group">
        <button class="btn btn-sm btn-primary" @onclick="@(() => SendInvitation(invitation.Id))">
            <span class="bi bi-envelope"></span> Senden
        </button>
        <button class="btn btn-sm btn-secondary" @onclick="@(() => EditInvitation(invitation.Id))">
            <span class="bi bi-pencil"></span> Bearbeiten
        </button>
        <button class="btn btn-sm btn-danger" @onclick="@(() => DeleteInvitation(invitation))">
            <span class="bi bi-trash"></span> Löschen
        </button>
    </div>
}
else if (invitation.Status == InvitationStatus.Sent)
{
    <div class="btn-group">
        <button class="btn btn-sm btn-warning" @onclick="@(() => ResendInvitation(invitation.Id))">
            <span class="bi bi-envelope-arrow-up"></span> Erneut senden
        </button>
        <button class="btn btn-sm btn-secondary" @onclick="@(() => EditInvitation(invitation.Id))">
            <span class="bi bi-pencil"></span> Bearbeiten
        </button>
        <button class="btn btn-sm btn-danger" @onclick="@(() => DeleteInvitation(invitation))">
            <span class="bi bi-trash"></span> Löschen
        </button>
    </div>
}
@* Similar patterns for Booked and Cancelled statuses *@
```
**Source:** Existing project pattern from Phase 2 (EventList.razor action buttons)

### Pattern 4: Nested Collection Validation with FluentValidation

**What:** Validate collections and nested objects using RuleForEach with SetValidator

**When to use:** When form models contain collections that need item-level validation (e.g., agenda item pricing list)

**Example:**
```csharp
// Validators/CompanyInvitationValidator.cs
public class CompanyInvitationValidator : AbstractValidator<CompanyInvitationFormModel>
{
    public CompanyInvitationValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Firmenname ist erforderlich")
            .MaximumLength(200);

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Kontakt-E-Mail ist erforderlich")
            .EmailAddress().WithMessage("Ungültige E-Mail-Adresse");

        RuleFor(x => x.PercentageDiscount)
            .InclusiveBetween(0, 100)
            .When(x => x.PercentageDiscount.HasValue)
            .WithMessage("Rabatt muss zwischen 0% und 100% liegen");

        // Validate nested collection of agenda item prices
        RuleForEach(x => x.AgendaItemPrices)
            .SetValidator(new CompanyAgendaItemPriceValidator());
    }
}

public class CompanyAgendaItemPriceValidator : AbstractValidator<CompanyAgendaItemPriceModel>
{
    public CompanyAgendaItemPriceValidator()
    {
        RuleFor(x => x.CustomPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CustomPrice.HasValue)
            .WithMessage("Preis darf nicht negativ sein");
    }
}
```
**Source:** [Collections — FluentValidation documentation](https://docs.fluentvalidation.net/en/latest/collections.html)

### Pattern 5: HTML Email Template with Personal Message

**What:** Build HTML email templates with dynamic content placeholders following MailKit pattern from Phase 3

**When to use:** For professional email notifications (invitations, confirmations, reminders)

**Example:**
```csharp
// Infrastructure/Email/MailKitEmailSender.cs (extend existing class)
public async Task SendCompanyInvitationAsync(
    EventCompany invitation,
    Event evt,
    string personalMessage,
    string invitationLink)
{
    try
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(new MailboxAddress(invitation.CompanyName, invitation.ContactEmail));
        message.Subject = $"Einladung: {evt.Title}";

        var htmlBody = BuildCompanyInvitationHtmlBody(invitation, evt, personalMessage, invitationLink);
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
            "Successfully sent company invitation email to {Email} for event {EventId}",
            invitation.ContactEmail,
            evt.Id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Failed to send company invitation email to {Email} for event {EventId}",
            invitation.ContactEmail,
            evt.Id);
        throw;
    }
}

private string BuildCompanyInvitationHtmlBody(
    EventCompany invitation,
    Event evt,
    string personalMessage,
    string invitationLink)
{
    var startDate = TimeZoneHelper.FormatDateTimeCet(evt.StartDateUtc, "dd.MM.yyyy HH:mm");
    var endDate = TimeZoneHelper.FormatDateTimeCet(evt.EndDateUtc, "dd.MM.yyyy HH:mm");

    // Build pricing summary HTML
    var pricingSummaryHtml = BuildPricingSummaryHtml(invitation);

    return $@"
<!DOCTYPE html>
<html lang=""de"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Veranstaltungseinladung</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0;"">
        <h1 style=""margin: 0;"">Sie sind eingeladen</h1>
    </div>

    <div style=""background-color: #ffffff; padding: 20px; border: 1px solid #dee2e6; border-radius: 0 0 5px 5px;"">
        <p>Sehr geehrte Damen und Herren,</p>

        {(!string.IsNullOrWhiteSpace(personalMessage) ? $@"
        <div style=""background-color: #f8f9fa; padding: 15px; margin: 20px 0; border-left: 4px solid #007bff; font-style: italic;"">
            {personalMessage}
        </div>" : "")}

        <p>wir laden Sie herzlich zur folgenden Veranstaltung ein:</p>

        <div style=""background-color: #f8f9fa; padding: 15px; margin: 20px 0; border-radius: 5px;"">
            <h2 style=""color: #007bff; margin-top: 0;"">{evt.Title}</h2>
            <p style=""margin: 5px 0;""><strong>Datum:</strong> {startDate} - {endDate}</p>
            <p style=""margin: 5px 0;""><strong>Ort:</strong> {evt.Location}</p>
        </div>

        {pricingSummaryHtml}

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{invitationLink}""
               style=""display: inline-block; background-color: #007bff; color: white; padding: 12px 30px;
                      text-decoration: none; border-radius: 5px; font-weight: bold;"">
                Jetzt anmelden
            </a>
        </div>

        <p style=""margin-top: 30px;"">Wir freuen uns auf Ihre Teilnahme!</p>

        <hr style=""border: none; border-top: 1px solid #dee2e6; margin: 30px 0;""/>

        <p style=""color: #666; font-size: 12px; text-align: center;"">
            Bei Fragen wenden Sie sich bitte an: {_settings.SenderEmail}
        </p>
    </div>
</body>
</html>";
}
```
**Source:** Existing project pattern from Phase 3 (MailKitEmailSender.cs), [Email-Templating with Blazor - Fusonic](https://www.fusonic.net/en/blog/email-templating-blazor)

### Anti-Patterns to Avoid

- **Using Guid.NewGuid() for security tokens:** Standard GUIDs use timestamps and MAC addresses, making them partially predictable. Use RandomNumberGenerator instead.
- **Skip navigation without custom properties:** Cannot store per-item pricing. Use explicit join entity instead.
- **Hard-deleting invitations with bookings:** Violates referential integrity. Use soft delete (status = Cancelled) or prevent deletion via business rules.
- **Inline pricing calculation in UI:** Business logic belongs in service layer. UI should display calculated values from service.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Cryptographic GUID generation | Custom random string generator | RandomNumberGenerator.Fill() with RFC 4122 compliance | Proper entropy, standard format, prevents timing attacks and predictability |
| SMTP email sending | Custom email sender with sockets | MailKit library (already in project) | Handles authentication, TLS, MIME encoding, cross-platform support |
| HTML email templates | String concatenation | String interpolation with helper methods | Maintainability, testability, XSS prevention via proper encoding |
| Per-item pricing storage | JSON column or serialized data | Explicit join entity with EF Core | Type safety, queryability, referential integrity, migration support |
| Form validation | Manual checks in code-behind | FluentValidation with Blazored integration | Reusable rules, automatic UI feedback, testability, separation of concerns |
| Status management | Boolean flags (isDraft, isSent, etc.) | Enum (InvitationStatus) | Type safety, prevents impossible states (e.g., both draft and sent), clear state machine |

**Key insight:** Company invitation management involves complex business rules (pricing overrides, status transitions, email delivery). Using established patterns (service layer, FluentValidation, EF Core relationships, MailKit) prevents common pitfalls like race conditions, validation bypass, and email deliverability issues.

## Common Pitfalls

### Pitfall 1: GUID Enumeration Attacks

**What goes wrong:** Using predictable GUIDs (Guid.NewGuid()) or insufficient entropy allows attackers to guess other companies' invitation links

**Why it happens:** Guid.NewGuid() uses timestamp and MAC address components, making GUIDs partially predictable. Standard implementations generate ~122 bits of randomness instead of full 128 bits.

**How to avoid:**
1. Use RandomNumberGenerator.Fill() to generate cryptographically secure random bytes
2. Apply RFC 4122 version 4 formatting (set version and variant bits)
3. Consider adding rate limiting on invitation link access (Phase 5 concern)
4. Log suspicious access patterns (multiple failed GUID attempts)

**Warning signs:**
- Invitation links accessed without corresponding email opens
- Multiple 404s for near-sequential GUIDs
- Bookings from unexpected IP ranges

**Source:** [Security: Generating a Secure Random GUID in .NET](https://copyprogramming.com/howto/create-a-cryptographically-secure-random-guid-in-net)

### Pitfall 2: Cascade Delete Conflicts with Pricing Overrides

**What goes wrong:** Deleting an agenda item cascades to EventCompanyAgendaItemPrice, but circular cascade paths cause SQL Server errors

**Why it happens:** EF Core cannot determine safe cascade order when multiple cascade paths exist (EventAgendaItem → AgendaItemPrices → EventCompany → Event)

**How to avoid:**
1. Set DeleteBehavior.NoAction on one side of the relationship (typically the "weaker" side)
2. Use DeleteBehavior.Restrict for EventCompany → Registration to prevent deletion if bookings exist
3. Implement soft delete for EventCompany (Status = Cancelled) instead of hard delete
4. Service layer enforces business rule: cannot delete agenda items if company invitations with pricing exist

**Warning signs:**
- SqlException: "Introducing FOREIGN KEY constraint may cause cycles or multiple cascade paths"
- Cascade delete warnings during migration generation
- DeleteBehavior.Cascade on multiple paths to same entity

**Source:** Project experience from Phase 1 (RegistrationAgendaItemConfiguration.cs uses DeleteBehavior.NoAction)

### Pitfall 3: Email Deliverability (SPF/DKIM/DMARC)

**What goes wrong:** Invitation emails land in spam folders or are rejected by recipient mail servers

**Why it happens:**
- SMTP server lacks proper SPF records
- Domain not configured for DKIM signing
- DMARC policy rejects emails from application's sender address
- HTML emails flagged as phishing due to embedded links

**How to avoid:**
1. Configure SPF records for SMTP server IP
2. Enable DKIM signing on mail server (MailKit supports this)
3. Set DMARC policy to quarantine (not reject) during testing
4. Use trusted sender domain (not free email providers)
5. Include plain text alternative for HTML emails
6. Test with mail-tester.com or similar tools
7. Implement email bounce handling (Phase 5 concern)

**Warning signs:**
- High bounce rate in logs
- Users report not receiving emails
- Emails arrive after significant delay
- Links in emails flagged by security software

**Source:** STATE.md blockers list, [Blazor Send Email with MailKit](https://akifmt.github.io/dotnet/2023-09-03-blazor-send-email-with-mailkit/)

### Pitfall 4: Pricing Edits After Booking Create Invoice Confusion

**What goes wrong:** Admin edits company pricing after company has booked, causing mismatch between booking confirmation and actual invoice

**Why it happens:** User requirement allows pricing edits at any time ("Pricing is always editable, even after the company has booked")

**How to avoid:**
1. Display clear warning when editing pricing for Booked status: "Achtung: Diese Firma hat bereits gebucht. Preisänderungen betreffen die Abrechnung."
2. Log all pricing changes with timestamp and admin user (audit trail)
3. Consider adding "InvoiceGenerated" status to lock pricing (discuss with user in later phase)
4. Service layer creates clear separation: pricing = current editable prices, invoice = snapshot at booking time (Phase 5 concern)

**Warning signs:**
- Users confused about invoice amounts
- Frequent "I didn't change the price" support tickets
- Discrepancies between email quotes and invoices

**Source:** User decision from CONTEXT.md (pricing always editable)

### Pitfall 5: Percentage Discount Calculation Rounding Errors

**What goes wrong:** Applying percentage discount then allowing manual overrides creates inconsistent final prices due to rounding

**Why it happens:**
- Decimal precision loss during percentage calculation
- Manual overrides applied to already-rounded values
- Different rounding strategies (banker's rounding vs. away-from-zero)

**How to avoid:**
1. Use decimal (not float/double) for all price calculations
2. Apply consistent rounding strategy: Math.Round(value, 2, MidpointRounding.AwayFromZero)
3. Calculate percentage discount server-side, not in JavaScript
4. Store both base price and custom price in join entity for audit trail
5. Display calculation details to admin: "Basispreis: €100, Rabatt 10%: €90, Ihr Preis: €85"

**Warning signs:**
- Prices differ by 1 cent between preview and email
- Inconsistent totals across UI refreshes
- Pricing "jumps" when toggling between percentage and manual override

**Source:** Project decision from STATE.md (decimal with precision 18,2 for money types)

### Pitfall 6: Race Condition in Batch Invitation Creation

**What goes wrong:** Admin clicks "Create invitations" for multiple companies, async operations overlap, resulting in duplicate invitations or missing records

**Why it happens:**
- Blazor Server fire-and-forget pattern for email sending
- No transaction scope around invitation creation + email sending
- Database concurrency issues when checking for existing invitations

**How to avoid:**
1. Use Database.BeginTransactionAsync for atomic invitation creation (like Phase 3 registration)
2. Disable "Create" button after click (Blazor @onclick with state management)
3. Check for existing EventCompany records within transaction scope
4. Fire-and-forget email sending AFTER transaction commit (not during)
5. Display progress indicator for batch operations

**Warning signs:**
- Duplicate invitation emails sent
- Some companies missing from invitation list
- Partial batch completions with no error message

**Source:** Project pattern from Phase 3 (RegistrationService.cs uses transaction for atomic registration)

## Code Examples

Verified patterns from official sources and existing project:

### Cryptographically Secure Invitation Code Generation
```csharp
// Services/CompanyInvitationService.cs
public string GenerateSecureInvitationCode()
{
    byte[] data = new byte[16];
    RandomNumberGenerator.Fill(data);

    // RFC 4122 version 4 UUID format
    data[7] = (byte)((data[7] & 0x0F) | 0x40); // Version 4
    data[8] = (byte)((data[8] & 0x3F) | 0x80); // Variant 10

    var guid = new Guid(data);
    return guid.ToString("N"); // 32 hex chars: 5f3d8b2e1a7c4f9e8d6b5a4c3e2f1a0b
}
```
**Source:** [Improve Data Security with Cryptographically Secure Random Generation in .NET](https://ilovedotnet.org/blogs/improve-data-security-with-cryptographically-secure-random-generation-in-dotnet/)

### Percentage Discount with Manual Override Logic
```csharp
// Services/CompanyInvitationService.cs
public decimal CalculateCustomPrice(
    decimal basePrice,
    decimal? percentageDiscount,
    decimal? manualOverride)
{
    // Step 1: Apply percentage discount if provided
    decimal discountedPrice = basePrice;
    if (percentageDiscount.HasValue && percentageDiscount.Value > 0)
    {
        decimal discountAmount = basePrice * (percentageDiscount.Value / 100m);
        discountedPrice = basePrice - discountAmount;
        discountedPrice = Math.Round(discountedPrice, 2, MidpointRounding.AwayFromZero);
    }

    // Step 2: Apply manual override if provided (takes precedence)
    if (manualOverride.HasValue)
    {
        return Math.Round(manualOverride.Value, 2, MidpointRounding.AwayFromZero);
    }

    return discountedPrice;
}
```
**Source:** Project pattern for decimal precision from STATE.md

### Service Layer Method for Creating Invitation
```csharp
// Services/CompanyInvitationService.cs
public async Task<(bool Success, int? InvitationId, string? ErrorMessage)> CreateInvitationAsync(
    int eventId,
    CompanyInvitationFormModel model)
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // Validate event exists
        var evt = await _context.Events
            .Include(e => e.AgendaItems)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (evt == null)
        {
            return (false, null, "Veranstaltung nicht gefunden.");
        }

        // Check for duplicate company invitation
        var existingInvitation = await _context.EventCompanies
            .FirstOrDefaultAsync(ec =>
                ec.EventId == eventId &&
                ec.ContactEmail.Equals(model.ContactEmail, StringComparison.OrdinalIgnoreCase));

        if (existingInvitation != null)
        {
            return (false, null, "Diese Firma wurde bereits eingeladen.");
        }

        // Create invitation
        var invitation = new EventCompany
        {
            EventId = eventId,
            CompanyName = model.CompanyName,
            ContactEmail = model.ContactEmail,
            ContactPhone = model.ContactPhone,
            InvitationCode = GenerateSecureInvitationCode(),
            Status = model.SendImmediately ? InvitationStatus.Sent : InvitationStatus.Draft,
            InvitationSentUtc = model.SendImmediately ? DateTime.UtcNow : null
        };

        _context.EventCompanies.Add(invitation);
        await _context.SaveChangesAsync();

        // Create per-item pricing overrides
        foreach (var priceModel in model.AgendaItemPrices)
        {
            var customPrice = CalculateCustomPrice(
                priceModel.BasePrice,
                model.PercentageDiscount,
                priceModel.ManualOverride
            );

            var agendaItemPrice = new EventCompanyAgendaItemPrice
            {
                EventCompanyId = invitation.Id,
                AgendaItemId = priceModel.AgendaItemId,
                CustomPrice = customPrice != priceModel.BasePrice ? customPrice : null
            };

            _context.Set<EventCompanyAgendaItemPrice>().Add(agendaItemPrice);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Send email if requested (fire-and-forget after transaction commit)
        if (model.SendImmediately)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var invitationLink = BuildInvitationLink(invitation.InvitationCode);
                    await _emailSender.SendCompanyInvitationAsync(
                        invitation,
                        evt,
                        model.PersonalMessage ?? string.Empty,
                        invitationLink
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send invitation email for company {CompanyId}",
                        invitation.Id);
                }
            });
        }

        return (true, invitation.Id, null);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Error creating company invitation for event {EventId}", eventId);
        return (false, null, "Ein Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.");
    }
}
```
**Source:** Project pattern from Phase 3 (RegistrationService.cs)

### Blazor Invitation Form with Percentage Discount + Override
```razor
@* Components/Pages/Admin/Events/CompanyInvitationForm.razor *@
<EditForm Model="@Model" OnValidSubmit="@HandleValidSubmit">
    <FluentValidationValidator />
    <ValidationSummary />

    <div class="card mb-3">
        <div class="card-header">
            <h4>Firmendaten</h4>
        </div>
        <div class="card-body">
            <div class="mb-3">
                <label for="companyName" class="form-label">Firmenname *</label>
                <InputText id="companyName" class="form-control" @bind-Value="Model.CompanyName" />
                <ValidationMessage For="@(() => Model.CompanyName)" />
            </div>

            <div class="mb-3">
                <label for="contactEmail" class="form-label">Kontakt-E-Mail *</label>
                <InputText id="contactEmail" type="email" class="form-control" @bind-Value="Model.ContactEmail" />
                <ValidationMessage For="@(() => Model.ContactEmail)" />
            </div>
        </div>
    </div>

    <div class="card mb-3">
        <div class="card-header">
            <h4>Preisgestaltung</h4>
        </div>
        <div class="card-body">
            <div class="mb-3">
                <label for="percentageDiscount" class="form-label">Prozentrabatt (optional)</label>
                <div class="input-group">
                    <InputNumber id="percentageDiscount"
                                 class="form-control"
                                 @bind-Value="Model.PercentageDiscount"
                                 @bind-Value:after="ApplyPercentageDiscount" />
                    <span class="input-group-text">%</span>
                </div>
                <div class="form-text">Wird auf alle Agendapunkte angewendet</div>
                <ValidationMessage For="@(() => Model.PercentageDiscount)" />
            </div>

            <h5>Preise pro Agendapunkt</h5>
            <table class="table table-sm">
                <thead>
                    <tr>
                        <th>Agendapunkt</th>
                        <th>Basispreis</th>
                        <th>Rabattpreis</th>
                        <th>Individueller Preis</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var priceModel in Model.AgendaItemPrices)
                    {
                        <tr>
                            <td>@priceModel.AgendaItemTitle</td>
                            <td>
                                <span class="text-muted">@priceModel.BasePrice.ToString("C")</span>
                            </td>
                            <td>
                                @if (Model.PercentageDiscount.HasValue)
                                {
                                    var discounted = CalculateDiscountedPrice(priceModel.BasePrice, Model.PercentageDiscount.Value);
                                    <span class="text-success">@discounted.ToString("C")</span>
                                }
                                else
                                {
                                    <span class="text-muted">—</span>
                                }
                            </td>
                            <td>
                                <div class="input-group input-group-sm">
                                    <span class="input-group-text">€</span>
                                    <InputNumber class="form-control"
                                                 @bind-Value="priceModel.ManualOverride"
                                                 placeholder="Optional" />
                                </div>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </div>

    <div class="card mb-3">
        <div class="card-header">
            <h4>Persönliche Nachricht (optional)</h4>
        </div>
        <div class="card-body">
            <InputTextArea class="form-control"
                           rows="4"
                           @bind-Value="Model.PersonalMessage"
                           placeholder="Diese Nachricht wird in der Einladungs-E-Mail angezeigt..." />
        </div>
    </div>

    <div class="d-flex gap-2">
        <button type="submit" name="action" value="save-draft" class="btn btn-secondary">
            Als Entwurf speichern
        </button>
        <button type="submit" name="action" value="send" class="btn btn-primary">
            Erstellen & Senden
        </button>
        <button type="button" class="btn btn-outline-secondary" @onclick="Cancel">
            Abbrechen
        </button>
    </div>
</EditForm>

@code {
    private decimal CalculateDiscountedPrice(decimal basePrice, decimal percentage)
    {
        var discount = basePrice * (percentage / 100m);
        return Math.Round(basePrice - discount, 2, MidpointRounding.AwayFromZero);
    }

    private void ApplyPercentageDiscount()
    {
        if (!Model.PercentageDiscount.HasValue) return;

        // Auto-populate manual override fields with discounted prices
        foreach (var priceModel in Model.AgendaItemPrices)
        {
            if (!priceModel.ManualOverride.HasValue) // Don't overwrite manual edits
            {
                priceModel.ManualOverride = CalculateDiscountedPrice(
                    priceModel.BasePrice,
                    Model.PercentageDiscount.Value
                );
            }
        }
    }
}
```
**Source:** Project pattern from Phase 2 (EventForm.razor), user requirements from CONTEXT.md

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Guid.NewGuid() for tokens | RandomNumberGenerator.Fill() with RFC 4122 | .NET Framework → .NET Core | Cryptographically secure GUIDs prevent enumeration attacks |
| EF Core skip navigation only | Explicit join entity with custom properties | EF Core 5.0+ | Supports per-item pricing and metadata on relationships |
| Manual email HTML building | String interpolation with helper methods | Ongoing best practice | Maintainable templates, XSS prevention via encoding |
| Multiple boolean flags for status | Single enum (InvitationStatus) | Modern C# patterns | Type safety, clear state machine, prevents invalid states |
| Fire-and-forget without transaction | Transaction commit before async operations | Blazor Server maturity | Prevents partial data on email failures |

**Deprecated/outdated:**
- System.Net.Mail.SmtpClient: Not cross-platform, use MailKit instead (project already uses MailKit)
- EF Core InMemory provider for tests: Doesn't enforce FK constraints, use SQLite in-memory (project already uses SQLite)

## Open Questions

1. **GUID Expiration Strategy**
   - What we know: Phase 5 requirements mention "GUID expiration and rate limiting"
   - What's unclear: Should invitation codes expire after X days? Should they expire after first use?
   - Recommendation: Phase 4 generates codes only. Phase 5 implements expiration logic (CBOK-01 requirement). Add `ExpiresAtUtc` field to EventCompany for Phase 5 use, leave null in Phase 4.

2. **Batch Invitation Email Throttling**
   - What we know: User wants batch invitation mode for standard pricing
   - What's unclear: Should we throttle email sending (e.g., max 10 emails/minute) to avoid SMTP server limits?
   - Recommendation: Implement simple batch loop in Phase 4. If SMTP throttling needed, add delay between sends (await Task.Delay(100)) and log warnings if > 50 invitations sent.

3. **Invoice Snapshot vs. Live Pricing**
   - What we know: Pricing is always editable, even after booking
   - What's unclear: Should we snapshot pricing at booking time for invoice integrity?
   - Recommendation: Phase 4 allows live edits with warning message. Phase 5 (booking implementation) can add snapshot if needed. Defer decision until Phase 5 planning.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.4.2 with Moq 4.x |
| Config file | none — convention-based discovery |
| Quick run command | `dotnet test --filter "FullyQualifiedName~CompanyInvitation" --no-build` |
| Full suite command | `dotnet test --no-build` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| COMP-01 | Create company invitation with required fields | unit | `dotnet test --filter "FullyQualifiedName~CompanyInvitationServiceTests.CreateInvitationAsync_ValidInput_CreatesInvitation" -x` | ❌ Wave 0 |
| COMP-01 | Prevent duplicate invitation to same company/email | unit | `dotnet test --filter "FullyQualifiedName~CompanyInvitationServiceTests.CreateInvitationAsync_DuplicateEmail_ReturnsError" -x` | ❌ Wave 0 |
| COMP-02 | Store custom price per agenda item | unit | `dotnet test --filter "FullyQualifiedName~CompanyInvitationServiceTests.CreateInvitationAsync_CustomPricing_StoresCorrectly" -x` | ❌ Wave 0 |
| COMP-02 | Calculate percentage discount correctly | unit | `dotnet test --filter "FullyQualifiedName~CompanyInvitationServiceTests.CalculateCustomPrice_WithPercentageDiscount_ReturnsCorrectValue" -x` | ❌ Wave 0 |
| COMP-02 | Manual override takes precedence over percentage | unit | `dotnet test --filter "FullyQualifiedName~CompanyInvitationServiceTests.CalculateCustomPrice_ManualOverrideTakesPrecedence" -x` | ❌ Wave 0 |
| COMP-03 | Generate cryptographically secure GUID | unit | `dotnet test --filter "FullyQualifiedName~CompanyInvitationServiceTests.GenerateSecureInvitationCode_ReturnsValidGuid" -x` | ❌ Wave 0 |
| COMP-03 | Send invitation email with correct content | unit (mock) | `dotnet test --filter "FullyQualifiedName~EmailServiceTests.SendCompanyInvitationAsync_ValidInput_SendsEmail" -x` | ❌ Wave 0 |
| COMP-04 | Delete draft invitation successfully | unit | `dotnet test --filter "FullyQualifiedName~CompanyInvitationServiceTests.DeleteInvitationAsync_DraftStatus_SuccessfullyDeletes" -x` | ❌ Wave 0 |
| COMP-04 | Prevent deletion when bookings exist | unit | `dotnet test --filter "FullyQualifiedName~CompanyInvitationServiceTests.DeleteInvitationAsync_WithBookings_ReturnsError" -x` | ❌ Wave 0 |
| COMP-05 | Update invitation status correctly | unit | `dotnet test --filter "FullyQualifiedName~CompanyInvitationServiceTests.SendInvitationAsync_DraftToSent_UpdatesStatus" -x` | ❌ Wave 0 |
| MAIL-03 | Invitation email includes secure link | unit (mock) | `dotnet test --filter "FullyQualifiedName~EmailServiceTests.SendCompanyInvitationAsync_IncludesSecureLink" -x` | ❌ Wave 0 |
| ALL | FluentValidation rules enforce constraints | unit | `dotnet test --filter "FullyQualifiedName~CompanyInvitationValidatorTests" -x` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~CompanyInvitation" --no-build`
- **Per wave merge:** `dotnet test --no-build` (full suite)
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `EventCenter.Tests/Services/CompanyInvitationServiceTests.cs` — covers COMP-01, COMP-02, COMP-03, COMP-04, COMP-05
- [ ] `EventCenter.Tests/Services/EmailServiceTests.cs` — extend existing file with MAIL-03 tests
- [ ] `EventCenter.Tests/Validators/CompanyInvitationValidatorTests.cs` — covers form validation rules
- [ ] Test fixtures in `EventCenter.Tests/Helpers/` — add EventCompany factory methods to existing TestDbContextFactory pattern

## Sources

### Primary (HIGH confidence)
- [Many-to-many relationships - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many) - EF Core join entity pattern
- [Collections — FluentValidation documentation](https://docs.fluentvalidation.net/en/latest/collections.html) - Nested collection validation with RuleForEach
- [Improve Data Security with Cryptographically Secure Random Generation in .NET](https://ilovedotnet.org/blogs/improve-data-security-with-cryptographically-secure-random-generation-in-dotnet/) - RandomNumberGenerator usage
- Existing project code:
  - `/home/winkler/dev/EventCenter/EventCenter.Web/Services/RegistrationService.cs` - Transaction pattern, fire-and-forget email
  - `/home/winkler/dev/EventCenter/EventCenter.Web/Infrastructure/Email/MailKitEmailSender.cs` - HTML email template pattern
  - `/home/winkler/dev/EventCenter/EventCenter.Web/Data/Configurations/RegistrationAgendaItemConfiguration.cs` - Join entity configuration pattern

### Secondary (MEDIUM confidence)
- [Email-Templating with Blazor - Fusonic](https://www.fusonic.net/en/blog/email-templating-blazor) - Blazor email template best practices
- [Create a cryptographically secure random GUID in .NET](https://iditect.com/faq/csharp/create-a-cryptographically-secure-random-guid-in-net.html) - GUID security patterns
- [Blazor Send Email with MailKit](https://akifmt.github.io/dotnet/2023-09-03-blazor-send-email-with-mailkit/) - MailKit integration patterns

### Tertiary (LOW confidence)
- None - all findings verified with official docs or existing project code

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All libraries already in project, versions verified via .csproj
- Architecture: HIGH - Patterns verified against existing project code (Phase 2-3)
- Pitfalls: MEDIUM-HIGH - Based on project decisions (STATE.md) and general .NET best practices
- Email deliverability: MEDIUM - General best practices, not project-specific configuration

**Research date:** 2026-02-27
**Valid until:** 2026-03-27 (30 days - stable .NET 8 ecosystem)
