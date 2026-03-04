# Anforderungen mit Akzeptanzkriterien — EventCenter

Stand: 2026-03-03 (aktualisiert 2026-03-03 — Hinweis-Felder, WBNR-03 Weiterbildungsstunden)
Basis: Implementierter Code (Phasen 1–10 + Hotfixes)

---

## 1. Authentifizierung & Autorisierung

### AUTH-01 — Rollenbasierte Weiterleitung nach Login
**Als** authentifizierter Nutzer
**möchte ich** nach dem Login automatisch zur für meine Rolle vorgesehenen Startseite weitergeleitet werden
**damit** ich sofort den für mich relevanten Bereich sehe.

**Akzeptanzkriterien:**
- Admin wird nach Login zu `/admin/events` weitergeleitet
- Makler wird nach Login zu `/portal/events` weitergeleitet
- Nutzer mit unbekannter Rolle sehen eine "Zugang verweigert"-Meldung
- Unauthentifizierte Nutzer werden zur Keycloak-Loginseite weitergeleitet

### AUTH-02 — Keycloak OIDC Login
**Akzeptanzkriterien:**
- Login erfolgt über Keycloak OIDC (kein lokales Passwortformular)
- Nach erfolgreichem Login wird ein verschlüsselter Auth-Cookie gesetzt
- Realm-Rollen aus Keycloak werden als Claims übernommen und für `IsInRole()` ausgewertet
- `ReturnUrl` wird nach Login respektiert

### AUTH-03 — Logout
**Akzeptanzkriterien:**
- Logout beendet die Keycloak-Session (Single Sign-Out)
- Auth-Cookie wird gelöscht
- Nutzer wird nach Logout zur Startseite weitergeleitet

---

## 2. Veranstaltungsverwaltung (Admin)

### EVNT-01 — Veranstaltung erstellen
**Als** Admin
**möchte ich** neue Veranstaltungen anlegen
**damit** Makler und Firmen sich anmelden können.

**Akzeptanzkriterien:**
- Pflichtfelder: Titel, Ort, Startdatum, Enddatum, Anmeldefrist, Max. Kapazität
- Optionale Felder: Beschreibung, Kontaktperson, Kontakt-E-Mail, Kontakttelefon, Max. Begleitpersonen
- Optionale Hinweisfelder (mehrzeilig): Anmeldehinweis, Stornohinweis für Makler, Stornohinweis für Unternehmen
- Veranstaltungstyp wählbar: Präsenzveranstaltung oder Webinar
- Webinar: Externes Registrierungslink-Feld wird eingeblendet
- Erstellte Veranstaltung startet im Zustand "nicht veröffentlicht"
- Validierungsfehler werden inline angezeigt

### EVNT-02 — Veranstaltung bearbeiten
**Akzeptanzkriterien:**
- Alle Felder der Veranstaltung sind editierbar
- Änderungen werden mit optimistischem Concurrency-Schutz (`RowVersion`) gespeichert
- Konflikt bei gleichzeitiger Bearbeitung wird dem Nutzer angezeigt

### EVNT-03 — Veranstaltung veröffentlichen / depublizieren
**Akzeptanzkriterien:**
- Admin kann Veranstaltungen mit einem Klick veröffentlichen oder depublizieren
- Nur veröffentlichte Veranstaltungen sind für Makler sichtbar

### EVNT-04 — Veranstaltungsstatus (berechnet)
**Akzeptanzkriterien:**
- `Public`: Veranstaltung ist veröffentlicht und Anmeldefrist noch nicht abgelaufen
- `DeadlineReached`: Anmeldefrist abgelaufen, Veranstaltung noch nicht beendet
- `Finished`: Enddatum ist verstrichen
- `NotPublished`: Veranstaltung ist nicht veröffentlicht
- Status wird automatisch berechnet — kein manuelles Setzen erforderlich

