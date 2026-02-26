---
phase: 03-makler-event-discovery-registration
plan: 01
subsystem: domain-model-and-contracts
tags: [domain-model, ef-core, nuget, email, calendar, validation]
dependency_graph:
  requires: [phase-02-domain-model]
  provides: [registration-domain-model, email-contract, calendar-contract, registration-validator]
  affects: [EventCenterDbContext, Event, Registration]
tech_stack:
  added: [MailKit-4.15.0, Ical.Net-5.2.1]
  patterns: [optimistic-concurrency, many-to-many-join-table, fluent-validation]
key_files:
  created:
    - EventCenter.Web/Domain/Entities/RegistrationAgendaItem.cs
    - EventCenter.Web/Data/Configurations/RegistrationAgendaItemConfiguration.cs
    - EventCenter.Web/Infrastructure/Email/IEmailSender.cs
    - EventCenter.Web/Infrastructure/Email/SmtpSettings.cs
    - EventCenter.Web/Infrastructure/Calendar/ICalendarExportService.cs
    - EventCenter.Web/Models/RegistrationFormModel.cs
    - EventCenter.Web/Validators/RegistrationValidator.cs
    - EventCenter.Web/Data/Migrations/20260226175258_AddPhase03RegistrationFields.cs
  modified:
    - EventCenter.Web/Domain/Entities/Event.cs
    - EventCenter.Web/Domain/Entities/Registration.cs
    - EventCenter.Web/Domain/EventCenterDbContext.cs
    - EventCenter.Web/Data/Configurations/EventConfiguration.cs
    - EventCenter.Web/Data/Configurations/RegistrationConfiguration.cs
    - EventCenter.Web/EventCenter.Web.csproj
decisions:
  - Use RowVersion on Event entity for optimistic concurrency during registration
  - Create explicit join table entity (RegistrationAgendaItem) for many-to-many relationship
  - Add IsCancelled and CancellationDateUtc fields to Registration entity for future Phase 7 use
  - Use MailKit for SMTP email sending (industry standard, cross-platform)
  - Use Ical.Net for RFC 5545-compliant iCalendar export
  - Create DTO (RegistrationFormModel) for form validation instead of validating entity directly
metrics:
  duration: 199
  completed_date: "2026-02-26"
  tasks_completed: 2
  files_created: 8
  files_modified: 6
  lines_added: 709
  commits: 2
---

# Phase 03 Plan 01: Domain Model and Service Contracts Summary

**One-liner:** Extended domain model with optimistic concurrency, many-to-many registration-agenda relationship, installed MailKit/Ical.NET packages, and created service interfaces for email and calendar functionality.

## What Was Built

### Domain Model Extensions

**Event Entity:**
- Added `RowVersion` property with `[Timestamp]` attribute for optimistic concurrency control
- Configured as row version in EventConfiguration to detect concurrent registration attempts
- Prevents race conditions when multiple brokers register for limited capacity events

**Registration Entity:**
- Added `IsCancelled` boolean field (default false) for future cancellation feature (Phase 7)
- Added `CancellationDateUtc` nullable DateTime field to track when registration was cancelled
- Added `RegistrationAgendaItems` navigation property for many-to-many relationship with agenda items

**RegistrationAgendaItem Join Table:**
- Created explicit join table entity with composite primary key (RegistrationId, AgendaItemId)
- Configured cascade delete from Registration side
- Configured NoAction delete from AgendaItem side to prevent cascade cycles
- Supports brokers selecting specific agenda items during registration

### Infrastructure Contracts

**Email Service Interface (IEmailSender):**
- `SendRegistrationConfirmationAsync(Registration)` method for sending confirmation emails
- Abstraction allows testing with mocks and production implementation with MailKit
- SmtpSettings class created for dependency injection configuration

**Calendar Export Interface (ICalendarExportService):**
- `GenerateEventCalendar(Event)` method returns byte array of .ics file content
- Abstraction for RFC 5545-compliant iCalendar generation
- Implementation will use Ical.NET library (Plan 03)

### Validation

