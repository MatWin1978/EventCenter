# Feature Research

**Domain:** Event Management System (Broker Portal)
**Researched:** 2026-02-26
**Confidence:** HIGH

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Online Event Registration | All modern event systems offer self-service registration; manual registration feels outdated | MEDIUM | Must handle user authentication, data validation, and confirmation workflows |
| Event Listing & Search | Users expect to browse available events with basic filtering (date, type, location) | LOW | Standard CRUD + search functionality |
| Registration Confirmation | Automated confirmation emails are industry standard; users expect immediate proof of registration | LOW | Email templates + send logic |
| RSVP Status Tracking | Users need to see which events they're registered for and their registration status | LOW | Simple status field (registered, cancelled, waitlist) |
| Event Details Display | Comprehensive event information (date, time, location, description, agenda) is baseline | LOW | Standard detail view with structured data |
| Cancellation/Withdrawal | Users expect ability to cancel their own registrations within deadline constraints | MEDIUM | Requires business logic for deadlines, refunds, notifications |
| Admin Event Management | Event creation, editing, publishing/unpublishing are fundamental admin capabilities | MEDIUM | Standard CRUD with state management |
| Participant List Export | Admins need to export attendee data for logistics (catering, name badges, etc.) | LOW | CSV/Excel export with configurable fields |
| Registration Deadlines | Enforcing registration cutoff dates prevents last-minute chaos | LOW | Date validation + UI feedback |
| Accessibility Compliance | WCAG 2.1 standards are table stakes in 2026; required for enterprise/government use | MEDIUM | Requires proper semantic HTML, ARIA labels, keyboard navigation |
| Mobile Responsiveness | 50%+ of users access on mobile; non-responsive sites feel broken | MEDIUM | Blazor responsive layouts, touch-friendly controls |
| Data Privacy & Security | GDPR compliance, data encryption, secure authentication are non-negotiable for enterprise | MEDIUM | Keycloak handles auth; need data handling policies |

### Differentiators (Competitive Advantage)