### EVNT-05 — Dokumente hochladen
**Akzeptanzkriterien:**
- Admin kann Dokumente (z. B. PDFs) zu einer Veranstaltung hochladen
- Hochgeladene Dokumente sind für Makler auf der Detailseite sichtbar und downloadbar
- Dokumente werden im Dateisystem unter `wwwroot/uploads/` gespeichert
- **PDF-Dokumente** zeigen zusätzlich einen "Öffnen"-Button — öffnet das PDF in einem neuen Browser-Tab (`Content-Disposition: inline`)
- Nicht-PDF-Dokumente zeigen nur den Download-Button

### EVNT-06 — Hinweisfelder für Veranstaltungen
**Als** Admin
**möchte ich** optional Hinweistexte zu einer Veranstaltung hinterlegen
**damit** Makler und Firmen über Anmeldebedingungen und Stornoregelungen informiert werden.

**Akzeptanzkriterien:**
- Drei optionale Freitext-Felder (mehrzeilig) im Veranstaltungsformular:
  - **Anmeldehinweis**: allgemeiner Hinweis, sichtbar für Makler und Unternehmen
  - **Stornohinweis für Makler**: Stornobedingungen, sichtbar nur auf der Makler-Detailseite
  - **Stornohinweis für Unternehmen**: Stornobedingungen, sichtbar nur auf der Firmenbuchungsseite
- Leere Felder erzeugen keine leeren Alert-Boxen in der Ansicht
- Zeilenumbrüche im Text werden in der Anzeige erhalten (`white-space: pre-wrap`)
- Anmeldehinweis erscheint als blaue Info-Box, Stornohinweise als gelbe Warn-Box
- Auf der Makler-Detailseite: Anmeldehinweis + Stornohinweis für Makler werden nach der Beschreibung angezeigt
- Auf der Firmenbuchungsseite: Anmeldehinweis + Stornohinweis für Unternehmen werden im Formular- und Buchungsstatus-View angezeigt

---

## 3. Agendaverwaltung (Admin)

### AGND-01 — Agendapunkte anlegen
**Als** Admin
**möchte ich** Agendapunkte einer Veranstaltung zuordnen
**damit** Makler und Firmen gezielt Programmteile auswählen können.

**Akzeptanzkriterien:**
- Pflichtfelder: Titel, Startzeit, Endzeit, Preis für Makler, Preis für Gast
- Optional: Beschreibung, Max. Teilnehmer, Pflicht-Flag
- Steuerung ob Makler und/oder Gäste an einem Agendapunkt teilnehmen können
- Agendapunkte sind einer Veranstaltung zugeordnet und werden mit ihr gelöscht

### AGND-02 — Agendapunkte bearbeiten und löschen
**Akzeptanzkriterien:**
- Inline-Bearbeitung auf der Veranstaltungsformularseite
- Löschen möglich, solange keine Anmeldungen für den Agendapunkt existieren

### AGND-03 — Pflicht-Agendapunkte
**Akzeptanzkriterien:**
- Als `IsMandatory` markierte Agendapunkte sind für Makler nicht abwählbar
- Kosten für Pflicht-Agendapunkte werden immer in die Gesamtkosten eingerechnet

---

## 4. Zusatzoptionen (Admin)

### XOPT-01 — Zusatzoptionen anlegen
**Als** Admin
**möchte ich** buchbare Zusatzoptionen zu einer Veranstaltung hinzufügen
**damit** Firmen beim Buchen optionale Leistungen wählen können.

**Akzeptanzkriterien:**
- Pflichtfelder: Name, Preis
- Optional: Beschreibung, maximale Menge
- Optionen sind der Veranstaltung zugeordnet

### XOPT-02 — Zusatzoptionen bei Firmenbuchung auswählbar
**Akzeptanzkriterien:**
- Verfügbare Optionen werden auf der Firmenbuchungsseite angezeigt
- Auswahl beeinflusst die Gesamtkostenberechnung in Echtzeit

---

## 5. Makler-Veranstaltungssuche (Makler)

### MLST-01 — Veranstaltungsübersicht
**Als** Makler
**möchte ich** alle veröffentlichten Veranstaltungen sehen
**damit** ich mich anmelden kann.

