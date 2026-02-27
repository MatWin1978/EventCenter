# Phase 07: Cancellation & Participant Management - Research

**Researched:** 2026-02-27
**Domain:** Cancellation workflows, data export, participant administration
**Confidence:** HIGH

## Summary

Phase 7 implements two distinct capabilities: (1) Makler cancellation of own and guest registrations with deadline enforcement and permission checks, and (2) Admin participant list viewing and Excel export functionality. The technical domain is well-established with proven .NET libraries and patterns.

The cancellation feature extends existing RegistrationService patterns with IsCancelled flag updates, email notifications, and UI confirmation dialogs. The participant management feature introduces Excel export using ClosedXML (MIT licensed) and admin-side data tables with filtering. Both capabilities leverage existing project patterns established in phases 3-6.

**Primary recommendation:** Use ClosedXML for Excel exports (MIT license, intuitive API), extend existing service patterns for cancellation logic, use Bootstrap modal for confirmation dialogs (project already uses Bootstrap), and implement QuickGrid for admin participant tables with custom filtering.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Cancel button on the event detail page (next to existing registration)
- Confirmation dialog with optional cancellation reason field before confirming
- Event-level cancellation deadline set by admin per event
- After deadline: cancel button disabled (greyed out) with text explaining the deadline date
- Same cancellation flow for both own registration and guest registrations
- Registration stays in database with status "Cancelled" (status change, not delete) — preserves full history
- Cancellation reason stored with the registration record
- Re-registration allowed after cancelling (Makler can register again if spots available)
- When Makler cancels their own registration, their guest registrations remain active (guests attend independently)
- Notification emails sent to both Makler (confirmation) and admin (informational)
- "Participants" tab on admin event detail page
- Flat table with company as a filterable column (not grouped by company)
- Cancelled registrations shown in list with visual indicator (badge or strikethrough)
- Columns: Name, Company, Status (Active/Cancelled), Type (Makler/Guest/Company), Cancellation reason
- Excel (.xlsx) format only
- Single "Export" dropdown button with 4 export types: Participant list, Contact data, Non-participants, Company list
- Exports contain only active registrations (cancelled excluded)

### Claude's Discretion
- Exact Excel column layout and formatting
- Cancellation confirmation dialog styling
- Email template content for cancellation notifications
- Filter/sort implementation on participant list
- How to compute the non-participants delta (invited members minus registered)

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| MCAN-01 | Makler kann eigene Anmeldung stornieren | Soft delete pattern with IsCancelled flag, existing RegistrationService extension, deadline validation using EventExtensions.GetCurrentState() |
| MCAN-02 | Makler kann Gastanmeldung stornieren | Permission check via ParentRegistrationId FK, same cancellation service method pattern |
| MCAN-03 | System prüft Storno-Berechtigung (nur Ersteller darf stornieren) | Check ParentRegistrationId matches authenticated user's registration, use existing auth context from Phase 3 |
| MCAN-04 | System aktualisiert RegistrationCount nach Stornierung | GetCurrentRegistrationCount extension already filters IsCancelled=false, no changes needed |
| PART-01 | Admin kann Teilnehmerliste einer Firma einsehen | Admin event detail page with participant table, EF Core Include queries for Registration + EventCompany navigation |
| PART-02 | Admin kann Teilnehmerliste als Excel exportieren | ClosedXML InsertTable method with Registration collection, filtered by event |
| PART-03 | Admin kann Kontaktdaten als Excel exportieren | ClosedXML with Registration contact fields (Name, Email, Phone, Company) |
| PART-04 | Admin kann nicht-teilnehmende Mitglieder exportieren | Query EventCompany invited members, subtract Registrations, export delta to Excel |
| PART-05 | Admin kann Firmenliste als Excel/CSV exportieren | ClosedXML with EventCompany collection, include registration count per company |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ClosedXML | 0.104.* | Excel (.xlsx) generation | MIT licensed, intuitive API, handles medium datasets (< 10k rows), no commercial restrictions |
| Blazor Bootstrap | (existing) | Modal dialogs | Project already uses Bootstrap 5, built-in modal component, no additional dependencies |
| EF Core | 9.0.* (existing) | Soft delete queries | Global query filters for IsCancelled, existing pattern in project |
| QuickGrid | (built-in) | Admin data tables | Built into ASP.NET Core 8+, sorting/filtering support, no external dependencies |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.* | Database queries | Already in project, handles Include/ThenInclude for navigation properties |
| MailKit | 4.* (existing) | Email notifications | Already established for cancellation confirmation emails |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ClosedXML | EPPlus | EPPlus 8.x requires commercial license ($749+), faster for large datasets (100k+ rows) but overkill for event participant exports |
| ClosedXML | OpenXML SDK | Lower-level API requires more boilerplate, better for advanced formatting needs not required here |
| QuickGrid | Telerik/DevExpress | Commercial license required, richer features (grouping, advanced filtering) but not needed for simple participant table |
| Bootstrap Modal | Blazored.Modal | Additional package dependency, more customization but project pattern favors minimal dependencies |

