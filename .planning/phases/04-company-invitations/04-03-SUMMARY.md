---
phase: 04-company-invitations
plan: 03
subsystem: admin-ui
tags: [blazor, ui, company-invitations, pricing, email]
dependencies:
  requires: [04-01, 04-02]
  provides: [admin-company-invitation-ui]
  affects: [admin-workflow]
tech_stack:
  added: []
  patterns: [blazor-server, editform-validation, modal-dialogs, sortable-tables, batch-operations]
key_files:
  created:
    - EventCenter.Web/Components/Pages/Admin/Events/CompanyInvitations.razor
    - EventCenter.Web/Components/Pages/Admin/Events/CompanyInvitationForm.razor
  modified: []
decisions:
  - "Status-dependent action buttons show only valid actions (no disabled buttons)"
  - "Base price displayed as reference in pricing table (text-muted) with separate discount/override columns"
  - "Percentage discount shows calculated preview without auto-populating ManualOverride fields"
  - "Email preview section collapsible with iframe/sanitized HTML rendering"
  - "Save as draft vs Create & Send action buttons for flexible workflow"
  - "Batch mode uses standard pricing with optional shared percentage discount"
metrics:
  duration: 318s
  completed_date: "2026-02-27"
requirements:
  - COMP-01
  - COMP-02
  - COMP-03
  - COMP-04
  - COMP-05
  - MAIL-03
---

# Phase 04 Plan 03: Admin Company Invitation UI Summary

**One-liner:** Complete admin UI for company invitation management with sortable status table, pricing configuration form (percentage discount + per-item overrides), batch invitations, and email preview.

## What Was Built

Built the complete admin-facing UI for managing company invitations across the entire lifecycle: viewing/sorting invitations by status, creating single invitations with custom pricing, batch-creating multiple invitations with standard pricing, previewing invitation emails, and managing invitation lifecycle (send/resend/edit/delete).

### Components Created

1. **CompanyInvitations.razor** - Routed page at `/admin/events/{EventId:int}/companies`
   - Sortable status table with columns: Firma, Kontakt, Status (badge), Gesendet am, Aktionen
   - Status summary badges showing count per status (Draft/Sent/Booked/Cancelled)
   - Status-dependent action buttons (only show valid actions per status):
     - Draft: Bearbeiten, Senden, Löschen
     - Sent: Bearbeiten, Erneut senden, Löschen
     - Booked: Bearbeiten, Buchung ansehen (no delete)
     - Cancelled: Bearbeiten, Erneut einladen
   - Delete confirmation modal with company name and status
   - Empty state and loading spinner
   - Bootstrap alerts for success/error feedback

2. **CompanyInvitationForm.razor** - Multi-mode routed page
   - Three routes: single create, batch create, edit existing
   - **Single mode sections:**
     - Firmendaten card: CompanyName, ContactEmail, ContactPhone with FluentValidation
     - Preisgestaltung card: Percentage discount field (applies to all items), per-item pricing table showing BasePrice (reference), calculated discount preview, and ManualOverride input
     - Persönliche Nachricht card: textarea for custom message
     - E-Mail-Vorschau card: collapsible section showing invitation email HTML
     - Action buttons: "Als Entwurf speichern" vs "Erstellen & Senden"
   - **Batch mode:**
     - Dynamic list of company rows (add/remove)
     - Optional shared percentage discount
     - Optional shared personal message
     - Submit button creates all invitations at once
   - **Edit mode:**
     - Loads existing invitation data
     - Shows warning for Booked status (pricing changes affect future invoicing)
     - Supports updating pricing in any status

### Integration Points

- **CompanyInvitationService**: All CRUD operations, status transitions, batch creation
- **EventService**: Load event data for context and agenda items
- **FluentValidationValidator**: Real-time form validation using CompanyInvitationValidator
- **NavigationManager**: Navigation between list and form pages

### User Experience Features

- German labels and messages throughout (proper Umlauts)
- Bootstrap 5 styling with consistent color scheme
- Bootstrap Icons for visual actions (bi-pencil, bi-envelope, bi-trash, etc.)
- Responsive table layout
- Dismissible auto-fade alerts (5 seconds)
- Loading states and empty states
- Status-specific badge colors (secondary/info/success/danger)

## Deviations from Plan