**Akzeptanzkriterien:**
- Nur veröffentlichte Veranstaltungen (`IsPublished = true`) werden angezeigt
- Karten zeigen: Titel, Datum, Ort, Veranstaltungstyp-Icon (Präsenz/Webinar), freie Plätze
- Abgelaufene Veranstaltungen werden als "Anmeldefrist abgelaufen" gekennzeichnet

### MLST-02 — Suche und Filterung
**Akzeptanzkriterien:**
- Freitextsuche nach Titel und Ort
- Datumsfilter (von/bis)
- Filter arbeiten in Kombination

### MLST-03 — Anmeldestatus-Anzeige
**Akzeptanzkriterien:**
- "Freie Plätze: X" wenn Plätze verfügbar
- "Angemeldet" wenn Makler bereits angemeldet
- "Ausgebucht" wenn Kapazität erreicht
- "Anmeldefrist abgelaufen" wenn Deadline überschritten

---

## 6. Makler-Anmeldung (Makler)

### MREG-01 — Für Veranstaltung anmelden
**Als** Makler
**möchte ich** mich für eine Veranstaltung und einzelne Agendapunkte anmelden
**damit** meine Teilnahme registriert ist.

**Akzeptanzkriterien:**
- Makler kann aus verfügbaren Agendapunkten (MaklerCanParticipate = true) wählen
- Pflicht-Agendapunkte sind vorausgewählt und nicht abwählbar
- Kosten pro Agendapunkt werden angezeigt
- Gesamtkosten werden in Echtzeit berechnet
- Anmeldung nicht möglich nach Ablauf der Anmeldefrist
- Anmeldung nicht möglich wenn Kapazität erschöpft
- Doppelte Anmeldung (gleiche E-Mail) wird verhindert

### MREG-02 — Anmeldebestätigung per E-Mail
**Akzeptanzkriterien:**
- Nach erfolgreicher Anmeldung erhält der Makler eine Bestätigungs-E-Mail
- E-Mail enthält: Veranstaltungstitel, Datum, Ort, gewählte Agendapunkte, Gesamtkosten

### MREG-03 — Bestätigungsseite
**Akzeptanzkriterien:**
- Nach Absenden wird eine Bestätigungsseite angezeigt
- Seite zeigt Buchungsdetails und Download-Link für iCal-Termin

---

## 7. Begleitpersonenverwaltung (Makler)

### GREG-01 — Begleitperson anmelden
**Als** Makler
**möchte ich** Begleitpersonen (Gäste) für eine Veranstaltung anmelden
**damit** meine Kollegen oder Geschäftspartner teilnehmen können.

**Akzeptanzkriterien:**
- Pflichtfelder: Anrede, Vorname, Nachname, E-Mail, Beziehungstyp
- Gast kann aus Agendapunkten (GuestsCanParticipate = true) wählen
- Gast-Preise (`CostForGuest`) werden verwendet

### GREG-02 — Limit für Begleitpersonen
**Akzeptanzkriterien:**
- Anzahl der Gäste ist durch `MaxCompanions` der Veranstaltung begrenzt
- Beim Erreichen des Limits wird der "Begleitperson hinzufügen"-Button deaktiviert
- Validierungsfehler werden angezeigt wenn Limit überschritten wird

### GREG-03 — Bestätigungs-E-Mail für Begleitperson
**Akzeptanzkriterien:**
- Bestätigung der Gast-Anmeldung wird an die E-Mail-Adresse des Maklers (nicht des Gastes) gesendet
- E-Mail enthält Angaben zur Begleitperson und gewählten Agendapunkten

---

## 8. Stornierung (Makler)

### MCAN-01 — Eigene Anmeldung stornieren
**Als** Makler
**möchte ich** meine Anmeldung stornieren
**damit** der Platz für andere freigegeben wird.

**Akzeptanzkriterien:**
- Stornierung nur möglich vor Ablauf der Anmeldefrist
- Optionale Angabe eines Stornierungsgrundes
- Makler erhält Bestätigungs-E-Mail nach Stornierung
- Admin erhält Benachrichtigungs-E-Mail über die Stornierung
- Stornierter Status ist in der Anmeldungsübersicht sichtbar