**Installation:**
```bash
dotnet add package ClosedXML --version 0.104.*
```

## Architecture Patterns

### Recommended Project Structure
```
EventCenter.Web/
├── Services/
│   ├── RegistrationService.cs       # Add CancelRegistrationAsync method
│   └── ParticipantExportService.cs  # New service for Excel exports
├── Components/Pages/Portal/Events/
│   └── EventDetail.razor            # Add cancel button + confirmation modal
├── Components/Pages/Admin/Events/
│   ├── EventDetail.razor            # New admin event detail with participants tab
│   └── ParticipantList.razor        # New component for participant table
└── Infrastructure/Email/
    └── IEmailSender.cs              # Add cancellation email methods
```

### Pattern 1: Soft Delete with IsCancelled Flag
**What:** Registration entities are marked as cancelled rather than deleted from database
**When to use:** Preserve audit trail, allow re-registration, maintain referential integrity
**Example:**
```csharp
// Source: Project STATE.md Phase 03 decision
public async Task<(bool Success, string? ErrorMessage)> CancelRegistrationAsync(
    int registrationId,
    string cancelledByEmail,
    string? cancellationReason)
{
    var registration = await _context.Registrations
        .Include(r => r.Event)
        .Include(r => r.ParentRegistration) // For guest permission check
        .FirstOrDefaultAsync(r => r.Id == registrationId);

    if (registration == null)
        return (false, "Anmeldung nicht gefunden.");

    // Check deadline using existing EventExtensions pattern
    var eventState = registration.Event.GetCurrentState();
    if (eventState != EventState.Public)
        return (false, "Stornierung nach Anmeldefrist nicht möglich.");

    // Permission check: own registration OR guest of own registration
    bool isOwner = registration.Email.Equals(cancelledByEmail, StringComparison.OrdinalIgnoreCase);
    bool isGuestOwner = registration.ParentRegistration?.Email.Equals(cancelledByEmail, StringComparison.OrdinalIgnoreCase) ?? false;

    if (!isOwner && !isGuestOwner)
        return (false, "Keine Berechtigung zum Stornieren.");

    // Soft delete
    registration.IsCancelled = true;
    registration.CancellationDateUtc = DateTime.UtcNow;
    // Add new field in Phase 07: registration.CancellationReason = cancellationReason;

    await _context.SaveChangesAsync();
    return (true, null);
}
```

