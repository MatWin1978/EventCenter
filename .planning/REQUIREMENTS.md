# Requirements: Veranstaltungscenter

**Defined:** 2026-02-26
**Core Value:** Makler und eingeladene Firmen können sich reibungslos für Veranstaltungen anmelden, Agendapunkte auswählen und ihre Teilnahme verwalten.

## v1 Requirements

### Authentifizierung

- [x] **AUTH-01**: Admin kann sich via Keycloak im Backoffice anmelden
- [x] **AUTH-02**: Makler kann sich via Keycloak im Portal anmelden
- [ ] **AUTH-03**: Unternehmensvertreter kann per GUID-Link ohne Login auf Firmenbuchung zugreifen

### Veranstaltungsverwaltung (Admin)

- [ ] **EVNT-01**: Admin kann Präsenzveranstaltung anlegen (US-01)
- [ ] **EVNT-02**: Admin kann Veranstaltung bearbeiten (US-02)
- [ ] **EVNT-03**: Admin kann Veranstaltung veröffentlichen/zurückziehen (US-03)
- [x] **EVNT-04**: System berechnet EventState automatisch (Public, DeadlineReached, Finished)

### Webinar-Verwaltung (Admin)

- [ ] **WBNR-01**: Admin kann Webinar anlegen und bearbeiten (US-04)
- [ ] **WBNR-02**: Admin kann Webinar veröffentlichen/zurückziehen (US-04)

### Agendapunkte (Admin)

- [x] **AGND-01**: Admin kann Agendapunkt anlegen mit Kosten für Makler/Gäste (US-05)
- [ ] **AGND-02**: Admin kann Agendapunkt bearbeiten und löschen (US-06)
- [x] **AGND-03**: Admin kann Teilnahme für Makler oder Gäste pro Agendapunkt deaktivieren

### Zusatzoptionen (Admin)

- [x] **XOPT-01**: Admin kann Zusatzoptionen anlegen, bearbeiten und löschen (US-07)
- [ ] **XOPT-02**: System verhindert Löschen bereits gebuchter Zusatzoptionen

### Firmeneinladungen (Admin)

- [ ] **COMP-01**: Admin kann Firma zu Veranstaltung einladen (US-08)
- [ ] **COMP-02**: Admin kann firmenspezifische Konditionen pro Agendapunkt festlegen (US-08)
- [ ] **COMP-03**: Admin kann Einladungsmail an Firma versenden (US-08)
- [ ] **COMP-04**: Admin kann Firmeneinladung löschen (US-09)
- [ ] **COMP-05**: Admin kann Einladungs- und Buchungsstatus einer Firma einsehen (US-10)

### Teilnehmerverwaltung (Admin)

- [ ] **PART-01**: Admin kann Teilnehmerliste einer Firma einsehen (US-11)
- [ ] **PART-02**: Admin kann Teilnehmerliste als Excel exportieren (US-12)
- [ ] **PART-03**: Admin kann Kontaktdaten als Excel exportieren (US-12)
- [ ] **PART-04**: Admin kann nicht-teilnehmende Mitglieder exportieren (US-12)
- [ ] **PART-05**: Admin kann Firmenliste als Excel/CSV exportieren (US-12)

### Veranstaltungsübersicht (Makler)

- [ ] **MLST-01**: Makler sieht Liste aller für ihn sichtbaren Veranstaltungen (US-13)
- [ ] **MLST-02**: Makler kann nach Name/Ort suchen und nach Datum filtern (US-13)
- [ ] **MLST-03**: Makler sieht Anmeldestatus pro Veranstaltung (Plätze frei, Angemeldet, Ausgebucht, Verpasst)

### Veranstaltungsdetail (Makler)

- [ ] **MDET-01**: Makler sieht Veranstaltungsdetails (Titel, Beschreibung, Ort, Zeit, Kontakt) (US-14)
- [ ] **MDET-02**: Makler kann Dokumente herunterladen (US-14)
- [ ] **MDET-03**: Makler kann Termin als iCalendar exportieren (US-16)

### Selbstanmeldung (Makler)

- [ ] **MREG-01**: Makler kann sich für Veranstaltung anmelden mit Agendapunkt-Auswahl (US-15)
- [ ] **MREG-02**: System prüft Deadline, Kapazität und Berechtigung vor Anmeldung
- [ ] **MREG-03**: Makler sieht Teilnahmekosten pro Agendapunkt
- [ ] **MREG-04**: Makler erhält Bestätigungsseite nach erfolgreicher Anmeldung

### Gastanmeldung (Makler)

- [ ] **GREG-01**: Makler kann Begleitperson für Veranstaltung anmelden (US-17)
- [ ] **GREG-02**: System prüft Begleitpersonenlimit pro Makler
- [ ] **GREG-03**: Makler gibt Gast-Daten ein (Anrede, Name, Adresse, Beziehungstyp)