### MCAN-02 — Gast-Anmeldung stornieren
**Akzeptanzkriterien:**
- Makler kann Anmeldungen seiner Begleitpersonen stornieren
- Gleiche Regeln wie MCAN-01

---

## 9. Makler-Registrierungsübersicht (Makler)

### NAV-01 — Meine Anmeldungen
**Als** Makler
**möchte ich** alle meine Anmeldungen auf einen Blick sehen
**damit** ich den Überblick über meine gebuchten Veranstaltungen habe.

**Akzeptanzkriterien:**
- Auflistung aller Anmeldungen des eingeloggten Maklers als Karten
- Karte zeigt: Veranstaltungstitel, Datum, Ort, Status (aktiv/storniert)
- Stornierungen sind als solche markiert
- Link zur Veranstaltungsdetailseite

---

## 10. Kalenderintegration (Makler & Firma)

### ICAL-01 — iCal-Download
**Als** Nutzer
**möchte ich** eine Veranstaltung als Kalendertermin herunterladen
**damit** ich sie in meinen Kalender importieren kann.

**Akzeptanzkriterien:**
- Download-Link erzeugt eine gültige `.ics`-Datei nach RFC 5545
- Datei enthält: Titel, Beschreibung, Ort, Start- und Endzeit (UTC)
- Datei wird mit `Content-Type: text/calendar` ausgeliefert

### ICAL-02 — Kalender-Buttons
**Akzeptanzkriterien:**
- Drei Buttons verfügbar: "Google Calendar", "Outlook.com", "Apple / iCal"
- Google-Calendar-Link öffnet das Google-Kalender-Formular mit vorausgefüllten Feldern (neuer Tab)
- Outlook.com-Link öffnet Outlook Web mit vorausgefülltem Termin (neuer Tab)
- Apple/iCal-Link verwendet `webcal://`-Protokoll für direktes Öffnen in der Kalender-App
- Buttons erscheinen auf: Veranstaltungsdetailseite (Makler), Buchungsbestätigungsseite (Firma), gebuchter Firmenbuchungsseite

### ICAL-03 — iCal-Anhang in Einladungsmail
**Akzeptanzkriterien:**
- Jede Firmeneinladungs-E-Mail enthält `termin.ics` als Anhang
- Outlook zeigt automatisch "Zum Kalender hinzufügen"-Button
- Gmail zeigt automatisch "Add to Calendar"-Button

---

## 11. Firmeneinladung (Admin)

### COMP-01 — Firma zur Veranstaltung einladen (Einzeleinladung)
**Als** Admin
**möchte ich** einzelne Firmen zu einer Veranstaltung einladen
**damit** sie Teilnehmer anmelden und buchen können.

**Akzeptanzkriterien:**
- Firma wird über Adressbuch-Suche (Autocomplete) ausgewählt
- Auswahl befüllt Kontaktdaten automatisch
- Optionaler Prozentrabatt anwendbar auf alle Agendapunkte
- Individuelle Preisüberschreibung pro Agendapunkt möglich
- Persönliche Nachricht für die Einladungs-E-Mail
- Einladung kann als Entwurf gespeichert oder direkt versendet werden

### COMP-02 — Sammeleinladung (Batch)
**Akzeptanzkriterien:**
- Mehrere Firmen gleichzeitig über ein Multi-Select-Suchfeld auswählbar
- Ausgewählte Firmen werden als entfernbare Chips dargestellt
- Suche filtert bereits ausgewählte Firmen aus den Ergebnissen heraus
- Geteilter Prozentrabatt und persönliche Nachricht für alle Einladungen
- Alle Einladungen werden sofort versendet
- Fehlgeschlagene Einladungen werden gezählt und gemeldet; erfolgreiche werden trotzdem erstellt

### COMP-03 — Sicherer Einladungslink
**Akzeptanzkriterien:**
- Jede Einladung erhält einen kryptografisch sicheren GUID (32 Hex-Zeichen)
- Link ist eindeutig pro Firma und Veranstaltung (UNIQUE-Index in DB)
- Einladungs-E-Mail enthält den direkten Buchungslink