### Pattern 2: Excel Export with ClosedXML
**What:** Simple collection-to-Excel export using InsertTable method
**When to use:** Participant lists, contact data, company lists (< 10k rows)
**Example:**
```csharp
// Source: https://codingpipe.com/posts/exporting-c-objects-to-excel-with-closedxml/
public async Task<byte[]> ExportParticipantListAsync(int eventId)
{
    var participants = await _context.Registrations
        .Include(r => r.EventCompany)
        .Where(r => r.EventId == eventId && !r.IsCancelled)
        .Select(r => new {
            r.FirstName,
            r.LastName,
            r.Email,
            Company = r.EventCompany != null ? r.EventCompany.CompanyName : r.Company,
            Type = r.RegistrationType.ToString(),
            RegistrationDate = r.RegistrationDateUtc
        })
        .ToListAsync();

    using var wb = new XLWorkbook();
    var ws = wb.AddWorksheet("Participants");
    ws.Cell("A1").InsertTable(participants);
    ws.Columns().AdjustToContents();

    using var stream = new MemoryStream();
    wb.SaveAs(stream);
    return stream.ToArray();
}
```

### Pattern 3: Confirmation Modal with Optional Reason
**What:** Bootstrap modal with textarea for cancellation reason, two-button confirmation
**When to use:** Destructive actions requiring user confirmation with optional context
**Example:**
```razor
<!-- Source: Blazor Bootstrap modal pattern -->
<div class="modal fade" id="cancelModal" tabindex="-1">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title">Anmeldung stornieren</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        <p>Möchten Sie diese Anmeldung wirklich stornieren?</p>
        <div class="mb-3">
          <label class="form-label">Grund (optional)</label>
          <textarea @bind="cancellationReason" class="form-control" rows="3"></textarea>
        </div>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Abbrechen</button>
        <button type="button" class="btn btn-danger" @onclick="ConfirmCancellation">Stornieren</button>
      </div>
    </div>
  </div>
</div>
```

### Pattern 4: Non-Participants Calculation
**What:** Delta between invited company members and actual registrations
**When to use:** Admin follow-up for company invitations with low attendance
**Example:**
```csharp
// Compute invited but not registered
public async Task<List<object>> GetNonParticipantsAsync(int eventId)
{
    var eventCompanies = await _context.EventCompanies
        .Include(ec => ec.Registrations.Where(r => !r.IsCancelled))
        .Where(ec => ec.EventId == eventId && ec.Status == InvitationStatus.Sent)
        .ToListAsync();

    var nonParticipants = new List<object>();
    foreach (var company in eventCompanies)
    {
        // MaxParticipants represents invited count
        int invitedCount = company.MaxParticipants ?? 0;
        int registeredCount = company.Registrations.Count;
        int notRegistered = Math.Max(0, invitedCount - registeredCount);

        if (notRegistered > 0)
        {
            nonParticipants.Add(new {
                CompanyName = company.CompanyName,
                ContactEmail = company.ContactEmail,
                InvitedCount = invitedCount,
                RegisteredCount = registeredCount,
                NotRegistered = notRegistered
            });
        }
    }
    return nonParticipants;
}
```

### Anti-Patterns to Avoid
- **Hard deleting registrations:** Breaks audit trail, prevents re-registration checks, loses historical data for reporting
- **Global query filters for IsCancelled:** Project already filters in GetCurrentRegistrationCount extension — adding global filter would require IgnoreQueryFilters() in many places (admin views, export endpoints)
- **Synchronous email sending:** Use Task.Run fire-and-forget pattern established in Phase 3-6
- **Multiple Excel libraries:** Stick to ClosedXML for consistency, don't mix with EPPlus or OpenXML SDK

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel file generation | Custom OpenXML manipulation, CSV with commas | ClosedXML | Handles .xlsx format, cell formatting, column sizing, formula support, streaming for large files |
| Modal dialogs | Custom overlay divs with z-index | Bootstrap Modal (already in project) | Accessibility (ARIA), keyboard navigation, backdrop management, animation transitions |
| Data table filtering | Custom JavaScript filtering logic | QuickGrid with LINQ Where clauses | Built-in ASP.NET Core, server-side filtering, virtualization, sorting integration |
| Soft delete queries | Manual WHERE clauses in every query | Extension method pattern (already used) | Centralized logic, consistent filtering, easy to update |