Features that set the product apart. Not required, but valuable.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Agenda Item Selection | Allows attendees to personalize their event experience by choosing specific sessions/topics | HIGH | Multi-select with capacity management, conflict detection, pricing per item |
| Company-Specific Pricing | Enables special pricing for invited companies, improving B2B relationship management | MEDIUM | Price override logic per company per agenda item; transparency in pricing display |
| Anonymous Company Portal (GUID Links) | Reduces friction for company representatives who don't need personal accounts | HIGH | Security via unguessable GUIDs, session management without authentication, link expiry |
| Guest/Companion Registration | Brokers can bring colleagues/partners; increases attendance and networking value | MEDIUM | Guest limit enforcement per broker, separate guest data collection |
| Dual Event Types (In-Person + Webinar) | Single platform handles both formats; reduces tool sprawl | MEDIUM | Different field sets, validation rules, attendance tracking per type |
| Extra Options/Add-ons | Monetize additional services (meals, workshops, materials) beyond base registration | MEDIUM | Separate booking items with pricing, inventory if needed |
| iCalendar Export | One-click calendar integration; reduces no-shows and improves attendee experience | LOW | Generate .ics files with event details; standard format |
| Real-Time Availability | Show live capacity for events/sessions; creates urgency and prevents over-booking | MEDIUM | Requires live count calculations, optimistic locking for concurrent registrations |
| Company Invitation Workflow | Proactive invitation system vs. passive event listing; drives targeted engagement | HIGH | Invitation tracking, company-specific URLs, acceptance workflow |
| Multi-Track Agendas | Allow complex events with parallel sessions; supports professional conference formats | HIGH | Session scheduling, room assignments, conflict resolution |
| Registration on Behalf Of | Admins/assistants can register others; common in corporate environments | MEDIUM | Proxy registration with proper attribution, notification routing |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but create problems.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Complex Approval Workflows | "Management needs to approve all registrations" | Adds friction, delays confirmation, requires manual intervention, creates bottlenecks | Use invitation-only events with pre-approved lists; auto-approve with post-event audit |
| Payment Integration (v1) | "We should collect payment during registration" | Requires PCI compliance, merchant accounts, refund handling, financial reconciliation; scope creep | Out-of-scope per PROJECT.md; handle billing externally via invoicing |
| Automatic Waitlisting | "Move waitlist to registered when spots open" | Creates race conditions, notification spam, complex state management, user confusion | Manual waitlist promotion with admin notification; simpler and more controlled |
| Real-Time Chat/Messaging | "Attendees should chat with each other" | Requires moderation, websocket infrastructure, storage, privacy concerns, notification fatigue | Provide LinkedIn/email sharing for networking; leverage existing tools |
| Gamification (Badges/Points) | "Make it more engaging with points" | Adds development time, creates perverse incentives, may feel gimmicky in professional context | Focus on content quality and networking value; professional audience doesn't need game mechanics |
| Multi-Language Support (v1) | "We might have international attendees" | Doubles QA effort, requires translation management, complicates content, unclear ROI | German-only per PROJECT.md; add if demand proven post-launch |
| Mobile Native App | "We need iOS/Android apps" | Separate codebases, app store management, update cycles, high maintenance cost | Responsive web design with PWA features; 90% of benefit, 20% of cost |
| Social Media Sharing | "Let users share their registration" | Privacy concerns (who's attending?), spam potential, minimal actual usage | Simple "Add to Calendar" export; professional context doesn't need social broadcasting |
| Advanced Reporting Dashboard | "We need charts and graphs" | Over-engineering for v1; unclear which metrics actually matter pre-launch | Start with CSV export; build dashboards based on actual usage patterns |
| Automated Email Campaigns | "Send promotional emails to all users" | Becomes marketing automation tool; spam risk, unsubscribe management, regulatory compliance | Manual targeted emails for v1; integrate with existing email platform if needed |

## Feature Dependencies

```
Event Listing
    └──requires──> Event Details Display
                      └──requires──> Event Creation

Self-Registration
    └──requires──> Registration Confirmation
    └──requires──> RSVP Status Tracking
    └──requires──> Cancellation

Agenda Item Selection
    └──requires──> Event Creation with Agenda Items
    └──requires──> Pricing Logic (for per-item costs)
    └──enhances──> Multi-Track Agendas

Company Portal (GUID)
    └──requires──> Company Invitation System
    └──requires──> Anonymous Session Management
    └──conflicts──> Standard Authentication (by design)

Guest Registration
    └──requires──> Self-Registration (broker must register first)
    └──requires──> Guest Limit Enforcement

Extra Options
    └──requires──> Pricing Logic
    └──enhances──> Registration Flow

iCalendar Export
    └──requires──> Event Details Display
    └──requires──> RSVP Status Tracking

Real-Time Availability
    └──requires──> Capacity Management
    └──requires──> Registration Counting Logic
    └──enhances──> Agenda Item Selection (prevent overbooking)

Company-Specific Pricing
    └──requires──> Company Invitation System
    └──requires──> Pricing Logic
    └──requires──> Company-Event Relationship Data
```

### Dependency Notes

- **Agenda Item Selection requires Pricing Logic:** Each agenda item can have different costs for brokers vs. guests; pricing engine needed before agenda selection makes sense
- **Company Portal requires Anonymous Session Management:** GUID links bypass normal auth; requires separate session handling and security model
- **Guest Registration requires Self-Registration:** Logical flow—broker registers self first, then adds guests
- **Real-Time Availability enhances Agenda Item Selection:** Without live counts, users may select sessions that are full; creates bad UX
- **Company-Specific Pricing requires Company Invitation System:** Special pricing only makes sense in context of invited companies

## MVP Definition

### Launch With (v1)

Minimum viable product — what's needed to validate the concept.

**Admin Core:**
- [x] Event CRUD (create, edit, publish, delete) for both in-person and webinar types — Essential for any event system
- [x] Agenda item management with broker/guest pricing — Core differentiator per user stories
- [x] Company invitation with special pricing — Core workflow per US-08 to US-10
- [x] Participant list view and export — Needed for logistics (US-11, US-12)
- [x] Extra options management — Part of monetization model (US-07)

**Broker Portal:**
- [x] Event listing with search/filter — Discovery mechanism (US-13)
- [x] Event detail view with registration status — Information display (US-14)
- [x] Self-registration with agenda selection — Core workflow (US-15)
- [x] Guest/companion registration — Networking value (US-17)
- [x] Registration cancellation — User autonomy (US-18, US-19)
- [x] iCalendar export — Reduce no-shows, table stakes feature

**Company Portal:**
- [x] Anonymous access via GUID link — Core differentiator (US-20)
- [x] Participant booking and submission — Core workflow (US-21)
- [x] Booking cancellation — Flexibility (US-22)
- [x] Non-participation notification — Data quality (US-23)

**Cross-Cutting:**
- [x] Email confirmations for all registration actions — Communication baseline
- [x] Registration deadline enforcement — Business rule (per domain model)
- [x] Capacity management (companion limits) — Prevent overbooking
- [x] Responsive design — Mobile access requirement
- [x] Keycloak/OIDC authentication for brokers/admins — Security baseline

**Rationale:** These features align with all 23 user stories and deliver the core value proposition: "Makler und eingeladene Firmen können sich reibungslos für Veranstaltungen anmelden, Agendapunkte auswählen und ihre Teilnahme verwalten."

### Add After Validation (v1.x)

Features to add once core is working.

- [ ] Waitlist management — Add when events consistently sell out (currently no user stories require this)
- [ ] Advanced reporting dashboard — Build based on actual admin needs post-launch
- [ ] Bulk import for company invitations — Add if admins manage 50+ companies per event
- [ ] Email template customization — Add if default templates don't meet branding needs
- [ ] Session scheduling conflict detection — Add if multi-track events become common
- [ ] Badge printing integration — Add if on-site check-in becomes priority
- [ ] QR code check-in — Add if event volume justifies on-site efficiency gains
- [ ] Reminder emails (X days before event) — Add to reduce no-shows if validated as problem
- [ ] Registration analytics (conversion rates, popular sessions) — Add when optimization becomes priority
- [ ] Multi-language support — Add only if international events validated as use case

### Future Consideration (v2+)

Features to defer until product-market fit is established.

- [ ] Payment integration — Only add if external billing creates friction (currently out of scope)
- [ ] Mobile native apps — Only if responsive web proves insufficient (unlikely for broker audience)
- [ ] Social networking features — Only if organic networking need emerges
- [ ] Video conferencing integration — Only if webinar hosting becomes in-scope (currently external)
- [ ] Survey/feedback collection — Only if post-event engagement becomes strategic
- [ ] Sponsor/exhibitor management — Only if events expand to include commercial sponsors
- [ ] Speaker/presenter management — Only if content management becomes complex
- [ ] Custom branding per event — Only if white-label becomes business requirement
- [ ] API for third-party integrations — Only if ecosystem partnerships emerge
- [ ] Advanced access control (department-level permissions) — Only if org structure requires it

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Self-registration with agenda selection | HIGH | HIGH | P1 |
| Company portal (GUID links) | HIGH | HIGH | P1 |
| Event listing & search | HIGH | LOW | P1 |
| Registration confirmation emails | HIGH | LOW | P1 |
| Admin event management | HIGH | MEDIUM | P1 |
| Guest/companion registration | HIGH | MEDIUM | P1 |
| Company-specific pricing | HIGH | MEDIUM | P1 |
| Participant list export | HIGH | LOW | P1 |
| Registration cancellation | HIGH | MEDIUM | P1 |
| iCalendar export | HIGH | LOW | P1 |
| Extra options/add-ons | MEDIUM | MEDIUM | P1 |
| Real-time availability | MEDIUM | MEDIUM | P1 |
| Registration deadline enforcement | HIGH | LOW | P1 |
| Mobile responsiveness | HIGH | MEDIUM | P1 |
| Accessibility compliance | MEDIUM | MEDIUM | P1 |
| Waitlist management | MEDIUM | MEDIUM | P2 |
| Advanced reporting dashboard | MEDIUM | HIGH | P2 |
| QR code check-in | LOW | MEDIUM | P2 |
| Badge printing | LOW | MEDIUM | P2 |
| Reminder emails | MEDIUM | LOW | P2 |
| Multi-language support | LOW | HIGH | P3 |
| Payment integration | LOW | HIGH | P3 |
| Mobile native apps | LOW | HIGH | P3 |
| Social networking | LOW | HIGH | P3 |
| Video conferencing integration | LOW | HIGH | P3 |

**Priority key:**
- P1: Must have for launch (covers all 23 user stories)
- P2: Should have, add when possible (improves UX but not core workflow)
- P3: Nice to have, future consideration (strategic bets, not validated needs)

## Competitor Feature Analysis

| Feature | Eventbrite/Hopin | Zoom Events | Microsoft Dynamics | Our Approach |
|---------|------------------|-------------|-------------------|--------------|
| Event registration | Public marketplace + custom events | Webinar-focused | Enterprise CRM-integrated | Broker portal + anonymous company links (more focused) |
| Agenda selection | Basic session selection | Breakout rooms during event | Full session scheduling | Per-item pricing + capacity (more granular) |
| Pricing models | Ticket tiers, early bird | Per-seat licensing | Enterprise quotes | Broker vs. guest pricing + company specials (B2B optimized) |
| Guest management | +1 ticketing | Panelist/attendee roles | Contact relationships | Companion limits per broker (role-aware) |
| Company invitations | Email campaigns | Webinar invitations | Account-based marketing | GUID anonymous links (lower friction) |
| Authentication | Email/social login | Zoom accounts required | AD/SSO | Keycloak OIDC + anonymous GUID (hybrid) |
| Calendar integration | Basic .ics export | Zoom calendar | Outlook deep integration | iCalendar export (standards-based) |
| Reporting | Sales analytics, engagement | Attendance, Q&A analytics | Full CRM reporting | Participant lists + CSV (simple, actionable) |

**Differentiation Strategy:**
- **Hybrid authentication model:** Full accounts for brokers, anonymous GUID access for companies = lower friction than competitors
- **B2B pricing flexibility:** Company-specific pricing not common in standard event platforms; usually enterprise-only feature
- **Agenda pricing granularity:** Per-item costs for brokers vs. guests vs. company specials = more sophisticated than ticket tiers
- **Simplicity over features:** No payment processing, no social features, no marketing automation = faster to market, easier to support

## Sources

### Market Research & Trends
- [Event Management Software Trends to Watch in 2026](https://blackthorn.io/content-hub/event-management-software-trends-to-watch-in-2026-features-crm-connection-and-roi/)
- [Event Management Software in 2026: Evaluating Platforms, Integrations, and ROI](https://www.ticketfairy.com/blog/event-management-software-in-2026-evaluating-platforms-integrations-and-roi)
- [Best Event Management Software 2026](https://www.capterra.com/event-management-software/)
- [31 Event Management Software to Streamline Your Planning in 2026](https://eventify.io/blog/event-management-software)
- [Best Event Management Tools for 2026](https://www.bizzabo.com/blog/best-event-management-tools)

### Registration & Ticketing
- [Event Registration Software - RSVPify](https://rsvpify.com/event-registration/)
- [Top Features for Large Event Registration Systems](https://eventtechnology.org/solutions/top-features-for-large-event-registration-systems/)
- [How to Overcome Registration Bottlenecks](https://www.bizzabo.com/blog/best-online-event-registration-system)
- [15 Best Event Registration Platforms [2026]](https://www.vfairs.com/blog/best-event-registration-platforms/)
- [Event Registration System Features Guide](https://www.bizzabo.com/blog/event-registration-system-features-guide)

### Webinar Platforms
- [Best Webinar Platforms for Event Management (2026)](https://www.bizzabo.com/blog/best-webinar-platforms)
- [15 Best Webinar Software Platforms for 2026](https://webinarjam.com/blog/best-webinar-software-2026/)
- [8 Best Webinar Platforms: Features, Pricing & Reviews](https://whova.com/blog/best-webinar-platforms/)
- [Best Webinar Platforms 2026: Compare Top 15 Tools](https://webinarninja.com/blog/best-webinar-platforms/)
- [The Ultimate 2026 Guide: Choose the Best On-Demand Webinar Software](https://easywebinar.com/best-on-demand-webinar-software-guide-2026/)

### Corporate Event Management
- [Event Manager's Guide To Corporate Event Planning](https://www.perk.com/guides/corporate-event-management/)
- [Event Management for Corporate Events: 8 Best Practices](https://swoogo.events/blog/event-management-corporate-events-best-practices/)
- [The Ultimate Guide to Event Management Best Practices](https://www.cvent.com/en/blog/events/event-management-best-practices)
- [Corporate Event Planning: Types, Tips, and Common Mistakes](https://www.shms.com/en/news/corporate-event-planning/)
- [Event Management Best Practices for Unmatched Efficiency](https://gomomentus.com/blog/event-operations-best-practices)

### B2B Registration & Guest Management
- [Event Registration Management: B2B Organizer's Handbook](https://godreamcast.com/blog/solution/in-person-event/event-registration-management/)
- [A Complete Guide To Successful B2B Event Management](https://www.goodfirms.co/resources/top-tips-b2b-event-management-registration-sponsorship)
- [B2B Event Marketing Guide 2026: Strategy & Best Practices](https://www.engineerica.com/conferences-and-events/post/b2b-event-marketing/)
- [How to collect guest or plus one information](https://support.eventcreate.com/en/articles/6458053-how-to-collect-guest-or-plus-one-information)
- [Event Guest List Management & Invitee Tracking](https://rsvpify.com/guest-list-management/)

### Company Invitations & Anonymous Registration
- [Invitation and registration - Trippus](https://www.trippus.com/invitation-and-registration)
- [Event invitations, registrations, and hotel bookings - Microsoft Dynamics](https://learn.microsoft.com/en-us/dynamics365/customer-insights/journeys/invite-register-house-event-attendees)
- [How to set up invitation only registration - EventMobi](https://help.eventmobi.com/en/knowledge/invitation-only-registration)
- [Sharing a Registration Link - Recollective](https://helpdesk.recollective.com/article/92-sharing-an-invitation-link)

### Agenda & Session Management
- [Add and Customize Sessions in the Agenda](https://support.accelevents.com/en/articles/5816280-add-and-customize-sessions-in-the-agenda)
- [How to Build a Successful Event Agenda](https://sched.com/blog/event-agenda/)
- [The Ultimate Event Agenda: 4 Ways to Personalize Them](https://www.socialtables.com/blog/event-planning/personalized-event-agenda/)
- [Adding agenda session registration in the event registration page](https://support.spotme.com/hc/en-us/articles/27055126539027-Adding-agenda-session-registration-in-the-event-registration-page)
- [How to Design an Event Agenda that Keeps Attendees Engaged](https://justattend.com/blog/event-agenda-builder-tips)

### iCalendar & Calendar Integration
- [iCal Event Maker - generate ics file (iCalendar)](https://ical.marudot.com/)
- [Generate iCalendar files for events and sessions - Microsoft Dynamics](https://learn.microsoft.com/en-us/dynamics365/marketing/add-to-calendar)
- [Subscribing to and Exporting Events - The Events Calendar](https://theeventscalendar.com/knowledgebase/exporting-events/)
- [WordPress iCal Import & Export in Events Calendar Plugin](https://motopress.com/blog/wordpress-ical-import-export-events/)

---
*Feature research for: Event Management System (Broker Portal)*
*Researched: 2026-02-26*
