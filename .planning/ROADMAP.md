# Roadmap: Veranstaltungscenter

## Overview

This roadmap delivers a complete event management system for brokers and invited companies, progressing from foundational authentication and infrastructure through admin event creation, broker self-registration, company invitation workflows, and finally advanced features like guest management and webinar support. The journey follows a strict dependency chain: authentication must be established before any feature work, admins must create events before brokers can register, and company invitations must be configured before companies can book participants.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Foundation & Authentication** - Establish Blazor Server infrastructure, authentication, and domain model (completed 2026-02-26)
- [x] **Phase 2: Admin Event Management** - Enable admins to create and configure events with agenda items and options (completed 2026-02-26)
- [ ] **Phase 3: Makler Event Discovery & Registration** - Allow brokers to browse events and self-register with agenda selection
- [ ] **Phase 4: Company Invitations** - Enable admins to invite companies with custom pricing
- [ ] **Phase 5: Company Booking Portal** - Anonymous company representative booking via GUID links
- [x] **Phase 6: Guest Management** - Brokers can register companions with limit enforcement (completed 2026-02-27)
- [x] **Phase 7: Cancellation & Participant Management** - Registration cancellation and admin export capabilities (completed 2026-02-27)
- [x] **Phase 8: Webinar Support** - Support for webinar events alongside in-person events (completed 2026-02-27)

## Phase Details

### Phase 1: Foundation & Authentication
**Goal**: Establish Blazor Server application with authenticated access for admins and brokers
**Depends on**: Nothing (first phase)
**Requirements**: AUTH-01, AUTH-02
**Success Criteria** (what must be TRUE):
  1. Admin can log in to backoffice via Keycloak and access admin pages
  2. Makler can log in to portal via Keycloak and access broker pages
  3. Authentication state is properly managed across Blazor Server circuits
  4. Domain entities (Event, EventAgendaItem, EventCompany, Registrations) exist with EF Core configurations
  5. Database schema is created and ready for data
**Plans**: 4 plans

Plans:
- [ ] 01-01-PLAN.md — Project scaffolding, domain entities, and EF Core configuration
- [ ] 01-02-PLAN.md — Keycloak OIDC authentication with role-based access control
- [ ] 01-03-PLAN.md — Role-based landing pages, navigation, and timezone utilities
- [ ] 01-04-PLAN.md — EF Core migrations, test infrastructure, and FluentValidation setup

### Phase 2: Admin Event Management
**Goal**: Admins can create, configure, and publish events with agenda items and extra options
**Depends on**: Phase 1
**Requirements**: EVNT-01, EVNT-02, EVNT-03, EVNT-04, AGND-01, AGND-02, AGND-03, XOPT-01, XOPT-02
**Success Criteria** (what must be TRUE):
  1. Admin can create new in-person events with all required details (title, description, location, dates, limits)
  2. Admin can edit existing events and publish/unpublish them
  3. Admin can add agenda items with separate pricing for brokers and guests
  4. Admin can configure extra options (add-ons) and prevent deletion of already-booked options
  5. System automatically calculates and displays event state (Public, DeadlineReached, Finished)
**Plans**: 4 plans

Plans:
- [ ] 02-01-PLAN.md — Domain model extensions, EventState calculation, and validators (TDD)
- [ ] 02-02-PLAN.md — Event service layer with CRUD, publish/unpublish, duplicate, and file operations (TDD)
- [ ] 02-03-PLAN.md — Admin event list page with data table, status badges, and shared components
- [ ] 02-04-PLAN.md — Admin event form (create/edit) with inline agenda items and extra options

