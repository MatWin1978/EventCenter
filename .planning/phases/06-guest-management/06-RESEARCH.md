# Phase 6: Guest Management - Research

**Researched:** 2026-02-27
**Domain:** Guest/companion registration, inline forms, limit validation, email notifications
**Confidence:** HIGH

## Summary

Phase 6 enables brokers to register companions (guests) for events they're attending. The research validates that the existing architecture (Blazor Server, RegistrationService pattern, MailKit email) requires minimal extension. The key technical pattern is reusing the `Registration` entity with `RegistrationType.Guest` rather than creating a separate entity, leveraging the existing registration flow with appropriate modifications for guest-specific fields.

Critical considerations: parent-child relationship tracking (guest linked to broker's registration), limit enforcement via `Event.MaxCompanions` validation, inline form UX patterns for single-guest-at-a-time entry, and cost display using `EventAgendaItem.CostForGuest` pricing.

The project already has established patterns for inline forms (Phase 5's CompanyBooking uses similar patterns), service layer validation (RegistrationService), and email notifications (MailKit with IEmailSender), which can be directly extended for guest registration.

**Primary recommendation:** Extend `Registration` entity with `ParentRegistrationId` foreign key to link guest to broker, add `RelationshipType` field for guest relationship tracking, extend RegistrationService with `RegisterGuestAsync()` method following the existing `RegisterMaklerAsync()` pattern, implement limit validation before form display, and use inline collapsible form pattern similar to existing detail page sections.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Registration flow:**
- Guest registration lives on the event detail page (not a separate page)
- "Begleitperson anmelden" button expands an inline form section below the broker's own registration
- One guest at a time — form collapses after submission, button reappears for next guest
- After successful registration: simple success message ("Begleitperson erfolgreich angemeldet"), form collapses, page refreshes to show updated state
- Only registered brokers see the guest registration section (prerequisite: broker must be registered themselves)

**Guest data entry:**
- Required fields: Anrede, Vorname, Nachname, E-Mail, Beziehungstyp
- Address replaced by E-Mail (deviation from GREG-03 — address not needed, email more useful for communication)
- Beziehungstyp is free text (not a dropdown)
- Guest selects their own agenda items from the available list (with CompanionParticipationCost pricing)
- Costs shown during registration as the broker selects agenda items — no surprises

**Guest listing & costs:**
- "Meine Begleitpersonen" section on event detail page, below the broker's own registration
- Each guest row shows their name, agenda item costs (CompanionParticipationCost)
- Total cost for all guests shown at bottom
- No remove/cancel button on guest list — cancellation handled in Phase 7

**Limit enforcement:**
- "Begleitpersonen: 1/2" counter visible near the registration button so broker always knows remaining slots
- When limit reached: button disabled (grayed out), text below: "Maximale Anzahl Begleitpersonen erreicht (2/2)"
- If MaxCompanions = 0: guest section hidden entirely — no guest-related UI shown
- If broker not registered: guest section hidden entirely

### Claude's Discretion

- Exact form layout and field ordering
- Success message styling (toast vs inline alert)
- Agenda item display format within the guest form
- Loading states during registration submission

### Deferred Ideas (OUT OF SCOPE)

- Guest cancellation/removal — Phase 7 (Cancellation & Participant Management)

</user_constraints>

<phase_requirements>
## Phase Requirements

This phase must address the following requirements from REQUIREMENTS.md:

| ID | Description | Research Support |
|----|-------------|-----------------|
| GREG-01 | Makler kann Begleitperson für Veranstaltung anmelden | Registration entity with RegistrationType.Guest, ParentRegistrationId FK to link to broker's registration, RegistrationService.RegisterGuestAsync() method following existing RegisterMaklerAsync() pattern |
| GREG-02 | System prüft Begleitpersonenlimit pro Makler | Query existing guest count via `Registrations.Count(r => r.ParentRegistrationId == brokerRegId && !r.IsCancelled)`, compare against `Event.MaxCompanions`, validate before showing registration form |
| GREG-03 | Makler gibt Gast-Daten ein (Anrede, Name, Adresse, Beziehungstyp) | Modified per CONTEXT.md: Replace "Adresse" with "E-Mail". Add Salutation (Anrede) enum or string field, RelationshipType string field. Use RegistrationFormModel with FluentValidation |
| MAIL-02 | System sendet Bestätigung an Makler nach Gastanmeldung | Extend IEmailSender with `SendGuestRegistrationConfirmationAsync(Registration guestReg, Registration brokerReg)`, use MailKit SmtpClient with HTML template, fire-and-forget pattern after SaveChangesAsync() |

</phase_requirements>

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core Blazor | 8.0 | UI framework | Already in use, server rendering for direct DB access, inline form patterns proven in Phase 5 |
| Entity Framework Core | 9.0 | Data access | Already configured, extend Registration entity with self-referencing FK for parent-child relationship |
| FluentValidation | 11.* | Validation | Already integrated, reuse RegistrationValidator with conditional rules for guest vs broker registration types |
| Blazored.FluentValidation | 2.* | Blazor validation integration | Already in project for EventForm/RegistrationForm, same pattern for guest form |
| MailKit | 4.15+ | Email sending | Already in use for broker registration emails, extend IEmailSender for guest confirmation emails |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap 5 | 5.x | UI styling | Project standard, collapse component for inline form, badge for limit counter |
| TimeZoneHelper | Custom | Timezone handling | Already in project for CET conversions, use for displaying guest agenda item times |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Reusing Registration entity | Separate Companion/Guest entity | Separate entity adds schema complexity, requires duplicate registration flow logic. Reusing Registration with discriminator (RegistrationType.Guest) leverages existing patterns and keeps schema normalized. |
| Inline form | Separate guest registration page | Inline form keeps broker context visible (event details, broker's own registration), reduces navigation friction. Separate page would require passing event context between pages. |
| Fire-and-forget email | Synchronous email sending | Synchronous blocks user flow if SMTP slow/down. Fire-and-forget pattern (Task.Run with try-catch) already proven in Phase 3/4/5, maintains non-blocking UX. |

**Installation:**

No new packages required — all dependencies already installed in previous phases.

## Architecture Patterns

### Recommended Project Structure

```
EventCenter.Web/
├── Domain/Entities/
│   └── Registration.cs              # EXTEND with ParentRegistrationId, RelationshipType, Salutation
├── Domain/Enums/
│   └── Salutation.cs                # NEW - Herr, Frau, Divers (or use string field)
├── Services/
│   └── RegistrationService.cs       # EXTEND with RegisterGuestAsync()
├── Infrastructure/Email/
│   ├── IEmailSender.cs              # EXTEND with SendGuestRegistrationConfirmationAsync()
│   └── MailKitEmailSender.cs        # IMPLEMENT guest confirmation email template
├── Validators/
│   └── RegistrationValidator.cs     # EXTEND with guest-specific validation rules
├── Models/
│   └── GuestRegistrationFormModel.cs # NEW - DTO for guest form (or extend RegistrationFormModel)
└── Components/Pages/Portal/Events/
    └── EventDetail.razor            # EXTEND with guest registration section, list, and counter
```

### Pattern 1: Self-Referencing Foreign Key for Parent-Child Relationship

**What:** Guest registrations link to their broker's registration via `ParentRegistrationId` foreign key on the same `Registration` table.

**When to use:** One entity type has parent-child relationship (broker registration is parent, guest registration is child).

**Example:**

```csharp
// Source: EF Core documentation on self-referencing relationships
public class Registration
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int? EventCompanyId { get; set; }
    public RegistrationType RegistrationType { get; set; } // Makler, Guest, CompanyParticipant

    // Existing fields
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }

    // NEW for Phase 6: Guest fields
    public int? ParentRegistrationId { get; set; }  // NULL for broker/company, FK to broker for guest
    public string? Salutation { get; set; }         // "Herr", "Frau", "Divers" (or use enum)
    public string? RelationshipType { get; set; }   // "Ehepartner", "Geschäftspartner", etc.

    // Existing temporal fields
    public DateTime RegistrationDateUtc { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancellationDateUtc { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public EventCompany? EventCompany { get; set; }
    public Registration? ParentRegistration { get; set; }      // NEW: broker registration
    public ICollection<Registration> GuestRegistrations { get; set; } = new List<Registration>(); // NEW: broker's guests
    public ICollection<RegistrationAgendaItem> RegistrationAgendaItems { get; set; } = new List<RegistrationAgendaItem>();
}

// EF Core configuration
public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        // Self-referencing relationship
        builder.HasOne(r => r.ParentRegistration)
            .WithMany(r => r.GuestRegistrations)
            .HasForeignKey(r => r.ParentRegistrationId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete of guests if broker registration deleted

        // ParentRegistrationId must be NULL for Makler/CompanyParticipant types
        // and NOT NULL for Guest type (enforced in validation, not DB constraint)
    }
}
```

### Pattern 2: Inline Collapsible Form with State Management

**What:** Form section that expands/collapses via button click, using Bootstrap collapse component with Blazor state binding.

**When to use:** Adding child entities (guests) inline without navigation, maintaining parent context visibility.

**Example:**

```razor
@* Source: Bootstrap 5 collapse + Blazor state management pattern *@
@* EventDetail.razor - Guest Registration Section *@

@if (isUserRegistered && evt.MaxCompanions > 0)
{
    <div class="card mb-4">
        <div class="card-header d-flex justify-content-between align-items-center">
            <h5 class="mb-0">Begleitpersonen</h5>
            <span class="badge bg-secondary">
                @currentGuestCount / @evt.MaxCompanions
            </span>
        </div>
        <div class="card-body">
            @* Guest List *@
            @if (userGuestRegistrations.Any())
            {
                <div class="mb-3">
                    <h6>Meine Begleitpersonen</h6>
                    <ul class="list-group">
                        @foreach (var guest in userGuestRegistrations)
                        {
                            <li class="list-group-item d-flex justify-content-between align-items-start">
                                <div>
                                    <strong>@guest.FirstName @guest.LastName</strong>
                                    <br/>
                                    <small class="text-muted">@guest.RelationshipType</small>
                                </div>
                                <div class="text-end">
                                    @{
                                        var guestCost = guest.RegistrationAgendaItems
                                            .Sum(rai => rai.AgendaItem.CostForGuest);
                                    }
                                    <span class="badge bg-primary">@guestCost.ToString("N2") EUR</span>
                                </div>
                            </li>
                        }
                    </ul>
                    <div class="mt-2 text-end">
                        <strong>Gesamtkosten Begleitpersonen:</strong>
                        <span class="text-primary">@totalGuestCost.ToString("N2") EUR</span>
                    </div>
                </div>
            }

            @* Add Guest Button *@
            @if (currentGuestCount < evt.MaxCompanions)
            {
                @if (!showGuestForm)
                {
                    <button type="button" class="btn btn-primary" @onclick="() => showGuestForm = true">
                        <i class="bi bi-person-plus"></i> Begleitperson anmelden
                    </button>
                }
                else
                {
                    @* Inline Guest Registration Form *@
                    <div class="border p-3 rounded">
                        <h6>Neue Begleitperson anmelden</h6>
                        <EditForm Model="guestFormModel" OnValidSubmit="HandleGuestRegistrationAsync">
                            <FluentValidationValidator />

                            <div class="row">
                                <div class="col-md-4 mb-3">
                                    <label class="form-label">Anrede *</label>
                                    <InputSelect @bind-Value="guestFormModel.Salutation" class="form-select">
                                        <option value="">Bitte wählen</option>
                                        <option value="Herr">Herr</option>
                                        <option value="Frau">Frau</option>
                                        <option value="Divers">Divers</option>
                                    </InputSelect>
                                    <ValidationMessage For="() => guestFormModel.Salutation" />
                                </div>
                                <div class="col-md-4 mb-3">
                                    <label class="form-label">Vorname *</label>
                                    <InputText @bind-Value="guestFormModel.FirstName" class="form-control" />
                                    <ValidationMessage For="() => guestFormModel.FirstName" />
                                </div>
                                <div class="col-md-4 mb-3">
                                    <label class="form-label">Nachname *</label>
                                    <InputText @bind-Value="guestFormModel.LastName" class="form-control" />
                                    <ValidationMessage For="() => guestFormModel.LastName" />
                                </div>
                            </div>

                            <div class="row">
                                <div class="col-md-6 mb-3">
                                    <label class="form-label">E-Mail *</label>
                                    <InputText @bind-Value="guestFormModel.Email" type="email" class="form-control" />
                                    <ValidationMessage For="() => guestFormModel.Email" />
                                </div>
                                <div class="col-md-6 mb-3">
                                    <label class="form-label">Beziehungstyp *</label>
                                    <InputText @bind-Value="guestFormModel.RelationshipType" class="form-control"
                                               placeholder="z.B. Ehepartner, Kollege" />
                                    <ValidationMessage For="() => guestFormModel.RelationshipType" />
                                </div>
                            </div>

                            @* Agenda item selection with guest pricing *@
                            <div class="mb-3">
                                <label class="form-label">Agendapunkte für Begleitperson *</label>
                                @foreach (var item in evt.AgendaItems.Where(ai => ai.GuestsCanParticipate))
                                {
                                    <div class="form-check">
                                        <InputCheckbox class="form-check-input"
                                                       Value="item.Id"
                                                       @bind-Value="@(IsAgendaItemSelected(item.Id))" />
                                        <label class="form-check-label">
                                            @item.Title
                                            @if (item.CostForGuest > 0)
                                            {
                                                <span class="badge bg-primary ms-2">@item.CostForGuest.ToString("N2") EUR</span>
                                            }
                                            else
                                            {
                                                <span class="badge bg-success ms-2">Kostenfrei</span>
                                            }
                                        </label>
                                    </div>
                                }
                            </div>

                            <div class="d-flex justify-content-between align-items-center">
                                <button type="button" class="btn btn-secondary" @onclick="CancelGuestForm">
                                    Abbrechen
                                </button>
                                <div>
                                    <span class="me-3">
                                        <strong>Kosten:</strong> @selectedGuestCost.ToString("N2") EUR
                                    </span>
                                    <button type="submit" class="btn btn-primary" disabled="@isSubmittingGuest">
                                        @if (isSubmittingGuest)
                                        {
                                            <span class="spinner-border spinner-border-sm me-2"></span>
                                        }
                                        Begleitperson anmelden
                                    </button>
                                </div>
                            </div>
                        </EditForm>
                    </div>
                }
            }
            else
            {
                <div class="alert alert-warning mb-0">
                    <i class="bi bi-exclamation-triangle"></i>
                    Maximale Anzahl Begleitpersonen erreicht (@evt.MaxCompanions/@evt.MaxCompanions)
                </div>
            }

            @* Success Message *@
            @if (!string.IsNullOrEmpty(guestSuccessMessage))
            {
                <div class="alert alert-success mt-3">
                    <i class="bi bi-check-circle"></i> @guestSuccessMessage
                </div>
            }
        </div>
    </div>
}

@code {
    private bool showGuestForm = false;
    private bool isSubmittingGuest = false;
    private string? guestSuccessMessage;
    private int currentGuestCount = 0;
    private decimal totalGuestCost = 0;
    private decimal selectedGuestCost = 0;
    private List<Registration> userGuestRegistrations = new();
    private GuestRegistrationFormModel guestFormModel = new();

    private void CancelGuestForm()
    {
        showGuestForm = false;
        guestFormModel = new();
        guestSuccessMessage = null;
    }

    private async Task HandleGuestRegistrationAsync()
    {
        isSubmittingGuest = true;
        guestSuccessMessage = null;

        var result = await RegistrationService.RegisterGuestAsync(
            userBrokerRegistrationId,
            guestFormModel.Salutation!,
            guestFormModel.FirstName,
            guestFormModel.LastName,
            guestFormModel.Email,
            guestFormModel.RelationshipType!,
            guestFormModel.SelectedAgendaItemIds
        );

        if (result.Success)
        {
            guestSuccessMessage = "Begleitperson erfolgreich angemeldet.";
            showGuestForm = false;
            guestFormModel = new();

            // Refresh page data to show updated guest list
            await LoadEventDataAsync();
        }
        else
        {
            // Show error (could use alert or validation message)
        }

        isSubmittingGuest = false;
    }
}
```

### Pattern 3: Guest Registration Service Method

**What:** Dedicated service method for guest registration that validates limit, creates guest registration entity, and sends confirmation email.

**When to use:** Complex business logic (limit checks, parent-child linking, guest-specific pricing) extracted from UI.

**Example:**

```csharp
// Source: Existing RegistrationService.RegisterMaklerAsync() pattern + guest-specific logic
public class RegistrationService
{
    // ... existing fields and RegisterMaklerAsync() ...

    /// <summary>
    /// Registers a guest/companion for an event under a broker's registration.
    /// Validates companion limit and creates guest registration with parent link.
    /// </summary>
    public async Task<(bool Success, int? GuestRegistrationId, string? ErrorMessage)> RegisterGuestAsync(
        int brokerRegistrationId,
        string salutation,
        string firstName,
        string lastName,
        string email,
        string relationshipType,
        List<int> selectedAgendaItemIds)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Load broker registration with event
            var brokerRegistration = await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.Event.AgendaItems)
                .FirstOrDefaultAsync(r => r.Id == brokerRegistrationId && !r.IsCancelled);

            if (brokerRegistration == null)
            {
                return (false, null, "Makler-Anmeldung nicht gefunden.");
            }

            if (brokerRegistration.RegistrationType != RegistrationType.Makler)
            {
                return (false, null, "Nur Makler können Begleitpersonen anmelden.");
            }

            var evt = brokerRegistration.Event;

            // 2. Validate event state
            var eventState = evt.GetCurrentState();
            if (eventState != EventState.Public)
            {
                return (false, null, "Anmeldung nicht möglich - Frist abgelaufen.");
            }

            // 3. Check companion limit
            var currentGuestCount = await _context.Registrations
                .CountAsync(r => r.ParentRegistrationId == brokerRegistrationId && !r.IsCancelled);

            if (currentGuestCount >= evt.MaxCompanions)
            {
                return (false, null, $"Maximale Anzahl Begleitpersonen erreicht ({evt.MaxCompanions}).");
            }

            // 4. Validate selected agenda items (guests only allowed in items where GuestsCanParticipate = true)
            if (selectedAgendaItemIds.Any())
            {
                var validAgendaItemIds = evt.AgendaItems
                    .Where(ai => ai.GuestsCanParticipate)
                    .Select(ai => ai.Id)
                    .ToHashSet();

                if (!selectedAgendaItemIds.All(id => validAgendaItemIds.Contains(id)))
                {
                    return (false, null, "Ungültige Agendapunkt-Auswahl für Begleitperson.");
                }
            }

            // 5. Create guest registration
            var guestRegistration = new Registration
            {
                EventId = evt.Id,
                ParentRegistrationId = brokerRegistrationId,
                RegistrationType = RegistrationType.Guest,
                Salutation = salutation,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                RelationshipType = relationshipType,
                IsConfirmed = true,
                RegistrationDateUtc = DateTime.UtcNow,
                IsCancelled = false
            };

            _context.Registrations.Add(guestRegistration);
            await _context.SaveChangesAsync();

            // 6. Create registration-agenda item links
            foreach (var agendaItemId in selectedAgendaItemIds)
            {
                var registrationAgendaItem = new RegistrationAgendaItem
                {
                    RegistrationId = guestRegistration.Id,
                    AgendaItemId = agendaItemId
                };
                _context.Set<RegistrationAgendaItem>().Add(registrationAgendaItem);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // 7. Send confirmation email (fire-and-forget with error handling)
            _ = Task.Run(async () =>
            {
                try
                {
                    var guestWithDetails = await GetRegistrationWithDetailsAsync(guestRegistration.Id);
                    if (guestWithDetails != null)
                    {
                        await _emailSender.SendGuestRegistrationConfirmationAsync(
                            guestWithDetails,
                            brokerRegistration
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send guest registration confirmation email for registration {RegistrationId}", guestRegistration.Id);
                }
            });

            return (true, guestRegistration.Id, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during guest registration for broker registration {BrokerRegistrationId}", brokerRegistrationId);
            return (false, null, "Ein Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.");
        }
    }

    /// <summary>
    /// Gets current guest count for a broker's registration.
    /// </summary>
    public async Task<int> GetGuestCountAsync(int brokerRegistrationId)
    {
        return await _context.Registrations
            .CountAsync(r => r.ParentRegistrationId == brokerRegistrationId && !r.IsCancelled);
    }

    /// <summary>
    /// Gets all guest registrations for a broker.
    /// </summary>
    public async Task<List<Registration>> GetGuestRegistrationsAsync(int brokerRegistrationId)
    {
        return await _context.Registrations
            .Include(r => r.RegistrationAgendaItems)
                .ThenInclude(rai => rai.AgendaItem)
            .Where(r => r.ParentRegistrationId == brokerRegistrationId && !r.IsCancelled)
            .OrderBy(r => r.RegistrationDateUtc)
            .ToListAsync();
    }
}
```

### Pattern 4: Guest-Specific Email Template

**What:** Separate email template method for guest confirmation that includes broker context and guest-specific pricing.

**When to use:** Guest notification differs from broker notification (includes relationship info, references broker, uses CostForGuest pricing).

**Example:**

```csharp
// Source: Existing MailKitEmailSender pattern + guest-specific template
public class MailKitEmailSender : IEmailSender
{
    // ... existing SendRegistrationConfirmationAsync() ...

    public async Task SendGuestRegistrationConfirmationAsync(
        Registration guestRegistration,
        Registration brokerRegistration)
    {
        var evt = guestRegistration.Event;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(new MailboxAddress(
            $"{guestRegistration.FirstName} {guestRegistration.LastName}",
            guestRegistration.Email));
        message.Subject = $"Anmeldebestätigung Begleitperson: {evt.Title}";

        var totalCost = guestRegistration.RegistrationAgendaItems
            .Sum(rai => rai.AgendaItem.CostForGuest);

        var htmlBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Anmeldebestätigung - Begleitperson</h2>

                <p>Sehr geehrte/r {guestRegistration.Salutation} {guestRegistration.LastName},</p>

                <p>Ihre Anmeldung als Begleitperson von <strong>{brokerRegistration.FirstName} {brokerRegistration.LastName}</strong> für die Veranstaltung wurde erfolgreich registriert.</p>

                <h3>Veranstaltungsdetails</h3>
                <ul>
                    <li><strong>Titel:</strong> {evt.Title}</li>
                    <li><strong>Datum:</strong> {TimeZoneHelper.FormatDateTimeCet(evt.StartDateUtc, ""dd.MM.yyyy HH:mm"")} Uhr</li>
                    <li><strong>Ort:</strong> {evt.Location}</li>
                    <li><strong>Beziehung:</strong> {guestRegistration.RelationshipType}</li>
                </ul>

                <h3>Ihre gewählten Agendapunkte</h3>
                <table style='width: 100%; border-collapse: collapse;'>
                    <thead>
                        <tr style='background-color: #f0f0f0;'>
                            <th style='border: 1px solid #ddd; padding: 8px; text-align: left;'>Agendapunkt</th>
                            <th style='border: 1px solid #ddd; padding: 8px; text-align: left;'>Zeit</th>
                            <th style='border: 1px solid #ddd; padding: 8px; text-align: right;'>Kosten</th>
                        </tr>
                    </thead>
                    <tbody>
                        {string.Join("", guestRegistration.RegistrationAgendaItems.Select(rai => $@"
                        <tr>
                            <td style='border: 1px solid #ddd; padding: 8px;'>{rai.AgendaItem.Title}</td>
                            <td style='border: 1px solid #ddd; padding: 8px;'>{TimeZoneHelper.FormatDateTimeCet(rai.AgendaItem.StartDateTimeUtc, "dd.MM.yyyy HH:mm")}</td>
                            <td style='border: 1px solid #ddd; padding: 8px; text-align: right;'>{rai.AgendaItem.CostForGuest:N2} EUR</td>
                        </tr>"))}
                    </tbody>
                    <tfoot>
                        <tr style='font-weight: bold;'>
                            <td colspan='2' style='border: 1px solid #ddd; padding: 8px; text-align: right;'>Gesamtkosten:</td>
                            <td style='border: 1px solid #ddd; padding: 8px; text-align: right;'>{totalCost:N2} EUR</td>
                        </tr>
                    </tfoot>
                </table>

                <p>Bei Fragen kontaktieren Sie uns gerne.</p>

                <p>Mit freundlichen Grüßen,<br/>
                {_settings.SenderName}</p>
            </body>
            </html>";

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl);
        await client.AuthenticateAsync(_settings.Username, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Parent-child entity relationships | Custom guest tracking system with separate tables and manual linking | EF Core self-referencing FK (`ParentRegistrationId`) on Registration entity | Self-referencing FK is standard EF Core pattern, maintains referential integrity, simplifies queries (Include navigation property), prevents orphaned records |
| Guest limit validation | Client-side only counter | Service-layer validation + DB query for current count before insert | Client-side can be bypassed, concurrent requests can exceed limit. Server-side validation with transaction ensures data integrity |
| Email templating | String concatenation for HTML | Structured template methods with type-safe parameters | String concat error-prone (missing closing tags, XSS risk with unescaped user input), template methods provide consistent structure and easier maintenance |
| Cost calculation | Manual sum in UI each time | Calculated property or service method with LINQ Sum() | Duplicate calculation logic creates maintenance burden, LINQ Sum() over navigation properties is type-safe and consistent |

**Key insight:** Guest management is a specialized case of the existing registration flow. Reusing the Registration entity with discriminator (RegistrationType.Guest) and self-referencing FK avoids premature abstraction while maintaining data integrity and leveraging existing service patterns.

## Common Pitfalls

### Pitfall 1: Separate Guest Entity Creating Duplicate Logic

**What goes wrong:** Creating a separate `Companion` or `Guest` entity with its own table forces duplication of registration flow logic, validation rules, and email templates.

**Why it happens:** Initial assumption that different entity types require separate tables, or misunderstanding of entity inheritance/discriminator patterns.

**How to avoid:** Use single Registration table with `RegistrationType` discriminator and conditional fields (e.g., `ParentRegistrationId` NULL for broker, NOT NULL for guest). Extend existing RegistrationService with guest-specific method rather than creating separate CompanionService.

**Warning signs:** Finding yourself copying RegisterMaklerAsync() logic into RegisterCompanionAsync() with minor changes, creating separate validators with 90% identical rules, maintaining parallel email templates.

### Pitfall 2: Not Enforcing Limit at Service Layer

**What goes wrong:** Checking guest limit in UI only allows concurrent requests to exceed limit if two brokers submit simultaneously, or malicious users bypass client-side validation.

**Why it happens:** Trusting client-side state, not using transactions for multi-step operations (check limit → insert guest).

**How to avoid:** Always query current guest count within transaction before insert, validate against MaxCompanions at service layer. Use database transaction to ensure atomic check-and-insert.

**Warning signs:** Guest count exceeding MaxCompanions in production data, race condition bugs reported during high-traffic events, relying on UI-disabled state without server validation.

### Pitfall 3: Cascade Delete Deleting Guests When Broker Cancels

**What goes wrong:** If `ParentRegistrationId` FK configured with `OnDelete(DeleteBehavior.Cascade)`, canceling broker registration automatically deletes all guest registrations, losing historical data.

**Why it happens:** Default EF Core cascade delete behavior, not considering audit/history requirements.

**How to avoid:** Use `OnDelete(DeleteBehavior.Restrict)` on ParentRegistrationId FK. Handle broker cancellation logic separately (e.g., automatically cancel guests via service method that sets IsCancelled = true rather than deleting).

**Warning signs:** Guest registrations disappearing from database when broker cancels, no audit trail of who was registered, inability to generate historical participation reports.

### Pitfall 4: Mixing Guest and Broker Pricing

**What goes wrong:** Displaying CostForMakler instead of CostForGuest in guest agenda selection, or vice versa, causing cost confusion and invoicing errors.

**Why it happens:** Reusing same agenda item display component for both registration types without pricing context.

**How to avoid:** Pass registration type or pricing context to agenda item components. Use separate computed properties or methods that select correct cost field based on RegistrationType.

**Warning signs:** Guest costs shown as broker costs in UI, email confirmations showing wrong pricing, financial reconciliation mismatches.

### Pitfall 5: Showing Guest Section When Broker Not Registered

**What goes wrong:** Broker sees guest registration form but can't submit because broker registration doesn't exist yet, creating confusing UX.

**Why it happens:** Not checking broker's own registration status before rendering guest section.

**How to avoid:** Query broker's registration for current event before rendering guest section. Hide entire guest section if broker not registered. Display "Bitte melden Sie sich zuerst selbst an" message if needed.

**Warning signs:** Validation errors on guest submit saying "broker registration not found", confusion reported by brokers, support tickets about "guest form not working".

## Code Examples

Verified patterns from existing codebase and official sources:

### Query Guest Count with Limit Check

```csharp
// Source: Existing RegistrationService pattern + EF Core queries
public async Task<bool> CanRegisterGuestAsync(int brokerRegistrationId, int eventId)
{
    var evt = await _context.Events.FindAsync(eventId);
    if (evt == null || evt.MaxCompanions == 0)
        return false;

    var currentGuestCount = await _context.Registrations
        .CountAsync(r => r.ParentRegistrationId == brokerRegistrationId
                      && !r.IsCancelled);

    return currentGuestCount < evt.MaxCompanions;
}
```

### FluentValidation for Guest Registration Form

```csharp
// Source: Existing RegistrationValidator pattern + conditional rules
public class GuestRegistrationValidator : AbstractValidator<GuestRegistrationFormModel>
{
    public GuestRegistrationValidator()
    {
        RuleFor(g => g.Salutation)
            .NotEmpty().WithMessage("Anrede ist erforderlich.")
            .Must(s => new[] { "Herr", "Frau", "Divers" }.Contains(s))
            .WithMessage("Ungültige Anrede.");

        RuleFor(g => g.FirstName)
            .NotEmpty().WithMessage("Vorname ist erforderlich.")
            .MaximumLength(100).WithMessage("Vorname darf maximal 100 Zeichen lang sein.");

        RuleFor(g => g.LastName)
            .NotEmpty().WithMessage("Nachname ist erforderlich.")
            .MaximumLength(100).WithMessage("Nachname darf maximal 100 Zeichen lang sein.");

        RuleFor(g => g.Email)
            .NotEmpty().WithMessage("E-Mail ist erforderlich.")
            .EmailAddress().WithMessage("Gültige E-Mail-Adresse ist erforderlich.");

        RuleFor(g => g.RelationshipType)
            .NotEmpty().WithMessage("Beziehungstyp ist erforderlich.")
            .MaximumLength(100).WithMessage("Beziehungstyp darf maximal 100 Zeichen lang sein.");

        RuleFor(g => g.SelectedAgendaItemIds)
            .NotEmpty().WithMessage("Bitte wählen Sie mindestens einen Agendapunkt aus.");
    }
}
```

### Load Broker's Guest Registrations with Costs

```csharp
// Source: Existing GetRegistrationWithDetailsAsync() pattern + navigation properties
public async Task<List<Registration>> GetBrokerGuestsWithCostsAsync(int brokerRegistrationId)
{
    return await _context.Registrations
        .Include(r => r.RegistrationAgendaItems)
            .ThenInclude(rai => rai.AgendaItem)
        .Where(r => r.ParentRegistrationId == brokerRegistrationId
                 && !r.IsCancelled)
        .OrderBy(r => r.RegistrationDateUtc)
        .ToListAsync();
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Separate Companion/Guest table | Single Registration table with RegistrationType discriminator | EF Core 3.0+ (2019) improved table-per-hierarchy support | Reduces schema complexity, reuses validation/service logic, simplifies queries |
| Synchronous email sending in transaction | Fire-and-forget async email after commit | Modern async/await patterns (2015+) | Non-blocking UX, prevents SMTP timeouts from failing registration transaction |
| Client-side only validation | Server-side validation with transaction | Security best practices (always) | Prevents limit bypass via concurrent requests or client manipulation |
| Manual HTML string building | Template methods with type-safe parameters | Modern C# string interpolation + BodyBuilder | Reduces XSS risk, improves maintainability |

**Deprecated/outdated:**
- Separate tables for each registration type (CompanyParticipant, Broker, Guest) — use single table with discriminator
- Cascade delete on parent-child relationships without considering audit trail — use Restrict and soft delete (IsCancelled flag)

## Open Questions

1. **Should Salutation be enum or string field?**
   - What we know: CONTEXT.md mentions "Anrede" as required field, common values are Herr/Frau/Divers
   - What's unclear: Whether to enforce enum (strict validation) or allow free text (flexibility for edge cases like "Herr Dr.")
   - Recommendation: Use string field with suggested values in UI (input with datalist) for flexibility, add validation for max length (50 chars)

2. **Should guest email confirmation also go to broker?**
   - What we know: MAIL-02 requires confirmation to broker (makler), unclear if guest also receives separate email
   - What's unclear: CONTEXT.md and requirements don't specify if guest gets their own email or only broker is notified
   - Recommendation: Send email to both guest (primary recipient) and CC broker, allows guest to have their own confirmation while keeping broker informed. Update IEmailSender signature accordingly.

3. **What happens to guests when broker registration is cancelled?**
   - What we know: Phase 6 scope excludes cancellation (Phase 7), but need to design schema to support it
   - What's unclear: Should guests be auto-cancelled when broker cancels, or can they attend independently?
   - Recommendation: Phase 7 concern, but use Restrict delete behavior now to preserve flexibility. Document decision in Phase 7 research.

## Validation Architecture

> Configuration check: workflow.nyquist_validation = false (per .planning/config.json), this section is informational only

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.4.2 + bUnit 1.* + EF Core SQLite (in-memory) |
| Config file | EventCenter.Tests/EventCenter.Tests.csproj (existing) |
| Quick run command | `dotnet test --filter "FullyQualifiedName~GuestRegistration" --no-build` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| GREG-01 | Register guest for event under broker registration | unit (service) | `dotnet test --filter "FullyQualifiedName~RegistrationServiceTests.RegisterGuestAsync_ValidGuest_ReturnsSuccess" --no-build` | ❌ Wave 0 |
| GREG-02 | Reject guest registration when limit reached | unit (service) | `dotnet test --filter "FullyQualifiedName~RegistrationServiceTests.RegisterGuestAsync_LimitReached_ReturnsError" --no-build` | ❌ Wave 0 |
| GREG-02 | Query current guest count for broker | unit (service) | `dotnet test --filter "FullyQualifiedName~RegistrationServiceTests.GetGuestCountAsync_ReturnsCorrectCount" --no-build` | ❌ Wave 0 |
| GREG-03 | Validate guest form model (all required fields) | unit (validator) | `dotnet test --filter "FullyQualifiedName~GuestRegistrationValidatorTests" --no-build` | ❌ Wave 0 |
| GREG-03 | Guest agenda items use CostForGuest pricing | unit (service) | `dotnet test --filter "FullyQualifiedName~RegistrationServiceTests.RegisterGuestAsync_ValidGuest_UsesGuestPricing" --no-build` | ❌ Wave 0 |
| MAIL-02 | Send confirmation email after guest registration | unit (service) | `dotnet test --filter "FullyQualifiedName~RegistrationServiceTests.RegisterGuestAsync_Success_SendsEmail" --no-build` | ❌ Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet test --filter "FullyQualifiedName~GuestRegistration" --no-build` (guest-related tests only, ~5-10s)
- **Per wave merge:** `dotnet test --no-build` (full suite, ~30-60s depending on test count)
- **Phase gate:** Full suite green + manual verification of guest registration flow in UI before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `EventCenter.Tests/Services/RegistrationServiceTests.cs` — extend with RegisterGuestAsync tests (GREG-01, GREG-02, MAIL-02)
- [ ] `EventCenter.Tests/Validators/GuestRegistrationValidatorTests.cs` — NEW file, covers GREG-03 field validation
- [ ] `EventCenter.Tests/Helpers/TestDbContextFactory.cs` — VERIFY supports self-referencing FK (should already work with SQLite)
- [ ] Mock IEmailSender in test setup to verify SendGuestRegistrationConfirmationAsync called with correct parameters

*(Framework already installed, no new packages needed)*

## Sources

### Primary (HIGH confidence)

- **Existing codebase patterns:**
  - `/home/winkler/dev/EventCenter/EventCenter.Web/Domain/Entities/Registration.cs` — RegistrationType.Guest already defined, entity structure verified
  - `/home/winkler/dev/EventCenter/EventCenter.Web/Services/RegistrationService.cs` — RegisterMaklerAsync() pattern to replicate for guests
  - `/home/winkler/dev/EventCenter/EventCenter.Web/Domain/Entities/Event.cs` — MaxCompanions field verified (line 15)
  - `/home/winkler/dev/EventCenter/EventCenter.Web/Domain/Entities/EventAgendaItem.cs` — CostForGuest field verified (line 12)
  - `/home/winkler/dev/EventCenter/EventCenter.Web/Infrastructure/Email/IEmailSender.cs` — Email service interface pattern
  - Phase 3 research (.planning/phases/03-makler-event-discovery-registration/03-RESEARCH.md) — Registration flow patterns, transaction boundaries, email fire-and-forget

- **EF Core documentation:**
  - Microsoft Learn: "Relationships - Self-referencing" (https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many#self-referencing) — Self-referencing FK configuration
  - Microsoft Learn: "DbContext Lifetime, Configuration, and Initialization" — Transaction scope patterns

- **FluentValidation documentation:**
  - Official docs v11.x: "Built-in Validators" — Email, NotEmpty, MaximumLength validators
  - Blazored.FluentValidation GitHub: Integration with Blazor EditForm

### Secondary (MEDIUM confidence)

- **Bootstrap 5 documentation:**
  - Official docs: "Collapse" component — Inline collapsible form pattern
  - Official docs: "Forms" — Form validation styling with is-invalid classes

- **Blazor documentation:**
  - Microsoft Learn: "ASP.NET Core Blazor forms" — EditForm, InputText, InputCheckbox components
  - Microsoft Learn: "ASP.NET Core Blazor component lifecycle" — State management in components

### Tertiary (LOW confidence)

None — all research grounded in existing codebase patterns or official documentation

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — All libraries already in use, no new dependencies required
- Architecture: HIGH — Self-referencing FK is standard EF Core pattern, inline form pattern proven in Phase 5 (CompanyBooking)
- Pitfalls: HIGH — Common issues identified from EF Core relationship patterns and existing project decisions (soft delete via IsCancelled)

**Research date:** 2026-02-27
**Valid until:** 2026-03-29 (30 days for stable stack, .NET 8/EF Core 9 not changing rapidly)