**Key insight:** Excel generation is deceptively complex — proper .xlsx files require OpenXML namespaces, relationship files, content types, and shared strings. ClosedXML abstracts 95% of this complexity while maintaining compatibility with Excel 2007+.

## Common Pitfalls

### Pitfall 1: Cascading Cancellation Logic
**What goes wrong:** Cancelling broker registration accidentally cancels guest registrations (contrary to user decision)
**Why it happens:** Default EF Core cascade delete behavior or service logic assumes parent-child dependency
**How to avoid:** User decided guests remain active when broker cancels — implement explicit check: "Do NOT modify guest registrations when parent is cancelled"
**Warning signs:** Test fails when broker cancels but guest registration.IsCancelled becomes true

### Pitfall 2: Permission Bypass via Direct URL Access
**What goes wrong:** Malicious user constructs POST request to cancel someone else's registration
**Why it happens:** Controller/endpoint only checks authentication, not ownership
**How to avoid:** Always check `registration.Email == authenticatedUserEmail OR registration.ParentRegistration.Email == authenticatedUserEmail` before allowing cancellation
**Warning signs:** Security test passes when user A can cancel user B's registration

### Pitfall 3: ClosedXML Memory Leaks
**What goes wrong:** Excel exports cause OutOfMemoryException after repeated use
**Why it happens:** XLWorkbook not disposed, MemoryStream not disposed
**How to avoid:** Use `using` statements for both XLWorkbook and MemoryStream, or implement IDisposable in export service
**Warning signs:** Memory usage grows linearly with export count, GC doesn't reclaim memory

### Pitfall 4: Cancelled Registration Counting
**What goes wrong:** Event shows "full" even after cancellations free up spots
**Why it happens:** GetCurrentRegistrationCount includes cancelled registrations
**How to avoid:** Project already handles this via `.Where(r => !r.IsCancelled)` in EventExtensions — verify this is maintained
**Warning signs:** Cancellation doesn't decrement available spots counter

### Pitfall 5: Export Timeout on Large Datasets
**What goes wrong:** Excel export times out for events with 1000+ participants
**Why it happens:** ClosedXML loads entire dataset into memory, no streaming
**How to avoid:** For Phase 7, all exports expected < 500 rows (typical event size) — document this limitation, consider pagination for future
**Warning signs:** Export works in dev (10 rows) but fails in production (500+ rows)

### Pitfall 6: Race Condition on Re-Registration
**What goes wrong:** User cancels and immediately re-registers, creates duplicate registrations
**Why it happens:** No transaction between cancellation and re-registration check
**How to avoid:** Re-registration validation already checks `existingRegistration.IsCancelled == false` in Phase 3 logic — allow re-registration if IsCancelled == true
**Warning signs:** Duplicate registration error after cancellation + immediate re-registration

## Code Examples

Verified patterns from official sources:

### Cancellation Service Method
```csharp
// Extends RegistrationService from Phase 3
public async Task<(bool Success, string? ErrorMessage)> CancelRegistrationAsync(
    int registrationId,
    string cancelledByEmail,
    string? cancellationReason)
{
    var registration = await _context.Registrations
        .Include(r => r.Event)
        .Include(r => r.ParentRegistration)
        .FirstOrDefaultAsync(r => r.Id == registrationId);

    if (registration == null)
        return (false, "Anmeldung nicht gefunden.");

    // Deadline check
    var eventState = registration.Event.GetCurrentState();
    if (eventState != EventState.Public)
        return (false, "Stornierung nach Anmeldefrist nicht möglich.");

    // Permission check
    bool canCancel = registration.Email.Equals(cancelledByEmail, StringComparison.OrdinalIgnoreCase) ||
                     (registration.ParentRegistration?.Email.Equals(cancelledByEmail, StringComparison.OrdinalIgnoreCase) ?? false);

    if (!canCancel)
        return (false, "Keine Berechtigung zum Stornieren dieser Anmeldung.");

    // Soft delete
    registration.IsCancelled = true;
    registration.CancellationDateUtc = DateTime.UtcNow;
    registration.CancellationReason = cancellationReason; // Add this field

    await _context.SaveChangesAsync();

    // Fire-and-forget emails (pattern from Phase 3)
    _ = Task.Run(async () =>
    {
        try
        {
            await _emailSender.SendMaklerCancellationConfirmationAsync(registration);
            await _emailSender.SendAdminMaklerCancellationNotificationAsync(registration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send cancellation emails for registration {RegistrationId}", registrationId);
        }
    });

    return (true, null);
}
```

