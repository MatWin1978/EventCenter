# Veranstaltungscenter

## What This Is

Ein Standalone-WebTool zur Verwaltung und Buchung von Präsenzveranstaltungen und Webinaren für ein Maklerportal. Drei Nutzergruppen (Admin, Makler, Unternehmensvertreter) können Veranstaltungen verwalten, sich anmelden und Firmenbuchungen durchführen.

## Core Value

Makler und eingeladene Firmen können sich reibungslos für Veranstaltungen anmelden, Agendapunkte auswählen und ihre Teilnahme verwalten.

## Requirements

### Validated

(None yet — ship to validate)

### Active

**Admin-Bereich:**
- [ ] Präsenzveranstaltungen anlegen, bearbeiten, veröffentlichen (US-01 bis US-03)
- [ ] Webinare anlegen und verwalten (US-04)
- [ ] Agendapunkte verwalten mit Kosten für Makler/Gäste (US-05, US-06)
- [ ] Zusatzoptionen verwalten (US-07)
- [ ] Firmen zu Veranstaltungen einladen mit Sonderkonditionen (US-08 bis US-10)
- [ ] Teilnehmerlisten einsehen und exportieren (US-11, US-12)

**Makler-Portal:**
- [ ] Veranstaltungsübersicht mit Suche und Filterung (US-13)
- [ ] Veranstaltungsdetails mit Anmeldestatus (US-14)
- [ ] Selbstanmeldung mit Agendapunkt-Auswahl (US-15)
- [ ] iCalendar-Export (US-16)
- [ ] Gastanmeldung (Begleitpersonen) (US-17)
- [ ] Anmeldung stornieren (US-18, US-19)

**Firmenportal (anonym):**
- [ ] Firmenbuchungsseite per GUID-Link (US-20)
- [ ] Teilnehmer eintragen und Buchung absenden (US-21)
- [ ] Buchung stornieren (US-22)
- [ ] Nicht-Teilnahme melden (US-23)

### Out of Scope

- Mobile App — Web-first, responsive Design reicht für v1
- Zahlungsintegration — Abrechnung erfolgt extern
- Mehrsprachigkeit — Deutsch only für v1
- Umbraco-Integration — Standalone-System mit eigenem Backend

## Context

**Domainmodell:**
Das System basiert auf einem klar definierten Domainmodell (siehe `EventCenter/docs/veranstaltungscenter-domainmodell.md`):
- `Event` / `WebinarEvent` (IEvent-Interface)
- `EventAgendaItem` mit `EventAgendaItemsSpecial` für Firmen-Sonderkonditionen
- `EventExtraOption` für buchbare Zusatzoptionen
- `EventCompany` mit `EventCompanyParticipant` für Firmenteilnehmer
- `MemberEventRegistration` / `GuestEventRegistration` für Makler-Anmeldungen

**Geschäftsregeln:**
- Anmeldefrist gilt bis einschließlich des Fristtags
- Ein Makler kann sich pro Veranstaltung nur einmal selbst anmelden
- Begleitpersonenlimit gilt pro Makler
- Firmenzugang erfolgt anonym über eindeutigen GUID-Link
- Nur der Ersteller einer Anmeldung darf stornieren

**User Stories:**
23 detaillierte User Stories in 11 Epics (siehe `EventCenter/docs/veranstaltungscenter-user-stories.md`)

## Constraints

- **Tech Stack**: ASP.NET Core + Blazor Server — passt zum .NET-Ökosystem
- **Datenbank**: SQL Server — Enterprise-Standard
- **Authentifizierung**: Keycloak/OIDC — zentrales Identity Management
- **Deployment**: Standalone-Anwendung mit eigener Datenbank

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Blazor Server statt WASM | Einfachere Entwicklung, direkter DB-Zugriff, kein separates API nötig | — Pending |
| Keycloak für Auth | Zentrales Identity Management, Rollen-basiert, SSO-fähig | — Pending |
| SQL Server | Enterprise-tauglich, EF Core Integration, bekannte Technologie | — Pending |
| Standalone statt Umbraco | Klare Trennung, keine Legacy-Abhängigkeiten, einfacher wartbar | — Pending |

---
*Last updated: 2026-03-03 — Hinweis-Felder (Anmeldehinweis, Stornohinweis Makler/Unternehmen) zu Event-Entity und allen betroffenen Seiten hinzugefügt (EVNT-06)*
