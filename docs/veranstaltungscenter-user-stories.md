# Veranstaltungscenter – User Stories

## Übersicht

Das Veranstaltungscenter ermöglicht die Verwaltung und Buchung von Präsenzveranstaltungen und Webinaren im Maklerportal. Es gibt drei Akteursgruppen:

| Akteur | Beschreibung |
|---|---|
| **Admin** | Umbraco-Backoffice-Benutzer (Redakteur/Administrator) |
| **Makler** | Authentifiziertes Portalmitglied (MemberAuthorize) |
| **Unternehmensvertreter** | Externer Nutzer ohne Login (Anonymous, per GUID-Link eingeladen) |

---

## Epics

1. [Veranstaltungsverwaltung (Admin)](#1-veranstaltungsverwaltung-admin)
2. [Webinar-Verwaltung (Admin)](#2-webinar-verwaltung-admin)
3. [Agendapunkte verwalten (Admin)](#3-agendapunkte-verwalten-admin)
4. [Zusatzoptionen verwalten (Admin)](#4-zusatzoptionen-verwalten-admin)
5. [Unternehmenseinladungen verwalten (Admin)](#5-unternehmenseinladungen-verwalten-admin)
6. [Teilnehmerverwaltung und Export (Admin)](#6-teilnehmerverwaltung-und-export-admin)
7. [Veranstaltungsübersicht (Makler)](#7-veranstaltungsübersicht-makler)
8. [Veranstaltungsdetail & Anmeldung (Makler)](#8-veranstaltungsdetail--anmeldung-makler)
9. [Gastanmeldung (Makler)](#9-gastanmeldung-makler)
10. [Anmeldung stornieren (Makler)](#10-anmeldung-stornieren-makler)
11. [Firmenbuchung (Unternehmensvertreter)](#11-firmenbuchung-unternehmensvertreter)

---

## 1. Veranstaltungsverwaltung (Admin)

---

### US-01 – Veranstaltung anlegen

**Als** Admin
**möchte ich** eine neue Präsenzveranstaltung anlegen,
**damit** Makler sich dafür registrieren können.

#### Akzeptanzkriterien

**Formularfelder (alle Pflichtfelder sofern nicht anders angegeben):**

| Feld | Typ | Pflicht | Validierung / Hinweise |
|---|---|---|---|
| Titel | Text | ja | – |
| Beschreibung | Rich-Text-Editor | nein | Toolbar: Undo, Redo, Bold, Italic, Underline, Strikethrough, Ausrichtung, Listen, Link, Medienpicker, Tabelle |
| Startdatum | DateTime | ja | Format: DD.MM.YYYY HH:mm, ohne 12-Stunden-Anzeige |
| Enddatum | DateTime | ja | Format: DD.MM.YYYY HH:mm |
| Veranstaltungsort (Name) | Text | nein | – |
| Straße | Text | nein | – |
| Hausnummer | Text | nein | – |
| PLZ | Text | nein | – |
| Stadt | Text | nein | – |
| Teilnehmerlimit | Integer | ja | Min: 1 |
| Begleitpersonenlimit | Integer | nein | Min: 0; wenn gesetzt, begrenzt es Gastanmeldungen pro Makler |
| Anmeldefrist | Datum | ja | Format: DD.MM.YYYY |
| Weiterbildungsstunden | Decimal | nein | Min: 0 |
| Ansprechpartner Name | Text | nein | – |
| Ansprechpartner E-Mail | Text | nein | – |
| Anmeldehinweis | Textarea | nein | Wird Maklern im Anmeldeformular angezeigt |
| Stornohinweis (Makler) | Textarea | nein | Wird bei Stornierung angezeigt |
| Stornohinweis (Firmen) | Textarea | nein | Wird im Firmenportal angezeigt |
| Teaser-Bild | Media Picker | nein | Einzelauswahl |
| Dokumente | Media Picker | nein | Mehrfachauswahl |
| Mitgliedergruppen | MemberGroup Picker | nein | Mehrfachauswahl; steuert Zugriff |

**Verhalten:**
- POST an `/umbraco/api/EventAdministrationApi/CreateEvent`
- Bei Erfolg: Weiterleitung auf Bearbeitungsseite der neuen Veranstaltung, Erfolgsmeldung "Veranstaltung erstellt"
- Bei Fehler: Fehlermeldung "Die Veranstaltung konnte nicht erfolgreich erstellt werden."
- Neue Veranstaltung hat initial `PublicationState = NotPublic`
- Nach dem Anlegen wird automatisch ein Standard-Agendapunkt erstellt (`CreateEventWithDefaultAgendaItem`)

---

### US-02 – Veranstaltung bearbeiten

**Als** Admin
**möchte ich** eine bestehende Veranstaltung bearbeiten,
**damit** ich Änderungen an Inhalten, Ort und Teilnahmekonditionen vornehmen kann.

#### Akzeptanzkriterien

- GET `/umbraco/api/EventAdministrationApi/GetEvent?eventId={id}` lädt die Veranstaltungsdaten vor
- Formularfelder identisch mit US-01
- PUT an `/umbraco/api/EventAdministrationApi/UpdateEvent?eventId={id}`
- Bei Erfolg: Seite neu laden, Erfolgsmeldung "Veranstaltung aktualisiert"
- Bei Fehler: Fehlermeldung "Die Daten der Veranstaltung konnten nicht erfolgreich aktualisiert werden."
- **Warnung:** Wenn bereits Anmeldungen existieren, wird ein Warnhinweis angezeigt: *"Achtung, für diese Veranstaltung existieren bereits Anmeldungen! Änderungen können gegebenenfalls zu Problemen führen."*

---

### US-03 – Veranstaltung veröffentlichen / zurückziehen

**Als** Admin
**möchte ich** eine Veranstaltung veröffentlichen oder wieder deaktivieren,
**damit** ich steuern kann, wann Makler die Veranstaltung sehen und sich anmelden können.

#### Akzeptanzkriterien

**Veröffentlichen:**
- PUT an `/umbraco/api/EventAdministrationApi/PublishEvent?eventId={id}`
- Setzt `PublicationState = Public`
- Veranstaltung erscheint in der Portalansicht für berechtigte Makler

**Zurückziehen:**
- PUT an `/umbraco/api/EventAdministrationApi/UnpublishEvent?eventId={id}`
- Setzt `PublicationState = NotPublic`
- Veranstaltung verschwindet aus der Portalansicht
- Bestehende Anmeldungen bleiben erhalten

**Computed State (`EventState`)** – wird automatisch berechnet, nicht gesetzt:

| Bedingung | State |
|---|---|
| `PublicationState == NotPublic` | `NotPublic` |
| `PublicationState == Public` AND `End < heute` | `Finished` |
| `PublicationState == Public` AND `RegistrationDeadline + 1 Tag < jetzt` | `DeadlineReached` |
| `PublicationState == Public` AND Deadline noch nicht abgelaufen | `Public` |

---

## 2. Webinar-Verwaltung (Admin)

---

### US-04 – Webinar anlegen und bearbeiten

**Als** Admin
**möchte ich** ein Webinar (externes Online-Event) anlegen,
**damit** Makler über das Portal davon erfahren und sich extern registrieren können.

#### Akzeptanzkriterien

**Formularfelder:**

| Feld | Typ | Pflicht | Hinweise |
|---|---|---|---|
| Titel | Text | ja | – |
| Beschreibung | Rich-Text-Editor | nein | – |
| Startdatum | DateTime | ja | – |
| Enddatum | DateTime | ja | – |
| Anmeldefrist | Datum | ja | – |
| Registrierungslink | URL/Text | ja | Externer Link (z. B. GoToWebinar) |
| Weiterbildungsstunden | Decimal | nein | – |
| Dokumente | Media Picker | nein | – |
| Mitgliedergruppen | MemberGroup Picker | nein | – |

- POST an `/umbraco/api/EventAdministrationApi/CreateWebinarEvent`
- PUT an `/umbraco/api/EventAdministrationApi/UpdateWebinarEvent?webinarEventId={id}`
- Webinar hat keinen eigenen Ort – `Location` gibt immer "GoToWebinar" zurück
- `AvailableSeats` gibt immer "siehe Webseite" zurück
- `EventType = Webinar`

**Veröffentlichen/Zurückziehen:**
- PUT `/umbraco/api/EventAdministrationApi/PublishWebinarEvent?webinarEventId={id}`
- PUT `/umbraco/api/EventAdministrationApi/UnpublishWebinarEvent?webinarEventId={id}`

---

## 3. Agendapunkte verwalten (Admin)

---

### US-05 – Agendapunkt anlegen

**Als** Admin
**möchte ich** einer Veranstaltung Agendapunkte (Programmpunkte) hinzufügen,
**damit** Makler und Gäste auswählen können, an welchen Teilen sie teilnehmen.

#### Akzeptanzkriterien

**Formularfelder:**

| Feld | Typ | Pflicht | Hinweise |
|---|---|---|---|
| Name | Text | ja | Bezeichnung des Programmpunkts |
| Programmpunktnummer | Integer | ja | Sortierreihenfolge (aufsteigend) |
| Maklerteilnahme deaktiviert | Boolean | nein | `MembersParticipationDisabled`; wenn true, können sich Makler für diesen Punkt nicht anmelden |
| Kosten Makler | Decimal | nein | `MembersParticipationCost`; Teilnahmekosten für Makler |
| Gastteilnahme deaktiviert | Boolean | nein | `CompanionParticipationDisabled`; wenn true, können Gäste nicht angemeldet werden |
| Kosten Gast | Decimal | nein | `CompanionParticipationCost` |

- POST an `/umbraco/api/EventAdministrationApi/CreateAgendaItem?eventId={id}`
- GET `/umbraco/api/EventAdministrationApi/GetAgendaItems?eventId={id}` lädt alle Punkte (sortiert aufsteigend)

---

### US-06 – Agendapunkt bearbeiten und löschen

**Als** Admin
**möchte ich** bestehende Agendapunkte bearbeiten oder entfernen.

#### Akzeptanzkriterien

- PUT `/umbraco/api/EventAdministrationApi/UpdateAgendaItem?agendaItemId={id}` – gleiche Felder wie US-05
- DELETE `/umbraco/api/EventAdministrationApi/DeleteAgendaItem?agendaItemId={id}`
- Wenn ein Datenbankfehler beim Update auftritt (z. B. aktive Anmeldungen), wird eine benutzerfreundliche Fehlermeldung angezeigt

---

## 4. Zusatzoptionen verwalten (Admin)

---

### US-07 – Zusatzoptionen anlegen, bearbeiten und löschen

**Als** Admin
**möchte ich** buchbare Zusatzoptionen für eine Veranstaltung verwalten (z. B. Unterkunft, Abendessen),
**damit** eingeladene Firmen diese auswählen können.

#### Akzeptanzkriterien

**Formularfelder:**

| Feld | Typ | Pflicht |
|---|---|---|
| Name | Text | ja |
| Preis | Decimal | ja |

- POST `/umbraco/api/EventAdministrationApi/CreateExtraOption?eventId={id}`
- PUT `/umbraco/api/EventAdministrationApi/UpdateExtraOption?extraOptionId={id}`
- DELETE `/umbraco/api/EventAdministrationApi/DeleteExtraOption?extraOptionId={id}`
  - **Fehlerfall:** Wenn die Option bereits von einer Firma gebucht wurde, schlägt das Löschen fehl mit der Meldung: *"Die Zusatzoption wurde bereits gebucht und kann deshalb nicht gelöscht werden."*
- GET `/umbraco/api/EventAdministrationApi/GetEventExtraOptions?eventId={id}` – gibt alle Optionen sortiert aufsteigend zurück

---

## 5. Unternehmenseinladungen verwalten (Admin)

---

### US-08 – Firma zu Veranstaltung einladen

**Als** Admin
**möchte ich** eine Firma zu einer Veranstaltung einladen,
**damit** der Unternehmensvertreter Teilnehmer und Buchungsoptionen eintragen kann.

#### Akzeptanzkriterien

**Ablauf:**
1. Admin wählt eine Firma aus einer Dropdown-Liste aus
2. Für jede Kombination Veranstaltung + Firma existiert genau ein `EventCompany`-Datensatz
3. Admin konfiguriert die firmenspezifischen Konditionen pro Agendapunkt (`EventAgendaItemsSpecial`)

**Formularfelder für Firmenkonditionen:**

| Feld | Typ | Pflicht | Hinweise |
|---|---|---|---|
| Fixpreis | Decimal | nein | `PriceFix` auf `EventCompany` |
| Preis pro Teilnehmer | Decimal | nein | `PricePerParticipant` auf `EventCompany` |
| Kommentar zur Einladung | Rich-Text-Editor | nein | Interne Anmerkung |

**Felder pro Agendapunkt (`EventAgendaItemsSpecial`):**

| Feld | Typ | Hinweise |
|---|---|---|
| Sichtbar | Boolean | `IsVisible`; steuert, ob der Punkt im Firmenportal angezeigt wird |
| Fixpreis | Decimal | Pauschalpreis für diesen Punkt |
| Inkludierte Teilnehmer | Integer | Anzahl Teilnehmer im Fixpreis enthalten |
| Preis pro Zusatzteilnehmer | Decimal | |
| Max. Teilnehmer | Integer | `NumberMaxParticipants` |
| Kommentar | Textarea | Hinweis/Anmerkung für den Punkt |

**Aktionen:**
- **Speichern (ohne E-Mail):** POST `/umbraco/api/EventAdministrationApi/CreateOrUpdateAgendaSpecialItems?sendMail=false`
  - Erfolgsmeldung: "Daten aktualisiert – Die Angaben zur Einladung des Unternehmens wurden erfolgreich aktualisiert."
- **Speichern und Einladungsmail versenden:** POST `/umbraco/api/EventAdministrationApi/CreateOrUpdateAgendaSpecialItems?sendMail=true`
  - Sendet E-Mail an die Firma mit den Einladungsdetails
  - Erfolgsmeldung: "Einladung erstellt – Die Einladung des Unternehmens wurde erfolgreich angelegt und versendet."
  - Weiterleitung zur Firmenliste

**Fehlerfall:**
- Fehlermeldung: "Die Einladung konnte nicht erfolgreich versendet werden."

---

### US-09 – Firmeneinladung löschen

**Als** Admin
**möchte ich** eine Firmeneinladung vollständig entfernen,
**damit** eine versehentlich angelegte Einladung rückgängig gemacht werden kann.

#### Akzeptanzkriterien

- DELETE `/umbraco/api/EventAdministrationApi/DeleteCompanyTerms?eventId={id}&companyId={id}`
- Löscht: alle `EventAgendaItemsSpecial` der Firma, alle `EventCompanyParticipant`-Einträge, den `EventCompany`-Datensatz
- Nicht rückgängig machbar

---

### US-10 – Einladungsstatus und Buchungsstatus einer Firma einsehen

**Als** Admin
**möchte ich** den aktuellen Status einer Firmeneinladung einsehen,
**damit** ich nachverfolgen kann, ob die Firma gebucht, storniert oder noch nicht reagiert hat.

#### Akzeptanzkriterien

- GET `/umbraco/api/EventAdministrationApi/GetEventCompanyDetails?eventId={id}&companyId={id}`
- GET `/umbraco/api/EventAdministrationApi/GetEventCompanyDetailsByEventId?eventId={id}` – alle Firmen einer Veranstaltung
- Angezeigte Statusfelder:

| Feld | Bedeutung |
|---|---|
| `SentOn` | Datum, wann die Einladungsmail zuletzt versendet wurde |
| `IsBooked` / `BookedOn` | Firma hat gebucht (vom Unternehmensvertreter gesetzt) |
| `IsCanceled` / `CanceledOn` | Firma hat die Buchung storniert |
| `IsParticipationCanceled` / `ParticipationCanceledOn` / `ParticipationCancelComment` | Firma hat Nicht-Teilnahme gemeldet |

---

## 6. Teilnehmerverwaltung und Export (Admin)

---

### US-11 – Teilnehmerliste einer Firma einsehen

**Als** Admin
**möchte ich** sehen, welche Personen einer eingeladenen Firma an der Veranstaltung teilnehmen,
**damit** ich die Planung und Abrechnung durchführen kann.

#### Akzeptanzkriterien

- GET `/umbraco/api/EventAdministrationApi/GetParticipants?eventId={id}&companyId={id}`
- Angezeigt werden:
  - Firmeninfo
  - Veranstaltungsinfo
  - `EventCompany`-Konditionen
  - `EventAgendaItemsSpecial` mit Preisangaben
  - Liste der Teilnehmer (`EventCompanyParticipant`): Vorname, Nachname, E-Mail, Telefon, GutBeraten-ID, Anmeldezeitpunkt
  - Gebuchte Zusatzoptionen

---

### US-12 – Veranstaltungsdaten exportieren (Admin)

**Als** Admin
**möchte ich** Teilnehmer- und Veranstaltungsdaten als Excel- oder CSV-Datei herunterladen,
**damit** ich diese für Abrechnung, Kommunikation und Planung nutzen kann.

#### Akzeptanzkriterien

Vier Export-Endpunkte, alle liefern XLSX außer dem CSV-Export:

| Export | Endpunkt | Dateiname | Format |
|---|---|---|---|
| Teilnehmerliste | `GET /ExportParticipators?eventId={id}` | `Teilnehmerliste {Titel}.xlsx` | XLSX |
| Teilnehmer-Kontaktdaten | `GET /ExportParticipatorsPersonalInfo?eventId={id}` | `Teilnehmer-Kontaktinformationen {Titel}.xlsx` | XLSX |
| Nicht-teilnehmende Mitglieder | `GET /ExportNotParticipatingMembers?eventId={id}` | `Liste der nicht teilnehmenden Mitglieder für {Titel}.xlsx` | XLSX |
| Teilnehmende Firmen | `GET /ExportEvent?eventId={id}` | `Liste der teilnehmenden Firmen für {Titel}.xlsx` | XLSX |
| Ausgewählte Firmen | `GET /ExportSelectedEventCompanies?eventId={id}&listOfCompanyIds=…` | `{JJJJ-MM-TT}-{Titel}.csv` | CSV |

- Content-Type für XLSX: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- Content-Type für CSV: `text/csv`
- Datei wird als Attachment geliefert (`Content-Disposition: attachment`)

---

## 7. Veranstaltungsübersicht (Makler)

---

### US-13 – Veranstaltungsübersicht aufrufen

**Als** Makler (eingeloggtes Portalmitglied)
**möchte ich** eine Liste aller für mich sichtbaren, veröffentlichten Veranstaltungen sehen,
**damit** ich mich informieren und anmelden kann.

#### Akzeptanzkriterien

- Zugriff nur für authentifizierte Mitglieder (`[MemberAuthorize]`)
- GET-Aktion `Index` mit optionalen Filterparametern
- **Standardmäßig:** Zeigt ab heute aufwärts, sortiert nach Datum aufsteigend, seitenweise (10 Einträge)
- **Nur veröffentlichte Events** (`PublicationState = Public`) werden angezeigt
- **Sichtbarkeitsfilter:** Nur Events, für die das Mitglied die passende Mitgliedergruppe hat (oder keine Gruppe gesetzt ist)

**Suchformular:**

| Feld | Typ | Pflicht | Hinweise |
|---|---|---|---|
| Suchbegriff | Text | nein | Suche nach Veranstaltungsname oder -ort; Placeholder: "Suchen nach Veranstaltungsname oder -ort" |
| Suche ab | Datum | nein | Format: dd.mm.yyyy; Standard: heute |
| Suche bis | Datum | nein | Format: dd.mm.yyyy |
| Sortierung | Dropdown | nein | `Event.OrderedBy`: Datum aufsteigend, Datum absteigend, Anmeldestatus; Änderung sendet Formular automatisch ab |

**Ergebnisliste:**
- Für jeden Treffer wird der Anmeldestatus des Maklers angezeigt (Ampel-Logik):

| Status | Anzeige |
|---|---|
| `RegistrationAvailable` | "Plätze frei" |
| `AlreadyRegistered` | "Angemeldet" |
| `FullyBooked` | "Ausgebucht" |
| `DeadlineMissed` | "Verpasst" |

- Falls keine Veranstaltungen gefunden: Meldung "Es wurden keine Veranstaltungen gefunden."
- Optional: Konfigurationspunkt `eventsPageHeaderLink` zeigt einen Link in der Kopfzeile

---

## 8. Veranstaltungsdetail & Anmeldung (Makler)

---

### US-14 – Veranstaltungsdetail aufrufen

**Als** Makler
**möchte ich** alle Details einer Veranstaltung einsehen,
**damit** ich entscheiden kann, ob ich mich anmelden möchte.

#### Akzeptanzkriterien

- GET-Aktion `Details(Event @event)`
- Zugriffsprüfung: `HasMemberAccessToEvent(@event, currentMemberId)` – bei fehlender Berechtigung wird auf eine "Kein Zugriff"-Seite weitergeleitet
- Angezeigt werden:
  - Titel, Beschreibung
  - Zeitraum (Start, Ende)
  - Ort (Veranstaltungsortname, Straße, PLZ, Stadt)
  - Anmeldefrist
  - Kontaktperson (Name, E-Mail)
  - Weiterbildungsstunden
  - Dokumente (downloadbar)
  - Anmeldestatus des Maklers

**Aktionsbuttons (nur sichtbar wenn Anmeldung möglich):**
- **"Jetzt anmelden"** → `RegisterSelfForm`: Nur aktiv wenn Makler noch nicht angemeldet und Deadline/Kapazität nicht überschritten
- **"Eine weitere Person anmelden"** → `RegisterGuestForm`: Nur aktiv wenn Begleitpersonenlimit noch nicht erreicht und Deadline/Kapazität nicht überschritten
- **"Termin übernehmen"** → `ExportEvent`: Immer sichtbar wenn Event vorhanden (iCal-Download)

**Status-Meldungen:**
- `FullyBooked`: "Die Veranstaltung ist ausgebucht. Es sind keine freien Plätze mehr verfügbar."
- `DeadlineMissed`: "Die Anmeldefrist für diese Veranstaltung ist am [Datum] abgelaufen."
- Sonst: "Die Anmeldefrist endet am: [Datum]"

---

### US-15 – Als Makler für Veranstaltung anmelden (Selbstanmeldung)

**Als** Makler
**möchte ich** mich selbst für eine Veranstaltung anmelden,
**damit** meine Teilnahme registriert ist.

#### Akzeptanzkriterien

**Anmeldeformular (`MemberEventRegistrationPage`):**
- Begrüßung: "Hallo [Anrede] [Nachname]"
- Hinweis: "bitte wählen Sie die Programmpunkte aus, an denen Sie teilnehmen wollen"
- **Agendapunkte** (Mehrfachauswahl, Checkboxen):
  - Ein Checkbox pro Agendapunkt
  - Anzeige der Teilnahmekosten: "Teilnahmekosten: {Preis}"
  - Deaktiviert wenn `MembersParticipationDisabled = true`
- **Anmeldehinweis** aus `Event.RegistrationHint` (wenn vorhanden)
- **AGB-Checkbox** (Pflicht): "Hiermit akzeptiere ich die Anmeldebedingungen"
- Buttons: "Verbindlich anmelden" (Submit) und "Abbrechen" (zurück zu Details)

**POST-Validierung (serverseitig):**

| Prüfung | Fehlertext |
|---|---|
| Event muss `PublicationState = Public` haben | "Die Veranstaltung ist nicht veröffentlicht." |
| `RegistrationDeadline + 1 Tag >= jetzt` | "Die Registrierungs-Deadline der Veranstaltung wurde bereits überschritten." |
| `RegistrationCount < ParticipantsLimit` | "Die maximale Teilnehmerzahl der Veranstaltung wurde überschritten." |
| Makler noch nicht für Event angemeldet | "Der Makler ist bereits für die Veranstaltung registriert." |
| Mind. ein Agendapunkt ausgewählt | "Die Liste der Programmpunkte ist leer." |
| Alle Agendapunkte gehören zum Event | "Der Programmpunkt ist Teil einer anderen Veranstaltung." |
| Kein ausgewählter Agendapunkt hat `MembersParticipationDisabled = true` | "Die Teilnahme für Makler ist für den Programmpunkt deaktiviert." |

- Bei Erfolg: Weiterleitung auf Bestätigungsseite (`MemberRegistrationConfirmation`)
- `RegistrationCount` der Veranstaltung wird um 1 erhöht

---

### US-16 – iCalendar-Export einer Veranstaltung

**Als** Makler
**möchte ich** einen Veranstaltungstermin als `.ics`-Datei herunterladen,
**damit** ich ihn in meinen Kalender importieren kann.

#### Akzeptanzkriterien

- GET-Aktion `ExportEvent(Event @event)`
- Erzeugt eine `.ics`-Datei mit:
  - `SUMMARY`: Veranstaltungstitel
  - `DTSTART` / `DTEND`: Start- und Endzeitpunkt in UTC
  - `LOCATION`: "{Straße} {Hausnummer}, {PLZ} {Stadt}"
- Dateiname: `{Titel}.ics`
- Zugriffsprüfung: Makler muss Zugriff auf das Event haben

---

## 9. Gastanmeldung (Makler)

---

### US-17 – Begleitperson (Gast) anmelden

**Als** Makler
**möchte ich** eine Begleitperson für eine Veranstaltung anmelden,
**damit** ich Kollegen, Familienmitglieder oder Geschäftspartner mitnehmen kann.

#### Akzeptanzkriterien

**Anmeldeformular (`GuestEventRegistrationPage`):**

| Feld | Typ | Pflicht | Hinweise |
|---|---|---|---|
| Beziehungstyp | Enum Dropdown | ja | `MemberGuestRelationTypes`; "Bitte auswählen:" als Standardoption |
| Anrede | Enum Dropdown | ja | Herr / Frau |
| Vorname | Text | ja | – |
| Nachname | Text | ja | – |
| Straße | Text | ja | – |
| PLZ | Text | ja | – |
| Stadt | Text | ja | – |
| Agendapunkte | Checkboxen | nein | Deaktiviert wenn `CompanionParticipationDisabled = true` |
| AGB-Checkbox | Boolean | ja | "Hiermit akzeptiere ich die Anmeldebedingungen" |

**POST-Validierung (serverseitig):**

| Prüfung | Fehlertext |
|---|---|
| Event `PublicationState = Public` | "Die Veranstaltung ist nicht veröffentlicht." |
| Deadline nicht überschritten | "Die Registrierungs-Deadline der Veranstaltung wurde bereits überschritten." |
| Teilnehmerlimit nicht überschritten | "Die maximale Teilnehmerzahl der Veranstaltung wurde überschritten." |
| Begleitpersonenlimit nicht überschritten | "Der Makler hat bereits die maximale Anzahl an Gästen für die Veranstaltung registriert." |
| Mind. ein Agendapunkt gewählt | "Die Liste der Programmpunkte ist leer." |
| Alle Agendapunkte gehören zum Event | "Der Programmpunkt ist Teil einer anderen Veranstaltung." |
| Kein Agendapunkt hat `CompanionParticipationDisabled = true` | "Die Teilnahme von Gästen ist für den Programmpunkt deaktiviert." |

**Begleitpersonenlimit-Logik:**
- Prüfung nur wenn `Event.CompanionshipLimit` gesetzt ist
- `GuestRegistrationCount >= CompanionshipLimit` → Fehler

- Bei Erfolg: Weiterleitung auf `GuestRegistrationConfirmation`
- `RegistrationCount` der Veranstaltung wird erhöht
- Der anmeldende Makler ist `ResponsibleMemberId` der Gastanmeldung

---

## 10. Anmeldung stornieren (Makler)

---

### US-18 – Eigene Anmeldung stornieren

**Als** Makler
**möchte ich** meine eigene Anmeldung zurückziehen,
**damit** mein Platz wieder freigegeben wird.

#### Akzeptanzkriterien

**Seite `EventRegistrationDetailsPage`:**
- Zeigt alle eigenen Anmeldungen (Selbst + Gäste) mit Agendapunkten und Kosten
- Storno-Button "Teilnahme zurückziehen" ist nur sichtbar wenn `DateTime.Today <= Event.RegistrationDeadline`
- Zusammenfassung der Anmeldung:
  - "Sie sind für diese Veranstaltung angemeldet." (nur Selbst)
  - "Sie haben sich und einen weiteren Gast angemeldet." (Selbst + 1 Gast)
  - "Sie haben sich und {n} weitere Gäste angemeldet." (Selbst + n Gäste)
  - "Sie haben einen Gast angemeldet." (nur 1 Gast)
  - "Sie haben {n} Gäste angemeldet." (nur Gäste)

**Stornoformular (`EventRegistrationCancellationPage`):**
- Zeigt gebuchte Agendapunkte mit Einzelkosten und Gesamtkosten
- Feld: "Grund für das Zurückziehen der Anmeldung" (optional)
- Button "Anmeldung zurückziehen" (Danger-Styling)

**Validierung:**
- Nur der Makler, der die Anmeldung erstellt hat, darf stornieren (`MemberId == currentMemberId`)
  - Fehlertext: "Eine Veranstaltungsteilnahme kann nur von dem Member abgesagt werden, der sie auch erstellt hat."
- Deadline darf noch nicht überschritten sein

- Bei Erfolg: Weiterleitung auf `RegistrationDetails`, `RegistrationCount` wird verringert

---

### US-19 – Gastanmeldung stornieren

**Als** Makler
**möchte ich** die Anmeldung eines von mir registrierten Gastes zurückziehen.

#### Akzeptanzkriterien

- Identisch mit US-18, jedoch für `GuestEventRegistration`
- Nur der Makler, der die Gastanmeldung erstellt hat (`ResponsibleMemberId == currentMemberId`), darf stornieren
- Formular-Header: "Anmeldung von [Anrede] [Nachname]"
- Bei Erfolg: `RegistrationCount` wird verringert

---

## 11. Firmenbuchung (Unternehmensvertreter)

---

### US-20 – Firmenbuchungsseite aufrufen (anonymer Zugriff)

**Als** Unternehmensvertreter (externer Nutzer ohne Login)
**möchte ich** über einen einzigartigen Link meine Firmenteilnahme verwalten,
**damit** ich Teilnehmer eintragen und Buchungsoptionen wählen kann.

#### Akzeptanzkriterien

- Zugriff über `[AllowAnonymous]` – kein Login erforderlich
- GET-Aktion `RegistrationDetailsCompany(Guid guid)` – Identifikation der Firma über `EventCompany.BusinessId` (GUID)
- Angezeigt werden:
  - Firmenlogo in der Kopfzeile
  - Veranstaltungstitel und optionales Teaser-Bild
  - Veranstaltungsdetails (Ort, Zeit, Kontakt)
  - Wenn `EventCompany.IsCanceled = true`: Warnhinweis "Die Buchung wurde storniert"
  - Agendapunkte mit firmenspezifischen Preisen (Fixpreis, inkludierte Teilnehmer, Zusatzpreis)
  - Automatische Kostenberechnung per JavaScript
  - Bestehende Teilnehmer vorausgefüllt (bei Wiederaufruf)
  - Buchbare Zusatzoptionen
  - Felder für Bemerkung und alternative Rechnungsanschrift
  - Stornohinweis aus `Event.StornoCompaniesHint` (wenn vorhanden)

---

### US-21 – Teilnehmer eintragen und Firmenbuchung absenden

**Als** Unternehmensvertreter
**möchte ich** die Teilnehmer meiner Firma eintragen und die Buchung abschicken.

#### Akzeptanzkriterien

**Formularfelder pro Teilnehmer (dynamisch, beliebig viele):**

| Feld | Typ | Pflicht | Hinweise |
|---|---|---|---|
| Vorname | Text | ja | – |
| Nachname | Text | ja | – |
| E-Mail | Text | ja | – |
| Telefon | Text | ja | – |
| Gut-beraten-ID | Text | nein | Pattern: `\d{8}-\w{6}-\w{2}` (JJJJMMTT-XXXXXX-XX); nur für Makler |
| Agendapunkt-Teilnahme | Checkboxen | nein | Ein Checkbox je Agendapunkt pro Teilnehmer |

**Zusätzliche Felder:**

| Feld | Typ | Pflicht |
|---|---|---|
| Bemerkung | Textarea | nein |
| Alternative Rechnungsanschrift | Textarea | nein |
| Zusatzoptionen | Checkboxen | nein |

**Funktionen:**
- Teilnehmer können dynamisch hinzugefügt ("+Teilnehmer:in hinzufügen") und entfernt (Lösch-Icon) werden
- Kostenberechnung wird bei jeder Änderung automatisch aktualisiert: `Fixpreis + (Anzahl Zusatzteilnehmer × Preis)`
- Meldung beim Absenden: *"Die Daten werden gespeichert und anschließend per Email bestätigt. Bitte haben Sie einen Moment Geduld."*

**POST `EventRegisterCompanyParticipants` – serverseitige Verarbeitung:**
1. Aktualisiert `EventAgendaItemsSpecial.NumberOfParticipants` pro Agendapunkt
2. Löscht alle bestehenden `EventCompanyParticipant`-Einträge der Firma
3. Legt neue `EventCompanyParticipant`-Einträge mit Agendapunkt-Zuordnungen an
4. Aktualisiert Zusatzoptionen (`EventExtraOptionsEventCompany`): bestehende löschen, neu zuordnen
5. Setzt `EventCompany.IsBooked = true`, `BookedOn = jetzt`
6. Setzt `EventCompany.IsCanceled = false`, `CanceledOn = null` (Storno rückgängig)
7. Speichert `CompanyComment` und `AlternateBillAddress`
8. Sendet Bestätigungs-E-Mail an Administrator (`SendCompanyParticipantNotification`)
   - E-Mail-Fehler werden geloggt, brechen den Prozess aber nicht ab
9. Weiterleitung auf `RegistrationDetailsCompany` (gleiche Seite mit GUID)

---

### US-22 – Firmenbuchung stornieren

**Als** Unternehmensvertreter
**möchte ich** eine bestehende Buchung stornieren,
**damit** meine Firma nicht an der Veranstaltung teilnimmt.

#### Akzeptanzkriterien

- GET-Aktion `CancelEventForCompany(Guid guid)` – `[AllowAnonymous]`
- Verarbeitung:
  1. Setzt `EventCompany.IsCanceled = true`, `CanceledOn = jetzt`
  2. Setzt `EventCompany.IsBooked = false`, `BookedOn = null`
  3. Sendet Stornobenachrichtigung an Administrator (`SendCompanyParticipantCancelationNotification`)
     - E-Mail-Fehler werden geloggt, brechen den Prozess nicht ab
- Weiterleitung auf `RegistrationDetailsCompany`
- Storno-Button ist nur sichtbar wenn `EventCompany.IsCanceled = false`

---

### US-23 – Nicht-Teilnahme melden

**Als** Unternehmensvertreter
**möchte ich** erklären, dass meine Firma nicht an der Veranstaltung teilnimmt (ohne zu buchen),
**damit** der Administrator informiert ist.

#### Akzeptanzkriterien

- Formularfeld: "Ihr Grund für die Nicht-Teilnahme" (Textarea, optional)
- POST `EventCompanyNoParticipation(Guid eventId, EventCompanyParticipationCommand command)` – `[AllowAnonymous]`
- Verarbeitung:
  1. Setzt `EventCompany.IsParticipationCanceled = true`, `ParticipationCanceledOn = jetzt`
  2. Speichert `ParticipationCancelComment`
  3. Sendet Benachrichtigung an Administrator (`SendCompanyNoParticipationNotification`)
- Weiterleitung auf `RegistrationDetailsCompany`
- Der "Keine Teilnahme"-Bereich ist auf der Seite standardmäßig ausgeblendet und wird per Button-Klick eingeblendet

---

## E-Mail-Benachrichtigungen (Übersicht)

| Auslöser | Template | Empfänger |
|---|---|---|
| Makler meldet sich an | `MemberEventRegistration.template` | Makler (Bestätigung) |
| Makler meldet Gast an | `GuestEventRegistration.template` | Makler (Bestätigung) |
| Admin lädt Firma ein (mit E-Mail) | `CompanyEventRegistration.template` | Unternehmensvertreter |
| Firma bucht Teilnehmer ein | `CompanyEventRegistrationConfirmation.template` | Administrator |
| Firma storniert Buchung | `EventCompanyParticipantNotification.template` | Administrator |
| Firma meldet Nicht-Teilnahme | `EventRegistrationCancellation.template` | Administrator |

---

## Geschäftsregeln (Zusammenfassung)

| Regel | Details |
|---|---|
| **Anmeldefrist** | Frist gilt bis einschließlich des Frist-Tags (`deadline + 1 Tag >= jetzt`) |
| **Einmalige Anmeldung** | Ein Makler kann sich pro Veranstaltung nur einmal selbst anmelden |
| **Begleitpersonenlimit** | Gilt pro Makler; nur aktiv wenn `CompanionshipLimit` gesetzt ist |
| **Agendapunkt-Deaktivierung** | `MembersParticipationDisabled` und `CompanionParticipationDisabled` können unabhängig gesetzt werden |
| **Storno-Berechtigung** | Nur der Ersteller einer Anmeldung darf stornieren |
| **Firmenportal-Zugang** | Anonymer Zugang über eindeutigen GUID-Link (`EventCompany.BusinessId`) |
| **Buchung überschreiben** | Firmen können ihre Buchung jederzeit (erneut) absenden – bestehende Teilnehmerliste wird ersetzt |
| **Veröffentlichung** | Anmeldungen sind nur bei `PublicationState = Public` möglich |