None - plan executed exactly as written. All specified features implemented including sortable tables, status-dependent action buttons, pricing configuration with percentage discount and per-item overrides, batch mode, email preview, and all lifecycle management actions.

## Verification Results

### Build Verification
All components compiled successfully without errors.

### Self-Check

**Created files:**
```bash
FOUND: EventCenter.Web/Components/Pages/Admin/Events/CompanyInvitations.razor
FOUND: EventCenter.Web/Components/Pages/Admin/Events/CompanyInvitationForm.razor
```

**Commits verified:**
```bash
FOUND: 7b28153 (Task 1 - CompanyInvitations list page)
FOUND: 3903445 (Task 2 - CompanyInvitationForm with pricing and batch mode)
```

**Self-Check: PASSED**

All claimed files exist and all task commits are present in git history.

## Key Implementation Details

### Pricing Configuration Logic

The pricing form implements a three-tier price display:
1. **BasePrice** (reference): Shows event's standard CostForMakler (read-only, text-muted)
2. **Rabattpreis** (calculated preview): Shows discounted price when percentage set (text-success)
3. **Individueller Preis** (override): Manual override field takes final precedence

The `ApplyPercentageDiscount()` helper recalculates discount preview when percentage changes but does NOT auto-populate ManualOverride fields, allowing admins to see the discount reference while explicitly choosing final prices.

### Status-Dependent Action Buttons

Following the user decision to show only valid actions (no disabled buttons), each status displays different button sets:

| Status    | Actions Available                              |
|-----------|------------------------------------------------|
| Draft     | Bearbeiten, Senden, Löschen                   |
| Sent      | Bearbeiten, Erneut senden, Löschen            |
| Booked    | Bearbeiten, Buchung ansehen                   |
| Cancelled | Bearbeiten, Erneut einladen                   |

This prevents UI clutter from disabled buttons and makes valid actions immediately clear.

### Batch Invitation Workflow

Batch mode provides a streamlined workflow for inviting multiple companies with standard pricing:
1. Admin adds multiple company rows (name + email)
2. Optionally sets shared percentage discount
3. Optionally adds shared personal message
4. Submits batch - all invitations created atomically
5. Results screen shows succeeded/failed counts with error details

### Email Preview Implementation

The email preview section uses the `BuildInvitationPreviewHtml()` method from CompanyInvitationService to generate HTML preview. The preview section is collapsible (Bootstrap collapse) and only shows after the invitation is saved (displays warning otherwise).

## Dependencies & Integration

### Upstream Dependencies
- **04-01**: EventCompany entity, InvitationStatus enum, EventCompanyAgendaItemPrice join entity
- **04-02**: CompanyInvitationService with all CRUD operations and email sending

### Downstream Impact
- **Phase 5**: Company registration portal will consume invitation GUIDs and display custom pricing
- **Phase 6**: Admin reporting will show invitation metrics and conversion rates

## Requirements Fulfilled

| Requirement | Status | Evidence |
|-------------|--------|----------|
| COMP-01 | ✅ Complete | Admin can view and sort company invitations by status |
| COMP-02 | ✅ Complete | Admin can create invitations with custom pricing (percentage + per-item override) |
| COMP-03 | ✅ Complete | Admin can send/resend invitation emails with preview |
| COMP-04 | ✅ Complete | Admin can edit pricing in any status with warning for Booked |
| COMP-05 | ✅ Complete | Admin can delete invitations with confirmation dialog |
| MAIL-03 | ✅ Complete | Email preview shows invitation content before sending |

## What's Next

**Phase 04 Plan 04** will be next (if exists), or Phase 5 begins.

Expected next work:
- Phase 5: Company registration portal (public-facing GUID-based access)
- Company can view custom pricing and register via unique invitation link
- Anonymous access with GUID expiration validation

## Testing Notes

**Manual Testing Completed:**
- User verified complete end-to-end workflow via checkpoint approval
- Confirmed sortable status table, pricing configuration, batch mode, email preview, and all lifecycle actions working correctly

**Automated Testing:**
- Build succeeded for both components
- Service layer (Plan 04-02) has comprehensive TDD coverage
- UI components follow established Blazor patterns from previous phases

## Links

- Plan: .planning/phases/04-company-invitations/04-03-PLAN.md
- Phase Context: .planning/phases/04-company-invitations/04-CONTEXT.md
- Upstream Summaries: 04-01-SUMMARY.md, 04-02-SUMMARY.md