### Stornierung (Makler)

- [ ] **MCAN-01**: Makler kann eigene Anmeldung stornieren (US-18)
- [ ] **MCAN-02**: Makler kann Gastanmeldung stornieren (US-19)
- [ ] **MCAN-03**: System prüft Storno-Berechtigung (nur Ersteller darf stornieren)
- [ ] **MCAN-04**: System aktualisiert RegistrationCount nach Stornierung

### Firmenbuchung (Unternehmensvertreter)

- [ ] **CBOK-01**: Unternehmensvertreter sieht Buchungsseite per GUID-Link (US-20)
- [ ] **CBOK-02**: Unternehmensvertreter sieht firmenspezifische Preise und Agendapunkte
- [ ] **CBOK-03**: Unternehmensvertreter kann beliebig viele Teilnehmer eintragen (US-21)
- [ ] **CBOK-04**: Unternehmensvertreter kann Zusatzoptionen auswählen (US-21)
- [ ] **CBOK-05**: System berechnet Kosten automatisch (Fixpreis + Zusatzteilnehmer)
- [ ] **CBOK-06**: Unternehmensvertreter kann Buchung absenden und erhält Bestätigung
- [ ] **CBOK-07**: Unternehmensvertreter kann Buchung stornieren (US-22)
- [ ] **CBOK-08**: Unternehmensvertreter kann Nicht-Teilnahme melden (US-23)

### E-Mail-Benachrichtigungen

- [ ] **MAIL-01**: System sendet Bestätigung an Makler nach Selbstanmeldung
- [ ] **MAIL-02**: System sendet Bestätigung an Makler nach Gastanmeldung
- [ ] **MAIL-03**: System sendet Einladung an Firma mit GUID-Link
- [ ] **MAIL-04**: System sendet Benachrichtigung an Admin nach Firmenbuchung
- [ ] **MAIL-05**: System sendet Benachrichtigung an Admin nach Firmenstorno

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
| AUTH-03 | Phase 5 | Pending |
| EVNT-01 | Phase 2 | Pending |
| EVNT-02 | Phase 2 | Pending |
| EVNT-03 | Phase 2 | Pending |
| EVNT-04 | Phase 2 | Complete |
| WBNR-01 | Phase 8 | Pending |
| WBNR-02 | Phase 8 | Pending |
| AGND-01 | Phase 2 | Complete |
| AGND-02 | Phase 2 | Pending |
| AGND-03 | Phase 2 | Complete |
| XOPT-01 | Phase 2 | Complete |
| XOPT-02 | Phase 2 | Pending |
| COMP-01 | Phase 4 | Pending |
| COMP-02 | Phase 4 | Pending |
| COMP-03 | Phase 4 | Pending |
| COMP-04 | Phase 4 | Pending |
| COMP-05 | Phase 4 | Pending |
| PART-01 | Phase 7 | Pending |
| PART-02 | Phase 7 | Pending |
| PART-03 | Phase 7 | Pending |
| PART-04 | Phase 7 | Pending |
| PART-05 | Phase 7 | Pending |
| MLST-01 | Phase 3 | Pending |
| MLST-02 | Phase 3 | Pending |
| MLST-03 | Phase 3 | Pending |
| MDET-01 | Phase 3 | Pending |
| MDET-02 | Phase 3 | Pending |
| MDET-03 | Phase 3 | Pending |
| MREG-01 | Phase 3 | Pending |
| MREG-02 | Phase 3 | Pending |
| MREG-03 | Phase 3 | Pending |
| MREG-04 | Phase 3 | Pending |
| GREG-01 | Phase 6 | Pending |
| GREG-02 | Phase 6 | Pending |
| GREG-03 | Phase 6 | Pending |
| MCAN-01 | Phase 7 | Pending |
| MCAN-02 | Phase 7 | Pending |
| MCAN-03 | Phase 7 | Pending |
| MCAN-04 | Phase 7 | Pending |
| CBOK-01 | Phase 5 | Pending |
| CBOK-02 | Phase 5 | Pending |
| CBOK-03 | Phase 5 | Pending |
| CBOK-04 | Phase 5 | Pending |
| CBOK-05 | Phase 5 | Pending |
| CBOK-06 | Phase 5 | Pending |
| CBOK-07 | Phase 5 | Pending |
| CBOK-08 | Phase 5 | Pending |
| MAIL-01 | Phase 3 | Pending |
| MAIL-02 | Phase 6 | Pending |
| MAIL-03 | Phase 4 | Pending |
| MAIL-04 | Phase 5 | Pending |
| MAIL-05 | Phase 5 | Pending |

**Coverage:**
- v1 requirements: 52 total
- Mapped to phases: 52
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-26*
*Last updated: 2026-02-26 after roadmap creation*