### Phase 3: Makler Event Discovery & Registration
**Goal**: Brokers can discover events, view details, and self-register with agenda item selection
**Depends on**: Phase 2
**Requirements**: MLST-01, MLST-02, MLST-03, MDET-01, MDET-02, MDET-03, MREG-01, MREG-02, MREG-03, MREG-04, MAIL-01
**Success Criteria** (what must be TRUE):
  1. Makler sees list of all published events with registration status indicators (available, registered, full, deadline passed)
  2. Makler can search events by name/location and filter by date
  3. Makler can view full event details including documents and can download them
  4. Makler can self-register for an event, select agenda items, see costs, and receive validation before submission
  5. Makler receives confirmation email after successful registration
  6. Makler can export event to iCalendar format for calendar integration
**Plans**: 5 plans

Plans:
- [x] 03-01-PLAN.md — Domain model extensions, NuGet packages (MailKit, Ical.NET), service interfaces, and registration validator (completed 2026-02-26)
- [ ] 03-02-PLAN.md — RegistrationService with concurrency + EventService broker queries (TDD)
- [ ] 03-03-PLAN.md — MailKit email sender, Ical.NET calendar export, and download API endpoints
- [ ] 03-04-PLAN.md — Broker event list page with card grid, search, date filtering, and status badges
- [ ] 03-05-PLAN.md — Event detail page, registration flow with confirmation modal, and confirmation page

### Phase 4: Company Invitations
**Goal**: Admins can invite companies to events with custom pricing and send invitation emails
**Depends on**: Phase 2
**Requirements**: COMP-01, COMP-02, COMP-03, COMP-04, COMP-05, MAIL-03
**Success Criteria** (what must be TRUE):
  1. Admin can invite a company to an event and configure company-specific pricing per agenda item
  2. Admin can send invitation email to company with secure GUID link
  3. Admin can view invitation status (sent, booking received, cancelled) for each company
  4. Admin can delete company invitations that have not yet been booked
  5. System generates cryptographically strong GUIDs for company access links
**Plans**: 3 plans

Plans:
- [ ] 04-01-PLAN.md — Domain model extensions, per-item pricing entity, email template, validators, and migration
- [ ] 04-02-PLAN.md — CompanyInvitationService with TDD (CRUD, GUID generation, pricing calculation, email triggering)
- [ ] 04-03-PLAN.md — Admin UI: invitation status table with sorting and management, invitation form with pricing configuration

### Phase 5: Company Booking Portal
**Goal**: Company representatives can access booking page via GUID link and submit participant lists
**Depends on**: Phase 4
**Requirements**: AUTH-03, CBOK-01, CBOK-02, CBOK-03, CBOK-04, CBOK-05, CBOK-06, CBOK-07, CBOK-08, MAIL-04, MAIL-05
**Success Criteria** (what must be TRUE):
  1. Company representative can access booking page using GUID link without logging in
  2. Company representative sees company-specific prices and available agenda items
  3. Company representative can enter unlimited participants with contact details
  4. Company representative can select extra options and see automatic cost calculation
  5. Company representative can submit booking and receive confirmation
  6. Company representative can cancel booking or report non-participation
  7. Admin receives email notification when company submits or cancels booking
  8. System enforces GUID expiration and rate limiting to prevent enumeration attacks
**Plans**: 3 plans

Plans:
- [ ] 05-01-PLAN.md — Domain extensions, DTOs, validators, admin email notifications, and rate limiting configuration
- [ ] 05-02-PLAN.md — CompanyBookingService TDD (GUID validation, booking submission, cancellation, non-participation)
- [ ] 05-03-PLAN.md — Company booking portal UI (anonymous page with booking form, participant table, cost summary, booking management)

### Phase 6: Guest Management
**Goal**: Brokers can register companions (guests) for events within configured limits
**Depends on**: Phase 3
**Requirements**: GREG-01, GREG-02, GREG-03, MAIL-02
**Success Criteria** (what must be TRUE):
  1. Makler can register a guest (companion) for an event they are attending
  2. System enforces companion limit per broker (MaxCompanions validation)
  3. Makler provides all required guest details (salutation, name, email, relationship type)
  4. Makler receives confirmation email after guest registration
  5. Guest registrations display correct costs based on CompanionParticipationCost