**RegistrationValidator:**
- Validates RegistrationFormModel DTO (not entity directly)
- German error messages following project convention
- Rules: FirstName/LastName required (max 100 chars), Email required and valid format, EventId > 0, SelectedAgendaItemIds not empty
- Follows existing FluentValidation patterns from Phase 02

### NuGet Packages Installed

**MailKit 4.15.0:**
- SMTP email sending library (recommended by Microsoft as System.Net.Mail.SmtpClient replacement)
- Dependencies: MimeKit 4.15.0, BouncyCastle.Cryptography 2.6.2, System.Security.Cryptography.Pkcs 8.0.1
- Cross-platform, async support, modern authentication (OAuth2, STARTTLS)

**Ical.Net 5.2.1:**
- RFC 5545-compliant iCalendar generation library
- Dependencies: NodaTime 3.2.2 for timezone handling
- v5 rewrite with performance improvements and .NET 8 compatibility

### Database Migration

**AddPhase03RegistrationFields Migration:**
- Adds Event.RowVersion column (rowversion type in SQL Server)
- Adds Registration.IsCancelled (bit, default false) and CancellationDateUtc (datetime2, nullable)
- Creates RegistrationAgendaItems table with composite primary key and foreign keys
- All existing data preserved, new fields nullable or default values

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

**Build Status:** SUCCESS
- EventCenter.Web builds with no errors
- Only pre-existing warnings (async methods without await in auth components)

**Test Status:** ALL PASS
- 45 existing tests still passing (no regressions)
- EventService, EventValidator, EventAgendaItemValidator tests unaffected
- Database configuration changes properly applied via migration

**Package Status:** VERIFIED
- MailKit 4.* resolved to 4.15.0 (latest stable)
- Ical.Net 5.* resolved to 5.2.1 (latest stable)
- No vulnerability warnings
- All dependencies compatible with .NET 8.0

**Must-Haves Verification:**
- ✅ Registration entity supports optimistic concurrency via RowVersion on Event
- ✅ Registration has many-to-many relationship with EventAgendaItem via RegistrationAgendaItem join table
- ✅ Registration includes IsCancelled and CancellationDateUtc for future-proofing
- ✅ IEmailSender interface exists for dependency injection and testability
- ✅ ICalendarExportService interface exists for iCalendar generation
- ✅ RegistrationValidator validates required fields with German error messages

**Artifact Verification:**
- ✅ EventCenter.Web/Domain/Entities/RegistrationAgendaItem.cs contains RegistrationId and AgendaItemId
- ✅ EventCenter.Web/Infrastructure/Email/IEmailSender.cs contains SendRegistrationConfirmationAsync method
- ✅ EventCenter.Web/Infrastructure/Calendar/ICalendarExportService.cs contains GenerateEventCalendar method
- ✅ EventCenter.Web/Validators/RegistrationValidator.cs inherits from AbstractValidator<RegistrationFormModel>

**Key-Links Verification:**
- ✅ EventCenter.Web/Domain/EventCenterDbContext.cs has DbSet<RegistrationAgendaItem> property
- ✅ EventCenter.Web/Domain/Entities/Event.cs has [Timestamp] attribute on RowVersion property

## Self-Check: PASSED

**Created files verification:**
```
FOUND: EventCenter.Web/Domain/Entities/RegistrationAgendaItem.cs
FOUND: EventCenter.Web/Data/Configurations/RegistrationAgendaItemConfiguration.cs
FOUND: EventCenter.Web/Infrastructure/Email/IEmailSender.cs
FOUND: EventCenter.Web/Infrastructure/Email/SmtpSettings.cs
FOUND: EventCenter.Web/Infrastructure/Calendar/ICalendarExportService.cs
FOUND: EventCenter.Web/Models/RegistrationFormModel.cs
FOUND: EventCenter.Web/Validators/RegistrationValidator.cs
FOUND: EventCenter.Web/Data/Migrations/20260226175258_AddPhase03RegistrationFields.cs
```

**Commits verification:**
```
FOUND: e2124f2 (Task 1 - domain model and packages)
FOUND: 1c974d1 (Task 2 - service interfaces and validator)
```

All claimed files exist and commits are in git history.