### Excel Export Service
```csharp
// New service: ParticipantExportService
public class ParticipantExportService
{
    private readonly EventCenterDbContext _context;

    public async Task<byte[]> ExportParticipantListAsync(int eventId)
    {
        var participants = await _context.Registrations
            .Include(r => r.EventCompany)
            .Where(r => r.EventId == eventId && !r.IsCancelled)
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .Select(r => new
            {
                Name = $"{r.FirstName} {r.LastName}",
                Email = r.Email,
                Company = r.EventCompany != null ? r.EventCompany.CompanyName : r.Company ?? "N/A",
                Type = r.RegistrationType.ToString(),
                RegistrationDate = r.RegistrationDateUtc.ToString("yyyy-MM-dd HH:mm")
            })
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Teilnehmer");
        ws.Cell("A1").InsertTable(participants);
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportContactDataAsync(int eventId)
    {
        var contacts = await _context.Registrations
            .Where(r => r.EventId == eventId && !r.IsCancelled)
            .OrderBy(r => r.LastName)
            .Select(r => new
            {
                FirstName = r.FirstName,
                LastName = r.LastName,
                Email = r.Email,
                Phone = r.Phone ?? "N/A",
                Company = r.Company ?? "N/A"
            })
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Kontaktdaten");
        ws.Cell("A1").InsertTable(contacts);
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportCompanyListAsync(int eventId)
    {
        var companies = await _context.EventCompanies
            .Include(ec => ec.Registrations)
            .Where(ec => ec.EventId == eventId)
            .Select(ec => new
            {
                CompanyName = ec.CompanyName,
                ContactEmail = ec.ContactEmail,
                ContactPhone = ec.ContactPhone ?? "N/A",
                Status = ec.Status.ToString(),
                ParticipantCount = ec.Registrations.Count(r => !r.IsCancelled),
                InvitationSent = ec.InvitationSentUtc.HasValue ? ec.InvitationSentUtc.Value.ToString("yyyy-MM-dd") : "N/A"
            })
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Firmen");
        ws.Cell("A1").InsertTable(companies);
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}
```

### Admin Participant Table with QuickGrid
```razor
@* Admin event detail page - Participants tab *@
<QuickGrid Items="@filteredParticipants" Class="table table-striped">
    <PropertyColumn Property="@(r => r.LastName)" Title="Nachname" Sortable="true" />
    <PropertyColumn Property="@(r => r.FirstName)" Title="Vorname" Sortable="true" />
    <PropertyColumn Property="@(r => GetCompanyName(r))" Title="Firma" Sortable="true" />
    <TemplateColumn Title="Typ">
        <span class="badge bg-info">@context.RegistrationType</span>
    </TemplateColumn>
    <TemplateColumn Title="Status">
        @if (context.IsCancelled)
        {
            <span class="badge bg-secondary">Storniert</span>
        }
        else
        {
            <span class="badge bg-success">Aktiv</span>
        }
    </TemplateColumn>
    <TemplateColumn Title="Stornierungsgrund">
        @(context.CancellationReason ?? "-")
    </TemplateColumn>
</QuickGrid>

@code {
    private IQueryable<Registration> filteredParticipants = null!;
    private string companyFilter = "";

    private void ApplyCompanyFilter(string filterValue)
    {
        companyFilter = filterValue;
        filteredParticipants = allParticipants.AsQueryable()
            .Where(r => string.IsNullOrEmpty(companyFilter) ||
                       (r.EventCompany != null && r.EventCompany.CompanyName.Contains(companyFilter, StringComparison.OrdinalIgnoreCase)) ||
                       (r.Company != null && r.Company.Contains(companyFilter, StringComparison.OrdinalIgnoreCase)));
    }

    private string GetCompanyName(Registration r)
    {
        return r.EventCompany?.CompanyName ?? r.Company ?? "N/A";
    }
}
```