### COMP-04 — Einladungsstatus-Verwaltung
**Akzeptanzkriterien:**
- Status-Workflow: `Draft → Sent → Booked / Cancelled`
- Entwürfe können versendet werden (Status → Sent)
- Versendete Einladungen können erneut versendet werden
- Einladungen im Status `Booked` können nicht gelöscht werden
- Statusübersicht (Anzahl pro Status) auf der Übersichtsseite sichtbar

### COMP-05 — Einladungsübersicht
**Akzeptanzkriterien:**
- Tabellarische Auflistung aller Einladungen einer Veranstaltung
- Sortierung nach Firmenname, Status, Versanddatum
- Statusbadges (Draft, Sent, Booked, Cancelled)
- Aktionsbuttons kontextabhängig: Bearbeiten, Senden, Erneut senden, Löschen, Buchung anzeigen

---

## 12. Firmenstammdaten / Adressbuch (Admin)

### FIRM-01 — Firma anlegen
**Als** Admin
**möchte ich** Firmen im Adressbuch pflegen
**damit** ich sie schnell für Einladungen auswählen kann.

**Akzeptanzkriterien:**
- Pflichtfelder: Firmenname, Kontakt-E-Mail
- Optionale Felder: Telefon, Ansprechpartner (Vor-/Nachname), Adresse (Straße, PLZ, Ort), Notizen
- Validierung: E-Mail-Format, Maximallängen

### FIRM-02 — Firma bearbeiten und löschen
**Akzeptanzkriterien:**
- Alle Felder bearbeitbar
- Löschen verhindert wenn Firma bereits mit Einladungen verknüpft ist
- Suchanfragen (Autocomplete) suchen nach Name, E-Mail und Ort

### FIRM-03 — Adressbuch-Suche bei Einladung
**Akzeptanzkriterien:**
- Suche beginnt ab einem eingegebenen Zeichen
- Treffer zeigen: Firmenname, E-Mail, Ort
- Auswahl befüllt alle Einladungsfelder automatisch
- "Neue Firma anlegen"-Link wenn keine Treffer

---

## 13. Firmenbuchungsportal (Anonym)

### CBOK-01 — Zugang per GUID-Link ohne Login
**Als** Firmenvertreter
**möchte ich** über einen eindeutigen Link buchen
**ohne** mich registrieren zu müssen.

**Akzeptanzkriterien:**
- Seite ist ohne Authentifizierung erreichbar (`[AllowAnonymous]`)
- Ungültige oder abgelaufene Links zeigen eine Fehlermeldung mit Kontakthinweis
- Einladungen im Status `Draft` sind nicht buchbar

### CBOK-02 — Firmenbuchung mit Teilnehmerliste
**Akzeptanzkriterien:**
- Firma gibt beliebig viele Teilnehmer ein (Vorname, Nachname, E-Mail, Anrede)
- Pro Teilnehmer: Auswahl aus verfügbaren Agendapunkten mit firmenspezifischen Preisen
- Mindestens ein Teilnehmer muss eingetragen werden

### CBOK-03 — Gesamtkostenberechnung
**Akzeptanzkriterien:**
- Kosten werden in der Sidebar in Echtzeit berechnet
- Aufschlüsselung nach Teilnehmer und Zusatzoptionen
- Hinweis "zzgl. MwSt." vorhanden

### CBOK-04 — Buchungsbestätigung
**Akzeptanzkriterien:**
- Nach Absenden erscheint Bestätigungsseite mit Firma und Veranstaltungstitel
- Kalender-Buttons (Google, Outlook, Apple) auf der Bestätigungsseite verfügbar
- Admin erhält Benachrichtigungs-E-Mail mit Teilnehmerliste

### CBOK-05 — Buchungsstatus-Anzeige (Rückkehr)
**Akzeptanzkriterien:**
- Beim erneuten Öffnen des Links wird der Buchungsstatus angezeigt
- Teilnehmerliste (nicht stornierte) wird angezeigt
- Buchungsdatum wird angezeigt
- Kalender-Buttons auch auf dieser Seite verfügbar