**Plans**: 2 plans

Plans:
- [ ] 06-01-PLAN.md — Domain model extensions, guest registration service methods, email template, and tests
- [ ] 06-02-PLAN.md — EventDetail page guest registration section with inline form, guest list, and limit enforcement

### Phase 7: Cancellation & Participant Management
**Goal**: Users can cancel registrations and admins can view/export participant data
**Depends on**: Phase 3, Phase 5, Phase 6
**Requirements**: MCAN-01, MCAN-02, MCAN-03, MCAN-04, PART-01, PART-02, PART-03, PART-04, PART-05
**Success Criteria** (what must be TRUE):
  1. Makler can cancel their own event registration within deadline constraints
  2. Makler can cancel guest registrations they created
  3. System verifies cancellation permissions (only registration creator can cancel)
  4. System correctly updates registration counts after cancellations
  5. Admin can view participant lists for any event and filter by company
  6. Admin can export participant data, contact information, non-participants, and company lists to Excel/CSV
**Plans**: 4 plans

Plans:
- [ ] 07-01-PLAN.md — Cancellation service with TDD (CancelRegistrationAsync, domain changes, email templates)
- [ ] 07-02-PLAN.md — Participant export service with ClosedXML (4 Excel export types, query service)
- [ ] 07-03-PLAN.md — Cancellation UI on EventDetail page (cancel button, confirmation modal, deadline enforcement)
- [ ] 07-04-PLAN.md — Admin participant list page with table, filtering, export dropdown, and EF Core migration

### Phase 8: Webinar Support
**Goal**: System supports webinar events with external registration links alongside in-person events
**Depends on**: Phase 2
**Requirements**: WBNR-01, WBNR-02
**Success Criteria** (what must be TRUE):
  1. Admin can create and edit webinar events (WebinarEvent type)
  2. Admin can publish and unpublish webinar events
  3. Webinar events display with distinct visual indicators (icon, label)
  4. Webinar events show external registration link instead of internal registration form
  5. Brokers can filter event list to show only webinars or only in-person events
**Plans**: TBD

Plans:
- [ ] TBD after phase planning

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation & Authentication | 4/4 | Complete   | 2026-02-26 |
| 2. Admin Event Management | 4/4 | Complete   | 2026-02-26 |
| 3. Makler Event Discovery & Registration | 3/5 | In Progress|  |
| 4. Company Invitations | 0/3 | Not started | - |
| 5. Company Booking Portal | 0/TBD | Not started | - |
| 6. Guest Management | 2/2 | Complete   | 2026-02-27 |
| 7. Cancellation & Participant Management | 4/4 | Complete   | 2026-02-27 |
| 8. Webinar Support | 3/3 | Complete    | 2026-02-27 |

### Phase 9: Navigation & Makler-Registrierungsübersicht

**Goal:** Role-basierter Redirect nach Login und Makler-Registrierungsübersicht als Kartenliste
**Requirements**: NAV-01, NAV-02
**Depends on:** Phase 8
**Plans:** 2/2 plans complete

Plans:
- [ ] 09-01-PLAN.md — Home.razor smart redirect + GetBrokerRegistrationsAsync service method
- [ ] 09-02-PLAN.md — /portal/registrations page with registration cards, empty state, and human-verify checkpoint

### Phase 10: Firmenstammdaten (Company Address Book)

**Goal:** Zentrales Firmenadressbuch damit Admins Firmen einmalig anlegen und bei Einladungen aus dem Adressbuch wählen können
**Requirements**: FIRM-01, FIRM-02, FIRM-03, FIRM-04, FIRM-05
**Depends on:** Phase 4 (Firmeneinladungen)
**Plans:** 1/1 plans complete (completed 2026-03-01)

Plans:
- [x] 10-01 — Company entity + Migration + CompanyService + CompanyValidator + Admin-UI (List/Form) + Einladungsformular-Umbau
