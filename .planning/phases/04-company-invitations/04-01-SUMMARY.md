---
phase: 04-company-invitations
plan: 01
subsystem: domain-model
tags: [entity-framework, domain-entities, validation, email-templates, mailkit, fluentvalidation]

# Dependency graph
requires:
  - phase: 03-makler-event-discovery-registration
    provides: Email infrastructure (MailKitEmailSender), Event/EventAgendaItem entities, validation patterns
provides:
  - InvitationStatus enum for invitation lifecycle tracking
  - EventCompany entity extended with invitation fields
  - EventCompanyAgendaItemPrice join entity for per-item pricing overrides
  - CompanyInvitationFormModel and validators for form input
  - SendCompanyInvitationAsync email template method
  - Database migration for Phase 04 schema changes
affects: [04-02-company-invitation-service, 04-03-admin-company-invitation-ui]

# Tech tracking
tech-stack:
  added: []
  patterns: [join-entity-composite-key, enum-string-conversion, per-entity-pricing-override, html-email-templates]

key-files:
  created:
    - EventCenter.Web/Domain/Enums/InvitationStatus.cs
    - EventCenter.Web/Domain/Entities/EventCompanyAgendaItemPrice.cs
    - EventCenter.Web/Data/Configurations/EventCompanyAgendaItemPriceConfiguration.cs
    - EventCenter.Web/Models/CompanyInvitationFormModel.cs
    - EventCenter.Web/Models/CompanyAgendaItemPriceModel.cs
    - EventCenter.Web/Validators/CompanyInvitationValidator.cs
    - EventCenter.Web/Data/Migrations/20260227081013_AddPhase04CompanyInvitationFields.cs
  modified:
    - EventCenter.Web/Domain/Entities/EventCompany.cs
    - EventCenter.Web/Data/Configurations/EventCompanyConfiguration.cs
    - EventCenter.Web/Domain/EventCenterDbContext.cs
    - EventCenter.Web/Infrastructure/Email/MailKitEmailSender.cs

key-decisions:
  - "Store InvitationStatus as string in database for readability (following existing enum pattern)"
  - "Use composite primary key (EventCompanyId, AgendaItemId) for EventCompanyAgendaItemPrice join entity"
  - "CustomPrice nullable - null indicates use base price from EventAgendaItem.CostForMakler"
  - "Add unique filtered index on InvitationCode to prevent duplicate codes (when not null)"
  - "Include ExpiresAtUtc field now for Phase 5 GUID expiration (leave null until needed)"
  - "Email template shows base price (CostForMakler) vs custom price in table format for transparency"
  - "Personal message displayed with blue accent (#007bff) matching brand colors"

patterns-established:
  - "Join entities with composite keys follow pattern: HasKey(p => new { p.Entity1Id, p.Entity2Id })"
  - "Nested validators use SetValidator(new NestedValidator()) for collection validation"
  - "Email templates use German culture formatting for currency (de-DE)"
  - "HTML emails maintain consistent structure: header banner, event card, content sections, CTA button, footer"

requirements-completed: [COMP-01, COMP-02, COMP-05, MAIL-03]

# Metrics
duration: 4.3min
completed: 2026-02-27
---

# Phase 04 Plan 01: Domain Model & Email Infrastructure Summary

**Company invitation domain model with per-item pricing overrides, status lifecycle (Draft/Sent/Booked/Cancelled), FluentValidation rules, and HTML invitation email template**

## Performance

- **Duration:** 4.3 min (258 seconds)
- **Started:** 2026-02-27T08:08:57Z
- **Completed:** 2026-02-27T08:13:15Z
- **Tasks:** 2 (combined into single commit due to tight coupling)
- **Files modified:** 11

## Accomplishments
- Extended EventCompany entity with invitation lifecycle fields (Status, PercentageDiscount, PersonalMessage, ExpiresAtUtc)
- Created EventCompanyAgendaItemPrice join entity for per-item pricing customization
- Implemented FluentValidation rules with German error messages for all invitation form inputs
- Built HTML email template with personal message section, pricing summary table, and CTA button
- Generated EF Core migration adding all Phase 04 schema changes