### CBOK-06 — Stornierung und Nicht-Teilnahme
**Akzeptanzkriterien:**
- Firma kann die gesamte Buchung stornieren (mit optionalem Kommentar)
- Firma kann Nicht-Teilnahme melden (Status bleibt "Booked", IsNonParticipation = true)
- Admin erhält Benachrichtigungs-E-Mail bei Stornierung oder Nicht-Teilnahme-Meldung

### CBOK-07 — Einzelne Teilnehmer stornieren
**Als** Firmenvertreter
**möchte ich** einzelne Teilnehmer aus meiner Buchung entfernen
**damit** ich Änderungen in der Teilnehmerzusammensetzung abbilden kann.

**Akzeptanzkriterien:**
- Jede Teilnehmerzeile in der Buchungsansicht hat einen "Stornieren"-Button
- Bestätigungsmodal zeigt den Namen des Teilnehmers und ein optionales Kommentarfeld
- Nur der betreffende Teilnehmer wird storniert; andere bleiben aktiv
- Wenn der letzte aktive Teilnehmer storniert wird, wechselt die Buchung automatisch in den Status "Cancelled"
- Keine separate Benachrichtigungs-E-Mail bei Einzelstornierung

---

## 14. Teilnehmerliste & Export (Admin)

### PART-01 — Teilnehmerübersicht
**Als** Admin
**möchte ich** alle Teilnehmer einer Veranstaltung einsehen
**damit** ich die Veranstaltung koordinieren kann.

**Akzeptanzkriterien:**
- Tabellarische Ansicht mit: Firma, Name, E-Mail, gebuchte Agendapunkte, gebuchte Optionen
- Filterung nach Firma möglich
- Stornierungen sind markiert

### PART-02 — Excel-Export Teilnehmer
**Akzeptanzkriterien:**
- Export aller aktiven Teilnehmer als `.xlsx`
- Spalten: Firma, Anrede, Vorname, Nachname, E-Mail, Agendapunkte, Optionen
- Download startet direkt im Browser

### PART-03 — Excel-Export Kontaktdaten
**Akzeptanzkriterien:**
- Export mit Kontaktdaten (Name, E-Mail, Telefon) pro Firma
- Für Nachbereitung und Kommunikation geeignet

### PART-04 — Excel-Export Nicht-Teilnehmer
**Akzeptanzkriterien:**
- Export der Firmen, die Nicht-Teilnahme gemeldet haben
- Enthält: Firmenname, Kontakt-E-Mail, Kommentar

### PART-05 — Firmenlisten-Export (CSV/Excel)
**Akzeptanzkriterien:**
- Export aller eingeladenen Firmen mit Status
- Geeignet für externe Auswertungen

---

## 15. Webinar-Unterstützung (Admin & Makler)

### WBNR-01 — Webinar-Veranstaltungstyp
**Akzeptanzkriterien:**
- Veranstaltungstyp "Webinar" bei Erstellung auswählbar
- Externes Registrierungs-URL-Feld erscheint nur bei Webinaren
- Webinar-Icon unterscheidet sich vom Präsenz-Icon in der Übersicht

### WBNR-02 — Webinar-Anmeldeverhalten
**Akzeptanzkriterien:**
- Bei Webinaren mit externer URL wird der Nutzer zur externen Seite weitergeleitet
- Internes Anmeldeformular entfällt bei externer Registrierung

### WBNR-03 — Weiterbildungsstunden
**Als** Admin
**möchte ich** bei Webinaren die Anzahl der Weiterbildungsstunden hinterlegen
**damit** Makler sehen, wie viele Stunden das Webinar für ihre Weiterbildung angerechnet wird.

**Akzeptanzkriterien:**
- Optionales Dezimalzahl-Feld im Veranstaltungsformular, erscheint nur bei Webinaren
- Eingabe als Kommazahl (z. B. `1,5` oder `2,25`), gespeichert als `decimal(5,2)`
- Auf der Makler-Detailseite: Weiterbildungsstunden werden im Webinar-Banner angezeigt, wenn befüllt
- Leeres Feld erzeugt keine Anzeige

