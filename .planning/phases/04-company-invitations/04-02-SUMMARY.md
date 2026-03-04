---
phase: 04-company-invitations
plan: 02
subsystem: backend-services
tags: [tdd, service-layer, email, cryptography, pricing]
dependency_graph:
  requires:
    - EventCompany entity with Phase 04 fields
    - InvitationStatus enum
    - EventCompanyAgendaItemPrice entity
    - IEmailSender interface with company invitation method
  provides:
    - CompanyInvitationService (DI registered)
    - CRUD operations for company invitations
    - Cryptographically secure GUID generation
    - Pricing calculation engine
  affects:
    - Plan 03 (Admin UI will consume this service)
tech_stack:
  added:
    - System.Security.Cryptography.RandomNumberGenerator for secure GUIDs
  patterns:
    - TDD with RED-GREEN-REFACTOR cycle
    - Fire-and-forget email pattern with Task.Run
    - Transaction-based create operations
    - Decimal math with MidpointRounding.AwayFromZero
    - Status-based business rules
key_files:
  created:
    - EventCenter.Web/Services/CompanyInvitationService.cs (437 lines)
    - EventCenter.Tests/Services/CompanyInvitationServiceTests.cs (524 lines)
    - EventCenter.Tests/Validators/CompanyInvitationValidatorTests.cs (112 lines)
  modified:
    - EventCenter.Web/Program.cs (added DI registration)
    - EventCenter.Tests/Services/EmailServiceTests.cs (updated TestEmailSender)
decisions:
  - key: Pricing always editable
    rationale: User decision from context - pricing can be modified in any status
    impact: UpdateInvitationAsync works in all statuses
  - key: Always store agenda item prices
    rationale: Track custom pricing for all items, even if equal to base price
    impact: Simplifies pricing display in UI
  - key: Hard delete for non-Booked invitations
    rationale: Simpler than soft delete; Booked status protection is sufficient
    impact: No is_deleted flag needed
metrics:
  duration: 518
  tasks_completed: 1
  tests_added: 27
  tests_passing: 94
  commits: 3
  completed_date: "2026-02-27"
---

# Phase 04 Plan 02: Company Invitation Service Summary

**One-liner:** Full-featured invitation service with secure GUID generation, percentage discount + manual override pricing, status transitions, and fire-and-forget email delivery.

## What Was Built

### Core Service Methods (8 async + 2 static)

**GUID Generation:**
- `GenerateSecureInvitationCode()` - RFC 4122 v4 GUID (32 hex chars, no dashes)
- Uses `RandomNumberGenerator.Fill()` for cryptographic security
- Sets version 4 bits per RFC 4122 specification

**Pricing Calculation:**
- `CalculateCustomPrice(basePrice, percentageDiscount, manualOverride)` - Decimal math
- Percentage discount applied first: `basePrice * (1 - discount/100)`
- Manual override takes absolute precedence
- `Math.Round(..., 2, MidpointRounding.AwayFromZero)` for consistent rounding

**CRUD Operations:**
- `CreateInvitationAsync()` - Transaction-based with optional immediate send
- `SendInvitationAsync()` - Draft → Sent transition
- `ResendInvitationAsync()` - Updates timestamp, re-triggers email
- `UpdateInvitationAsync()` - Editable in all statuses (replaces AgendaItemPrices collection)
- `DeleteInvitationAsync()` - Blocked if status = Booked
- `GetInvitationsForEventAsync()` - List with navigation properties
- `GetInvitationByIdAsync()` - Single with AgendaItemPrices loaded
- `GetInvitationStatusSummaryAsync()` - Counts per status for overview

**Business Rules Enforced:**
- ✅ Duplicate email prevention (per event)
- ✅ Booked invitations cannot be deleted
- ✅ Draft invitations can only use SendInvitationAsync
- ✅ Sent invitations can only use ResendInvitationAsync
- ✅ Fire-and-forget email with try-catch logging (non-blocking)

### TDD Execution

**RED Phase (Commit ef5dd3c + 229ce02):**
- Created missing dependencies (CompanyInvitationFormModel, CompanyAgendaItemPriceModel, CompanyInvitationValidator)
- Extended IEmailSender and MailKitEmailSender with company invitation method
- Wrote 27 failing tests (16 service + 11 validator)
- Build failed as expected - CompanyInvitationService didn't exist

**GREEN Phase (Commit 4a9afc2):**
- Implemented CompanyInvitationService with all 8 async methods
- Fixed test issues (EventCompany FK tracking, decimal type conversions)
- Registered service in DI container
- All 94 tests pass (27 new + 67 existing)

**REFACTOR Phase:**
- No refactoring needed - code follows established patterns
- Service aligns with RegistrationService pattern (transaction-based, fire-and-forget email)
- Decimal pricing math is clean and explicit

## Test Coverage

**Service Tests (16):**
- GUID generation uniqueness and format
- Pricing calculation (no discount, percentage discount, manual override)
- Create invitation (valid, event not found, duplicate email, send immediately)
- Send/resend invitation (status transitions, email triggering)
- Update invitation (pricing + contact details)
- Delete invitation (draft allowed, booked blocked)
- Get operations (list, by ID, status summary)

**Validator Tests (11):**
- Valid model passes
- Empty company name fails
- Invalid email fails
- Percentage discount bounds (0-100, negative, > 100)
- Negative manual override fails
- Null percentage discount passes