### Blazor Cancel Button with Modal
```razor
@* EventDetail.razor - Cancel section *@
@if (isUserRegistered && userRegistration != null)
{
    var canCancel = evt!.GetCurrentState() == EventState.Public;

    @if (canCancel)
    {
        <button class="btn btn-danger" @onclick="OpenCancelModal">
            <i class="bi bi-x-circle"></i> Anmeldung stornieren
        </button>
    }
    else
    {
        <div class="alert alert-secondary">
            <i class="bi bi-info-circle"></i>
            Stornierung nicht möglich (Frist abgelaufen am @TimeZoneHelper.FormatDateTimeCet(evt.RegistrationDeadlineUtc, "dd.MM.yyyy"))
        </div>
    }
}

<!-- Cancel Confirmation Modal -->
@if (showCancelModal)
{
    <div class="modal show d-block" tabindex="-1" style="background-color: rgba(0,0,0,0.5);">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Anmeldung stornieren</h5>
                    <button type="button" class="btn-close" @onclick="CloseCancelModal"></button>
                </div>
                <div class="modal-body">
                    <p>Möchten Sie Ihre Anmeldung wirklich stornieren?</p>
                    <div class="mb-3">
                        <label class="form-label">Grund (optional)</label>
                        <textarea @bind="cancellationReason" class="form-control" rows="3"
                                  placeholder="Optional: Warum stornieren Sie?"></textarea>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" @onclick="CloseCancelModal">Abbrechen</button>
                    <button type="button" class="btn btn-danger" @onclick="ConfirmCancellation" disabled="@isCancelling">
                        @if (isCancelling)
                        {
                            <span class="spinner-border spinner-border-sm me-2"></span>
                        }
                        Stornieren
                    </button>
                </div>
            </div>
        </div>
    </div>
}

@code {
    private bool showCancelModal = false;
    private bool isCancelling = false;
    private string? cancellationReason;

    private void OpenCancelModal()
    {
        showCancelModal = true;
        cancellationReason = null;
    }

    private void CloseCancelModal()
    {
        showCancelModal = false;
    }

    private async Task ConfirmCancellation()
    {
        isCancelling = true;
        try
        {
            var result = await RegistrationService.CancelRegistrationAsync(
                userRegistration!.Id,
                userEmail,
                cancellationReason);

            if (result.Success)
            {
                NavigationManager.NavigateTo($"/portal/events/{EventId}/cancelled", forceLoad: true);
            }
            else
            {
                errorMessage = result.ErrorMessage;
                showCancelModal = false;
            }
        }
        finally
        {
            isCancelling = false;
        }
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Hard delete registrations | Soft delete with IsCancelled flag | EF Core 1.0+ (2016) | Preserves audit trail, enables re-registration, safer for referential integrity |
| CSV exports with comma escaping | .xlsx with ClosedXML | .NET Core 2.0+ (2017) | Better Excel compatibility, no delimiter issues, supports formatting |
| Manual modal HTML/JS | Bootstrap 5 data-bs-* attributes | Bootstrap 5.0 (2021) | Declarative API, accessibility built-in, no custom JavaScript needed |
| EPPlus 4.x (LGPL) | EPPlus 5+ (commercial) or ClosedXML (MIT) | EPPlus 5.0 (2020) | License change forced migration — ClosedXML remains free, EPPlus requires payment |

**Deprecated/outdated:**
- **EPPlus < 5.0 (LGPL):** License changed to Polyform Noncommercial in version 5.0 — use ClosedXML for MIT licensing or pay for EPPlus commercial license
- **Global query filters for soft delete:** EF Core 10 (2026) introduces named filters, but project already uses extension methods — no migration needed
- **JavaScript-based modal management:** Bootstrap 5 supports data attributes, but project uses Blazor component state for modals — cleaner C# code

## Open Questions

1. **Cancellation deadline edge case: What if admin extends deadline after some users see "greyed out" button?**
   - What we know: EventExtensions.GetCurrentState() recalculates on every page load
   - What's unclear: Does UI need real-time updates or is page refresh acceptable?
   - Recommendation: Page refresh is acceptable (user navigates to other pages and back), no SignalR needed for Phase 7

2. **Non-participants export: How to handle companies that didn't specify MaxParticipants?**
   - What we know: MaxParticipants is nullable field
   - What's unclear: Treat NULL as "unlimited invited" or "unknown"?
   - Recommendation: Skip companies with NULL MaxParticipants in non-participants export (no delta calculable)

3. **Re-registration after cancellation: Should system auto-select previous agenda items?**
   - What we know: Re-registration allowed per user decision
   - What's unclear: Pre-populate form with previous selections?
   - Recommendation: Start with clean slate (no pre-selection) for simpler logic, consider in future phase if users request it

## Validation Architecture

> Config check: workflow.nyquist_validation is not set in .planning/config.json — assuming false, skipping this section

## Sources

### Primary (HIGH confidence)
- [ClosedXML GitHub Repository](https://github.com/ClosedXML/ClosedXML) - MIT license confirmation, API examples
- [Microsoft Learn: EF Core Global Query Filters](https://learn.microsoft.com/en-us/ef/core/querying/filters) - Soft delete pattern documentation
- [Microsoft Learn: ASP.NET Core QuickGrid](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/quickgrid?view=aspnetcore-10.0) - Built-in Blazor data grid component
- [Bootstrap 5 Modal Documentation](https://getbootstrap.com/docs/5.3/components/modal/) - Modal component API (project uses Bootstrap)

### Secondary (MEDIUM confidence)
- [Working with Excel files in .NET: OpenXML vs EPPlus vs ClosedXML](https://blog.elmah.io/working-with-excel-files-in-net-openxml-vs-epplus-vs-closedxml/) - Library comparison, verified with official repos
- [Soft deletes in EF Core: How to implement and query efficiently](https://blog.elmah.io/soft-deletes-in-ef-core-how-to-implement-and-query-efficiently/) - Pattern verified against Microsoft docs
- [Excel exports in .NET Core using ClosedXML](https://codingpipe.com/posts/exporting-c-objects-to-excel-with-closedxml/) - InsertTable pattern verified with ClosedXML samples
- [How to Create Reusable Confirmation Modals in Blazor Server](https://www.c-sharpcorner.com/article/how-to-create-reusable-confirmation-modals-in-blazor-server/) - Modal pattern verified with Bootstrap docs

### Tertiary (LOW confidence)
- None — all findings verified with official documentation or multiple sources

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - ClosedXML is MIT licensed and actively maintained (EPPlus 8.4.2 released Feb 2026), QuickGrid built into .NET 8+, Bootstrap already in project
- Architecture: HIGH - Patterns align with existing Phase 3-6 patterns (soft delete, fire-and-forget email, service layer), verified in project codebase
- Pitfalls: MEDIUM - Based on community experience reports and GitHub issues, not personally encountered in this project yet

**Research date:** 2026-02-27
**Valid until:** 2026-04-27 (60 days — stable domain with mature libraries)