## Task Commits

Tasks 1 and 2 were combined into a single atomic commit due to tight coupling (extending IEmailSender would break build until implementation added):

1. **Tasks 1-2: Domain model, DTOs, validators, and email infrastructure** - `7ba08ed` (feat)

## Files Created/Modified

**Created:**
- `EventCenter.Web/Domain/Enums/InvitationStatus.cs` - Enum with Draft, Sent, Booked, Cancelled states
- `EventCenter.Web/Domain/Entities/EventCompanyAgendaItemPrice.cs` - Join entity for per-item pricing with nullable CustomPrice
- `EventCenter.Web/Data/Configurations/EventCompanyAgendaItemPriceConfiguration.cs` - EF config with composite key and cascade rules
- `EventCenter.Web/Models/CompanyInvitationFormModel.cs` - DTO with EventId, company details, discount, personal message, SendImmediately flag
- `EventCenter.Web/Models/CompanyAgendaItemPriceModel.cs` - DTO with AgendaItemId, title, BasePrice, ManualOverride
- `EventCenter.Web/Validators/CompanyInvitationValidator.cs` - FluentValidation rules for form inputs (German messages)
- `EventCenter.Web/Data/Migrations/20260227081013_AddPhase04CompanyInvitationFields.cs` - Migration adding Status, PercentageDiscount, PersonalMessage, ExpiresAtUtc, AgendaItemPrices table

**Modified:**
- `EventCenter.Web/Domain/Entities/EventCompany.cs` - Added Phase 04 fields and AgendaItemPrices navigation property
- `EventCenter.Web/Data/Configurations/EventCompanyConfiguration.cs` - Added configurations for new fields, unique filtered index on InvitationCode
- `EventCenter.Web/Domain/EventCenterDbContext.cs` - Added DbSet for EventCompanyAgendaItemPrice
- `EventCenter.Web/Infrastructure/Email/MailKitEmailSender.cs` - Implemented SendCompanyInvitationAsync with BuildCompanyInvitationHtmlBody and BuildPricingSummaryHtml helpers

## Decisions Made

1. **Enum storage as string** - Stored InvitationStatus as string (not int) for database readability, following existing EventState pattern from Phase 02
2. **Composite primary key pattern** - Used `HasKey(p => new { p.EventCompanyId, p.AgendaItemId })` for join entity following RegistrationAgendaItem pattern
3. **Nullable CustomPrice semantics** - null CustomPrice means "use base price from EventAgendaItem.CostForMakler" - explicit override only when set
4. **Unique filtered index** - Added `IsUnique().HasFilter("[InvitationCode] IS NOT NULL")` to prevent duplicate invitation codes while allowing nulls
5. **Forward-looking schema** - Added ExpiresAtUtc now (left null) to avoid migration in Phase 5 when GUID expiration implemented
6. **Email pricing transparency** - Show both base price and custom price in table format so companies understand their discount
7. **Personal message styling** - Used #f8f9fa background with #007bff left border (matching brand blue) and italic text for emphasis
8. **CTA button color** - Used brand blue (#007bff) instead of green to match header banner

## Deviations from Plan

None - plan executed exactly as written.

Both tasks were combined into a single commit because extending IEmailSender without implementing the method would break the build. This was a logical grouping since the domain model, DTOs, validators, and email infrastructure form a cohesive foundation layer.

## Issues Encountered

**Deleted migration files in git status** - Found two deleted migration files (20260226160257_AddPhase02Fields) from a previous session. Cleaned up with `git rm` before generating new migration to avoid conflicts with model snapshot.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

All domain contracts, data model, and validation rules complete. Ready for:
- **Plan 02:** Service layer implementation with TDD (CompanyInvitationService)
- **Plan 03:** Admin UI for creating and managing company invitations

Database migration ready to apply. Email template ready for service layer to invoke.

---
*Phase: 04-company-invitations*
*Completed: 2026-02-27*
