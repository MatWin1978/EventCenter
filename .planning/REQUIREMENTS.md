# Requirements: Veranstaltungscenter

**Defined:** 2026-02-26
**Core Value:** Makler und eingeladene Firmen können sich reibungslos für Veranstaltungen anmelden, Agendapunkte auswählen und ihre Teilnahme verwalten.

## v1 Requirements

### Authentifizierung

- [x] **AUTH-01**: Admin kann sich via Keycloak im Backoffice anmelden
- [x] **AUTH-02**: Makler kann sich via Keycloak im Portal anmelden
- [x] **AUTH-03**: Unternehmensvertreter kann per GUID-Link ohne Login auf Firmenbuchung zugreifen

### Veranstaltungsverwaltung (Admin)

- [x] **EVNT-01**: Admin kann Präsenzveranstaltung anlegen (US-01)
- [x] **EVNT-02**: Admin kann Veranstaltung bearbeiten (US-02)
- [x] **EVNT-03**: Admin kann Veranstaltung veröffentlichen/zurückziehen (US-03)
- [x] **EVNT-04**: System berechnet EventState automatisch (Public, DeadlineReached, Finished)

### Webinar-Verwaltung (Admin)

- [ ] **WBNR-01**: Admin kann Webinar anlegen und bearbeiten (US-04)
- [ ] **WBNR-02**: Admin kann Webinar veröffentlichen/zurückziehen (US-04)

### Agendapunkte (Admin)

- [x] **AGND-01**: Admin kann Agendapunkt anlegen mit Kosten für Makler/Gäste (US-05)
- [x] **AGND-02**: Admin kann Agendapunkt bearbeiten und löschen (US-06)
- [x] **AGND-03**: Admin kann Teilnahme für Makler oder Gäste pro Agendapunkt deaktivieren

### Zusatzoptionen (Admin)

- [x] **XOPT-01**: Admin kann Zusatzoptionen anlegen, bearbeiten und löschen (US-07)
- [x] **XOPT-02**: System verhindert Löschen bereits gebuchter Zusatzoptionen

### Firmeneinladungen (Admin)

- [x] **COMP-01**: Admin kann Firma zu Veranstaltung einladen (US-08)
- [x] **COMP-02**: Admin kann firmenspezifische Konditionen pro Agendapunkt festlegen (US-08)
- [x] **COMP-03**: Admin kann Einladungsmail an Firma versenden (US-08)
- [x] **COMP-04**: Admin kann Firmeneinladung löschen (US-09)
- [x] **COMP-05**: Admin kann Einladungs- und Buchungsstatus einer Firma einsehen (US-10)

### Teilnehmerverwaltung (Admin)

- [x] **PART-01**: Admin kann Teilnehmerliste einer Firma einsehen (US-11)
- [x] **PART-02**: Admin kann Teilnehmerliste als Excel exportieren (US-12)
- [x] **PART-03**: Admin kann Kontaktdaten als Excel exportieren (US-12)
- [x] **PART-04**: Admin kann nicht-teilnehmende Mitglieder exportieren (US-12)
- [x] **PART-05**: Admin kann Firmenliste als Excel/CSV exportieren (US-12)

### Veranstaltungsübersicht (Makler)

- [x] **MLST-01**: Makler sieht Liste aller für ihn sichtbaren Veranstaltungen (US-13)
- [x] **MLST-02**: Makler kann nach Name/Ort suchen und nach Datum filtern (US-13)
- [x] **MLST-03**: Makler sieht Anmeldestatus pro Veranstaltung (Plätze frei, Angemeldet, Ausgebucht, Verpasst)

### Veranstaltungsdetail (Makler)

- [x] **MDET-01**: Makler sieht Veranstaltungsdetails (Titel, Beschreibung, Ort, Zeit, Kontakt) (US-14)
- [x] **MDET-02**: Makler kann Dokumente herunterladen (US-14)
- [x] **MDET-03**: Makler kann Termin als iCalendar exportieren (US-16)

### Selbstanmeldung (Makler)

- [x] **MREG-01**: Makler kann sich für Veranstaltung anmelden mit Agendapunkt-Auswahl (US-15)
- [x] **MREG-02**: System prüft Deadline, Kapazität und Berechtigung vor Anmeldung
- [x] **MREG-03**: Makler sieht Teilnahmekosten pro Agendapunkt
- [x] **MREG-04**: Makler erhält Bestätigungsseite nach erfolgreicher Anmeldung

### Gastanmeldung (Makler)

- [x] **GREG-01**: Makler kann Begleitperson für Veranstaltung anmelden (US-17)
- [x] **GREG-02**: System prüft Begleitpersonenlimit pro Makler
- [x] **GREG-03**: Makler gibt Gast-Daten ein (Anrede, Name, E-Mail, Beziehungstyp)

### Stornierung (Makler)

- [x] **MCAN-01**: Makler kann eigene Anmeldung stornieren (US-18)
- [x] **MCAN-02**: Makler kann Gastanmeldung stornieren (US-19)
- [x] **MCAN-03**: System prüft Storno-Berechtigung (nur Ersteller darf stornieren)
- [x] **MCAN-04**: System aktualisiert RegistrationCount nach Stornierung

