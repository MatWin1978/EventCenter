---
phase: 05-company-booking-portal
plan: 01
subsystem: domain-validation-email
tags:
  - domain-model
  - validation
  - email
  - rate-limiting
  - security
dependency_graph:
  requires:
    - 04-01 (EventCompany entity, InvitationStatus enum)
    - 04-02 (Email infrastructure)
  provides:
    - CompanyBookingFormModel and ParticipantModel DTOs
    - CompanyBookingValidator with FluentValidation rules
    - Admin booking notification emails
    - Admin cancellation notification emails
    - Rate limiting middleware for booking endpoint
  affects:
    - EventCompany entity (new fields for cancellation tracking)
    - IEmailSender interface (new admin notification methods)
tech_stack:
  added:
    - Microsoft.AspNetCore.RateLimiting (rate limiting middleware)
  patterns:
    - FluentValidation nested validators (RuleForEach with SetValidator)
    - Rate limiting with fixed window policy (10 req/min)
    - Fire-and-forget email pattern for admin notifications
    - HTML email templates with German culture formatting
key_files:
  created:
    - EventCenter.Web/Models/CompanyBookingFormModel.cs
    - EventCenter.Web/Validators/CompanyBookingValidator.cs
  modified:
    - EventCenter.Web/Domain/Entities/EventCompany.cs
    - EventCenter.Web/Infrastructure/Email/IEmailSender.cs
    - EventCenter.Web/Infrastructure/Email/MailKitEmailSender.cs
    - EventCenter.Web/Program.cs
    - EventCenter.Web/appsettings.json
decisions:
  - choice: Added CancellationComment, BookingDateUtc, and IsNonParticipation fields to EventCompany
    rationale: Separates full cancellation from non-participation reporting, provides audit trail
  - choice: Used nested ParticipantValidator within CompanyBookingValidator
    rationale: Follows FluentValidation best practice for collection validation
  - choice: Admin notification email reads from environment variable first, then config
    rationale: Allows runtime override without changing appsettings.json
  - choice: Rate limit of 10 requests per minute with zero queue
    rationale: Prevents abuse while allowing legitimate booking flow (AUTH-03)
  - choice: German error messages throughout validation
    rationale: Consistent with existing codebase patterns from Phase 2-4
metrics:
  duration: 179
  completed_at: "2026-02-27T11:05:21Z"
---

# Phase 05 Plan 01: Foundation Layer Summary

**One-liner:** Domain extensions, form DTOs, FluentValidation rules, admin email notifications, and rate limiting for company booking portal.

## What Was Built

Created the complete foundation layer for the company booking portal:

1. **Domain Model Extensions**: Added three new fields to EventCompany entity to support booking management and cancellation tracking (CancellationComment, BookingDateUtc, IsNonParticipation)

2. **Form DTOs**: Created CompanyBookingFormModel and ParticipantModel with per-participant agenda item selection support

3. **FluentValidation Rules**: Implemented CompanyBookingValidator and ParticipantValidator with German error messages, validating salutation, names, emails, and agenda selection

4. **Admin Email Notifications**: Extended IEmailSender interface and MailKitEmailSender implementation with two new methods:
   - SendAdminBookingNotificationAsync: Notifies admin of new company bookings with participant table
   - SendAdminCancellationNotificationAsync: Notifies admin of cancellations/non-participation with optional comment

5. **Rate Limiting Middleware**: Configured fixed window rate limiter (10 req/min) for anonymous company booking endpoint with German error message

6. **Configuration**: Added CompanyBooking section to appsettings.json with invitation expiration hours and admin notification email

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Domain model extensions and form DTOs | 6ede089 | EventCompany.cs, CompanyBookingFormModel.cs |
| 2 | FluentValidation rules and admin email notification methods | ae7c7d0 | CompanyBookingValidator.cs, IEmailSender.cs, MailKitEmailSender.cs |
| 3 | Rate limiting middleware and GUID expiration configuration | ef63fbd | Program.cs, appsettings.json |

## Deviations from Plan

None - plan executed exactly as written.

## Technical Implementation Notes

### Domain Extensions
- CancellationComment: Nullable string for optional cancellation/non-participation reason
- BookingDateUtc: Nullable DateTime for audit trail of when booking was submitted
- IsNonParticipation: Boolean flag distinguishes between full cancellation and non-participation report

### Validation Rules
- Salutation must be one of: "Herr", "Frau", "Divers"
- FirstName and LastName max 100 characters
- Email max 200 characters with EmailAddress validation
- Each participant must select at least one agenda item
- At least one participant required per booking

### Email Templates
Both admin notification emails follow existing HTML structure from Phase 4:
- Header banner with color coding (#007bff for bookings, #dc3545 for cancellations)
- Event card with CET-formatted dates
- Company details section
- Participant table (booking) or comment section (cancellation)
- CTA button linking to admin company management page
- German culture formatting for currency and dates

### Rate Limiting
- Policy name: "CompanyBooking"
- Fixed window: 10 permits per 1 minute
- Queue disabled (QueueLimit = 0)
- 429 response with German message: "Zu viele Anfragen. Bitte versuchen Sie es später erneut."
- Middleware placed before UseAuthentication in pipeline

### Configuration
- InvitationExpirationHours: 72 (3 days default)
- AdminNotificationEmail: "admin@example.com" (placeholder for production)
- Email implementation reads environment variable first, then falls back to config

## Verification Results

All automated verifications passed:
- ✅ Project builds successfully (4 pre-existing warnings, 0 errors)
- ✅ CompanyBookingFormModel.cs exists
- ✅ CompanyBookingValidator.cs exists
- ✅ IEmailSender has both new admin notification methods
- ✅ MailKitEmailSender implements both methods with HTML templates
- ✅ Program.cs has AddRateLimiter and UseRateLimiter configured
- ✅ EventCompany has CancellationComment, BookingDateUtc, IsNonParticipation fields
- ✅ appsettings.json has CompanyBooking configuration section

## Next Steps

Plan 05-02 (CompanyBookingService) will:
- Implement business logic using these DTOs and validators
- Use admin notification methods for fire-and-forget emails
- Read CompanyBooking configuration for GUID expiration
- Apply "CompanyBooking" rate limiting policy to booking endpoint

## Self-Check: PASSED

All claimed files exist and commits are valid:

✅ FOUND: CompanyBookingFormModel.cs
✅ FOUND: CompanyBookingValidator.cs
✅ FOUND: commit 6ede089
✅ FOUND: commit ae7c7d0
✅ FOUND: commit ef63fbd