---

## 16. E-Mail-Benachrichtigungen

| ID | Auslöser | Empfänger | Inhalt |
|---|---|---|---|
| MAIL-01 | Makler-Anmeldung | Makler | Bestätigung mit Agendapunkten und Kosten |
| MAIL-02 | Gast-Anmeldung | Makler (nicht Gast) | Bestätigung Begleitperson |
| MAIL-03 | Firmeneinladung versendet | Firma (ContactEmail) | Einladungsdetails, Buchungslink, **iCal-Anhang** |
| MAIL-04 | Firmenbuchung eingereicht | Admin | Teilnehmerliste, Firmendetails |
| MAIL-05 | Firmenstornierung / Nicht-Teilnahme | Admin | Art der Meldung, Firmendetails, Kommentar |
| MAIL-06 | Makler-Stornierung | Makler | Stornierungsbestätigung mit Grund |
| MAIL-07 | Makler-Stornierung | Admin | Benachrichtigung über Stornierung |

---

## 17. Guestoo-kompatibler Events-API-Endpunkt

### API-01 — GET /api/cs/guestooevents
**Als** externes System (z. B. Makler-Verwaltungssoftware)
**möchte ich** aktuelle Veranstaltungen per REST-API abrufen
**damit** ich das EventCenter als Datenquelle nutzen kann.

**Akzeptanzkriterien:**
- Endpunkt: `GET /api/cs/guestooevents`
- Authentifizierung: Keycloak Bearer-Token (JWT) — Schema `JwtBearer`
- Rückgabe: JSON-Array von `GuestooEventDto` (alle Felder nullable)
- Filterung: nur veröffentlichte Events mit `EndDateUtc >= DateTime.Today`
- Sortierung: aufsteigend nach `StartDateUtc`
- Optionaler Header `cPAgency` wird akzeptiert (kein Filter implementiert — kein Agentur-Konzept im EventCenter)
- Leeres Array `[]` wenn keine passenden Events vorhanden
- `eventLink` zeigt auf `{BaseUrl}/portal/events/{id}`
- `location.city` enthält den Freitext aus `Event.Location`
- `availableSeats` = `MaxCapacity - Anzahl aktiver Registrierungen`
- `subtitle` und `imageUrl` immer `null` (kein Äquivalent im Domainmodell)

---

## Nicht-funktionale Anforderungen

### Sicherheit
- Alle Admin-Seiten sind durch `[Authorize(Policy = "AdminOnly")]` geschützt
- Alle Makler-Seiten sind durch `[Authorize]` geschützt
- Firmenbuchungsseite ist `[AllowAnonymous]` — Zugang nur über sicheren GUID
- Einladungscodes sind kryptografisch sicher (RFC 4122-basiert, 32 Hex-Zeichen)
- Passwörter und Credentials werden ausschließlich über Keycloak verwaltet

### Datenkonsistenz
- Optimistische Nebenläufigkeit bei Veranstaltungsbearbeitung (`RowVersion`)
- SQL Server Retry-Strategie für transiente Fehler
- Background-Tasks (E-Mail-Versand) nutzen eigene DI-Scopes um DbContext-Threading-Konflikte zu vermeiden

### Technischer Stack
- .NET 8 Blazor Web App (Static SSR + InteractiveServer)
- SQL Server mit EF Core
- Keycloak für OIDC-Authentifizierung (Cookie + JwtBearer)
- MailKit für E-Mail-Versand (SMTP)
- Ical.Net für iCalendar-Generierung
- ClosedXML für Excel-Export
- FluentValidation für serverseitige Validierung
- **Mailpit** (Docker) für lokales E-Mail-Testing (`localhost:1025` SMTP, `localhost:8025` Web-UI)

### Konfiguration
- `BaseUrl` — absolute Basis-URL der App; wird in Einladungslinks und Admin-E-Mail-Links verwendet
- `Guestoo:BaseUrl` — Basis-URL für `eventLink` im Guestoo-API-Endpunkt
- `Smtp:*` — SMTP-Verbindungsparameter (Host, Port, SSL, Credentials, Absender)