**All tests pass: 94/94 ✅**

## Deviations from Plan

### Auto-fixed Issues (Rule 3 - Blocking Dependencies)

**1. [Rule 3] Missing Plan 01 dependencies**
- **Found during:** Test file creation
- **Issue:** CompanyInvitationFormModel, CompanyAgendaItemPriceModel, and CompanyInvitationValidator didn't exist (expected from Plan 01)
- **Fix:** Created all three files as complete implementations based on plan's interface section
- **Files created:**
  - EventCenter.Web/Models/CompanyInvitationFormModel.cs
  - EventCenter.Web/Models/CompanyAgendaItemPriceModel.cs
  - EventCenter.Web/Validators/CompanyInvitationValidator.cs
- **Commit:** ef5dd3c (chore commit)

**2. [Rule 3] IEmailSender missing company invitation method**
- **Found during:** Test compilation
- **Issue:** IEmailSender interface lacked SendCompanyInvitationAsync method
- **Fix:** Extended interface and implemented method in MailKitEmailSender with HTML email template
- **Files modified:**
  - EventCenter.Web/Infrastructure/Email/IEmailSender.cs
  - EventCenter.Web/Infrastructure/Email/MailKitEmailSender.cs
- **Commit:** ef5dd3c (same chore commit)

**3. [Rule 1] TestEmailSender missing interface implementation**
- **Found during:** Test build
- **Issue:** TestEmailSender didn't implement new SendCompanyInvitationAsync method
- **Fix:** Added stub implementation that captures sent invitations
- **Files modified:** EventCenter.Tests/Services/EmailServiceTests.cs
- **Commit:** 229ce02 (test commit)

**4. [Rule 1] Test data type mismatches**
- **Found during:** Test execution
- **Issue:** InlineData attributes used int/double instead of decimal literals, causing type conversion errors
- **Fix:** Converted Theory test to three separate Fact tests with explicit decimal literals
- **Files modified:** EventCenter.Tests/Services/CompanyInvitationServiceTests.cs
- **Commit:** 4a9afc2 (feat commit)

**5. [Rule 1] EF Core foreign key tracking issue**
- **Found during:** Test execution
- **Issue:** Adding EventCompanyAgendaItemPrice before saving parent EventCompany caused FK tracking error
- **Fix:** Split SaveChangesAsync calls - save parent first, then children
- **Files modified:** EventCenter.Tests/Services/CompanyInvitationServiceTests.cs (2 tests)
- **Commit:** 4a9afc2 (feat commit)

## Key Decisions

**1. Always store agenda item prices (even if equal to base price)**
- Simplifies UI logic - no need to calculate on-the-fly
- Explicit pricing history for auditing
- Trade-off: More DB rows vs. cleaner queries

**2. Fire-and-forget email pattern**
- Consistent with RegistrationService pattern
- Non-blocking user experience
- Errors logged but don't fail the request

**3. Hard delete for non-Booked invitations**
- Booked status protection is sufficient business rule
- No need for soft delete complexity
- Simplifies queries and cleanup

## Verification Results

✅ All service tests pass (16/16)
✅ All validator tests pass (11/11)
✅ Full test suite passes (94/94)
✅ Service has 8 async methods + 2 static helpers
✅ DI registration confirmed at line 46 of Program.cs

## Files Created

- `EventCenter.Web/Services/CompanyInvitationService.cs` (437 lines)
- `EventCenter.Web/Models/CompanyInvitationFormModel.cs` (13 lines)
- `EventCenter.Web/Models/CompanyAgendaItemPriceModel.cs` (10 lines)
- `EventCenter.Web/Validators/CompanyInvitationValidator.cs` (42 lines)
- `EventCenter.Tests/Services/CompanyInvitationServiceTests.cs` (524 lines)
- `EventCenter.Tests/Validators/CompanyInvitationValidatorTests.cs` (112 lines)

## Files Modified

- `EventCenter.Web/Program.cs` (+1 line - DI registration)
- `EventCenter.Web/Infrastructure/Email/IEmailSender.cs` (+1 method signature)
- `EventCenter.Web/Infrastructure/Email/MailKitEmailSender.cs` (+133 lines - email method + HTML builder)
- `EventCenter.Tests/Services/EmailServiceTests.cs` (+7 lines - TestEmailSender stub)

## Commits

1. **ef5dd3c** - chore(04-02): add missing Plan 01 dependencies
2. **229ce02** - test(04-02): add failing tests for CompanyInvitationService (RED)
3. **4a9afc2** - feat(04-02): implement CompanyInvitationService with full TDD coverage (GREEN)

## Next Steps

**Plan 03:** Admin UI for managing company invitations
- Create/edit invitation form with pricing configuration
- Invitation list with status badges and actions
- Email preview modal
- Batch invitation interface

## Self-Check: PASSED

✅ **Service file exists:** EventCenter.Web/Services/CompanyInvitationService.cs
✅ **Test files exist:**
- EventCenter.Tests/Services/CompanyInvitationServiceTests.cs
- EventCenter.Tests/Validators/CompanyInvitationValidatorTests.cs
✅ **Commits exist:**
- ef5dd3c: chore(04-02): add missing Plan 01 dependencies
- 229ce02: test(04-02): add failing tests for CompanyInvitationService
- 4a9afc2: feat(04-02): implement CompanyInvitationService with full TDD coverage
✅ **All 94 tests pass**
✅ **DI registration confirmed**
