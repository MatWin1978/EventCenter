---
status: testing
phase: 04-company-invitations
source: [04-01-SUMMARY.md, 04-02-SUMMARY.md, 04-03-SUMMARY.md]
started: 2026-02-27T09:49:10Z
updated: 2026-02-27T09:51:00Z
---

## Current Test

number: 2
name: Create Single Company Invitation (Draft)
expected: |
  Click to create a new invitation. Fill in Firmenname, Kontakt-E-Mail, and optionally Telefon.
  Click "Als Entwurf speichern". Returns to list page with the new invitation showing Status badge "Draft" (secondary color).
awaiting: user response

## Tests

### 1. Company Invitations List Page
expected: Navigate to an event's company invitations page (/admin/events/{id}/companies). Page shows a sortable table with columns: Firma, Kontakt, Status, Gesendet am, Aktionen. If no invitations exist, an empty state message is displayed. A button to create a new invitation is visible.
result: pass

### 2. Create Single Company Invitation (Draft)
expected: Click to create a new invitation. Fill in Firmenname, Kontakt-E-Mail, and optionally Telefon. Click "Als Entwurf speichern". Returns to list page with the new invitation showing Status badge "Draft" (secondary color).
result: [pending]

### 3. Pricing Configuration (Percentage Discount + Manual Override)
expected: On the invitation form, the Preisgestaltung card shows a table of agenda items with columns for BasePrice (read-only reference in text-muted), Rabattpreis (calculated preview), and Individueller Preis (override input). Setting a percentage discount recalculates the Rabattpreis preview for all items. Entering a manual override on one item takes precedence over the discount for that item.
result: [pending]

### 4. Send a Draft Invitation
expected: From the list page, click "Senden" on a Draft invitation. The status changes to "Sent" (info badge color). An invitation email is sent to the contact email address. The "Gesendet am" column shows the send timestamp.
result: [pending]

### 5. Status-Dependent Action Buttons
expected: On the list page, each invitation shows only valid actions based on status. Draft: Bearbeiten, Senden, Loschen. Sent: Bearbeiten, Erneut senden, Loschen. Booked: Bearbeiten, Buchung ansehen (no delete). Cancelled: Bearbeiten, Erneut einladen. No disabled buttons visible — only applicable actions shown.
result: [pending]

### 6. Batch Invitation Mode
expected: Navigate to batch create mode. A dynamic list allows adding multiple company rows (name + email). An optional shared percentage discount and shared personal message can be set. Submitting creates all invitations. A results summary shows succeeded/failed counts.
result: [pending]

### 7. Edit Existing Invitation
expected: Click "Bearbeiten" on an existing invitation. The form loads with pre-filled data (company details, pricing, personal message). Modify pricing or company details and save. Changes are persisted. Editing works in any status.
result: [pending]

### 8. Delete Invitation with Confirmation
expected: Click "Loschen" on a Draft or Sent invitation. A confirmation modal appears showing the company name and status. Confirming deletes the invitation and it disappears from the list. Booked invitations should NOT have a delete button.
result: [pending]

### 9. Email Preview
expected: On the invitation form (after saving or in edit mode), an "E-Mail-Vorschau" section is available. Expanding it shows an HTML preview of the invitation email including pricing summary and personal message if set.
result: [pending]

### 10. Form Validation (German Messages)
expected: Submit the invitation form with empty required fields (e.g., empty Firmenname or invalid email). German validation error messages appear next to the fields (e.g., "Firmenname ist erforderlich", invalid email format message).
result: [pending]

## Summary

total: 10
passed: 1
issues: 0
pending: 9
skipped: 0

## Gaps

[none yet]