### Firmenbuchung (Unternehmensvertreter)

- [x] **CBOK-01**: Unternehmensvertreter sieht Buchungsseite per GUID-Link (US-20)
- [x] **CBOK-02**: Unternehmensvertreter sieht firmenspezifische Preise und Agendapunkte
- [x] **CBOK-03**: Unternehmensvertreter kann beliebig viele Teilnehmer eintragen (US-21)
- [x] **CBOK-04**: Unternehmensvertreter kann Zusatzoptionen auswählen (US-21)
- [x] **CBOK-05**: System berechnet Kosten automatisch (Fixpreis + Zusatzteilnehmer)
- [x] **CBOK-06**: Unternehmensvertreter kann Buchung absenden und erhält Bestätigung
- [x] **CBOK-07**: Unternehmensvertreter kann Buchung stornieren (US-22)
- [x] **CBOK-08**: Unternehmensvertreter kann Nicht-Teilnahme melden (US-23)

### E-Mail-Benachrichtigungen

- [x] **MAIL-01**: System sendet Bestätigung an Makler nach Selbstanmeldung
- [x] **MAIL-02**: System sendet Bestätigung an Makler nach Gastanmeldung
- [x] **MAIL-03**: System sendet Einladung an Firma mit GUID-Link
- [x] **MAIL-04**: System sendet Benachrichtigung an Admin nach Firmenbuchung
- [x] **MAIL-05**: System sendet Benachrichtigung an Admin nach Firmenstorno

## v2 Requirements

### Erweiterte Funktionen

- **NOTF-01**: Push-Benachrichtigungen im Portal
- **NOTF-02**: Erinnerungs-E-Mails vor Anmeldefrist
- **WAIT-01**: Warteliste bei ausgebuchten Events
- **REPT-01**: Dashboard mit Statistiken
- **MULT-01**: Mehrsprachigkeit (Deutsch/Englisch)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Mobile App | Web-first, responsive Design reicht für v1 |
| Zahlungsintegration | Abrechnung erfolgt extern |
| Mehrsprachigkeit | Deutsch only für v1 |
| Umbraco-Integration | Standalone-System mit eigenem Backend |
| Echtzeit-Chat | Moderationsaufwand, nicht Kernfunktion |
| Automatische Warteliste | Race-Conditions, Komplexität |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| AUTH-01 | Phase 1 | Complete |
| AUTH-02 | Phase 1 | Complete |
| AUTH-03 | Phase 5 | Complete |
| EVNT-01 | Phase 2 | Complete |
| EVNT-02 | Phase 2 | Complete |
| EVNT-03 | Phase 2 | Complete |
| EVNT-04 | Phase 2 | Complete |
| WBNR-01 | Phase 8 | Pending |
| WBNR-02 | Phase 8 | Pending |
| AGND-01 | Phase 2 | Complete |
| AGND-02 | Phase 2 | Complete |
| AGND-03 | Phase 2 | Complete |
| XOPT-01 | Phase 2 | Complete |
| XOPT-02 | Phase 2 | Complete |
| COMP-01 | Phase 4 | Complete |
| COMP-02 | Phase 4 | Complete |
| COMP-03 | Phase 4 | Complete |
| COMP-04 | Phase 4 | Complete |
| COMP-05 | Phase 4 | Complete |
| PART-01 | Phase 7 | Complete |
| PART-02 | Phase 7 | Complete |
| PART-03 | Phase 7 | Complete |
| PART-04 | Phase 7 | Complete |
| PART-05 | Phase 7 | Complete |
| MLST-01 | Phase 3 | Complete |
| MLST-02 | Phase 3 | Complete |
| MLST-03 | Phase 3 | Complete |
| MDET-01 | Phase 3 | Complete |
| MDET-02 | Phase 3 | Complete |
| MDET-03 | Phase 3 | Complete (03-01) |
| MREG-01 | Phase 3 | Complete |
| MREG-02 | Phase 3 | Complete (03-01) |
| MREG-03 | Phase 3 | Complete |
| MREG-04 | Phase 3 | Complete |
| GREG-01 | Phase 6 | Complete |
| GREG-02 | Phase 6 | Complete |
| GREG-03 | Phase 6 | Complete |
| MCAN-01 | Phase 7 | Complete |
| MCAN-02 | Phase 7 | Complete |
| MCAN-03 | Phase 7 | Complete |
| MCAN-04 | Phase 7 | Complete |
| CBOK-01 | Phase 5 | Complete |
| CBOK-02 | Phase 5 | Complete |
| CBOK-03 | Phase 5 | Complete |
| CBOK-04 | Phase 5 | Complete |
| CBOK-05 | Phase 5 | Complete |
| CBOK-06 | Phase 5 | Complete |
| CBOK-07 | Phase 5 | Complete |
| CBOK-08 | Phase 5 | Complete |
| MAIL-01 | Phase 3 | Complete (03-01) |
| MAIL-02 | Phase 6 | Complete |
| MAIL-03 | Phase 4 | Complete |
| MAIL-04 | Phase 5 | Complete |
| MAIL-05 | Phase 5 | Complete |

**Coverage:**
- v1 requirements: 52 total
- Mapped to phases: 52
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-26*
*Last updated: 2026-02-26 after roadmap creation*