## Technical Notes

### Optimistic Concurrency Pattern

The `RowVersion` property on Event uses SQL Server's `rowversion` type (previously called `timestamp`). When EF Core loads an Event entity, it captures the current RowVersion value. On SaveChanges, EF Core includes the original RowVersion in the WHERE clause:

```sql
UPDATE Events SET ... WHERE Id = @p0 AND RowVersion = @p1
```

If another transaction modified the Event between load and save, the RowVersion won't match and no rows are updated, triggering `DbUpdateConcurrencyException`. RegistrationService (Plan 02) will catch this exception and return a user-friendly error message.

### Many-to-Many Join Table Strategy

While EF Core 5+ supports implicit many-to-many relationships, this plan creates an explicit `RegistrationAgendaItem` entity for two reasons:

1. **SQLite compatibility:** Explicit join tables are easier to configure for test database constraints
2. **Future extensibility:** If we need to add properties to the relationship (e.g., attendance status, check-in time), the entity already exists

The composite primary key (RegistrationId, AgendaItemId) automatically creates a unique index preventing duplicate selections.

### Cancellation Fields Design

The `IsCancelled` and `CancellationDateUtc` fields are added now (Phase 3) even though cancellation logic is deferred to Phase 7. This avoids:

1. Additional migration in Phase 7
2. Potential data migration complexity if registrations already exist
3. Breaking changes to RegistrationService signature

Default value of `false` for `IsCancelled` ensures existing queries don't need modification.

## Downstream Impact

### Plan 02 (RegistrationService)

- Can use `Event.RowVersion` for concurrency detection
- Must include Event with `.Include()` to load RowVersion
- Must wrap registration creation in transaction with try-catch for `DbUpdateConcurrencyException`
- Can populate `RegistrationAgendaItems` collection after creating Registration

### Plan 03 (Email and Calendar Services)

- Must implement `IEmailSender` interface (MailKitEmailSender class)
- Must implement `ICalendarExportService` interface (IcalNetCalendarService class)
- SmtpSettings configuration needed in appsettings.json and Program.cs DI registration
- MailKit and Ical.NET packages already available

### Plan 04 (Event List/Detail Pages)

- Can use `RegistrationValidator` for client-side validation with Blazored.FluentValidation
- EventDetail page can link to calendar export endpoint
- Event list can check `Event.Registrations.Count >= Event.MaxCapacity` for status badges

### Plan 05 (Registration Form)

- Must bind to `RegistrationFormModel` DTO for validation
- Can use `RegistrationValidator` with `<FluentValidationValidator />`
- Form submission calls RegistrationService.RegisterMaklerAsync with selected agenda item IDs

## Success Criteria Met

1. ✅ Domain model ready for registration service implementation (RowVersion, join table, cancellation fields)
2. ✅ Service interfaces defined as contracts for Plan 02 and Plan 03
3. ✅ NuGet packages (MailKit, Ical.NET) installed
4. ✅ RegistrationValidator ready for UI form validation
5. ✅ All existing tests pass (no regressions)
6. ✅ EF Core migration created

## Next Steps

**Plan 02:** Implement RegistrationService with business logic
- Check capacity and deadline before registration
- Use optimistic concurrency with Event.RowVersion
- Create Registration and RegistrationAgendaItem entries in transaction
- Call IEmailSender.SendRegistrationConfirmationAsync after commit

**Plan 03:** Implement Email and Calendar services
- MailKitEmailSender with HTML email templates
- IcalNetCalendarService with UTC timezone handling
- Configure SmtpSettings in appsettings.json and Program.cs

**Plan 04:** Build Event List and Detail pages
- Card grid layout with search/filter
- Status badges (Plätze frei, Angemeldet, Ausgebucht, Verpasst)
- Sticky sidebar with register button and iCal download
- Document download with FileStreamResult

**Plan 05:** Build Registration Form page
- Single-page flow with agenda item selection
- Cost summary calculation
- Confirmation modal before submission
- Success page with iCal download button

---

*Plan executed: 2026-02-26*
*Duration: 3m 19s (199 seconds)*
*Commits: e2124f2, 1c974d1*
