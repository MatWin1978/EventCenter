# Domainmodell — EventCenter

Stand: 2026-03-03 (aktualisiert 2026-03-03 — Hinweis-Felder, WeiterbildungsstundenWebinar)

## Entity-Relationship-Diagramm

```mermaid
erDiagram

    Event {
        int         Id                          PK
        string      Title
        string      Description
        string      Location
        datetime    StartDateUtc
        datetime    EndDateUtc
        datetime    RegistrationDeadlineUtc
        int         MaxCapacity
        int         MaxCompanions
        bool        IsPublished
        string      ContactName
        string      ContactEmail
        string      ContactPhone
        string      EventType                   "InPerson | Webinar"
        string      ExternalRegistrationUrl     "nullable – nur Webinar"
        decimal     WeiterbildungsstundenWebinar "nullable – nur Webinar, decimal(5,2)"
        string      Anmeldehinweis              "nullable – Freitext für Makler & Firmen"
        string      StornohinweisMakler         "nullable – Freitext nur für Makler"
        string      StornohinweisUnternehmen    "nullable – Freitext nur für Firmen"
        string[]    DocumentPaths               "JSON-serialisiert"
        byte[]      RowVersion                  "Optimistic Concurrency"
    }

    EventAgendaItem {
        int         Id                  PK
        int         EventId             FK
        string      Title
        string      Description
        datetime    StartDateTimeUtc
        datetime    EndDateTimeUtc
        decimal     CostForMakler
        decimal     CostForGuest
        bool        IsMandatory
        int         MaxParticipants
        bool        MaklerCanParticipate
        bool        GuestsCanParticipate
    }

    EventOption {
        int         Id          PK
        int         EventId     FK
        string      Name
        string      Description
        decimal     Price
        int         MaxQuantity
    }

    Company {
        int         Id              PK
        string      Name
        string      ContactEmail
        string      ContactPhone
        string      ContactFirstName
        string      ContactLastName
        string      Street
        string      PostalCode
        string      City
        string      Notes
    }

    EventCompany {
        int         Id                  PK
        int         EventId             FK
        int         CompanyId           FK  "nullable – Adressbuch-Link"
        string      CompanyName
        string      ContactEmail
        string      ContactPhone
        decimal     PricePerPerson
        int         MaxParticipants
        string      InvitationCode      "UNIQUE, nullable"
        datetime    InvitationSentUtc
        string      Status              "Draft | Sent | Booked | Cancelled"
        decimal     PercentageDiscount
        string      PersonalMessage
        datetime    ExpiresAtUtc
        datetime    BookingDateUtc
        string      CancellationComment
        bool        IsNonParticipation
    }

    EventCompanyAgendaItemPrice {
        int         EventCompanyId  PK,FK
        int         AgendaItemId    PK,FK
        decimal     CustomPrice     "null = Standardpreis"
    }

    Registration {
        int         Id                      PK
        int         EventId                 FK
        int         EventCompanyId          FK  "nullable – nur Firmenbuchungen"
        int         ParentRegistrationId    FK  "nullable – Begleitperson → Makler"
        string      RegistrationType        "Makler | Guest | CompanyParticipant"
        string      Salutation              "Herr | Frau | Divers"
        string      FirstName
        string      LastName
        string      Email
        string      Phone
        string      Company
        string      RelationshipType        "Beziehungstyp bei Begleitpersonen"
        string      SpecialRequirements
        datetime    RegistrationDateUtc
        bool        IsConfirmed
        int         NumberOfCompanions
        bool        IsCancelled
        datetime    CancellationDateUtc
        string      CancellationReason
    }

    RegistrationAgendaItem {
        int         RegistrationId  PK,FK
        int         AgendaItemId    PK,FK
    }

    RegistrationEventOption {
        int         RegistrationId  PK,FK
        int         EventOptionId   PK,FK
    }

    %% ── Beziehungen ──────────────────────────────────────────────

    Event              ||--o{ EventAgendaItem              : "hat Agendapunkte"
    Event              ||--o{ EventOption                  : "hat Zusatzoptionen"
    Event              ||--o{ EventCompany                 : "hat Firmeneinladungen"
    Event              ||--o{ Registration                 : "hat Anmeldungen"

    Company            ||--o{ EventCompany                 : "ist verlinkt mit"

    EventCompany       ||--o{ Registration                 : "hat Teilnehmer"
    EventCompany       ||--o{ EventCompanyAgendaItemPrice  : "hat individuelle Preise"

    EventAgendaItem    ||--o{ EventCompanyAgendaItemPrice  : "hat Firmenpreis"
    EventAgendaItem    ||--o{ RegistrationAgendaItem       : "gewählt von"

    Registration       ||--o{ RegistrationAgendaItem       : "wählt Agendapunkte"
    Registration       ||--o{ RegistrationEventOption      : "wählt Optionen"
    Registration       ||--o{ Registration                 : "hat Begleitpersonen"

    EventOption        ||--o{ RegistrationEventOption      : "gewählt in"
```

---

## Enumerationen

| Enum | Werte |
|---|---|
| `EventType` | `InPerson`, `Webinar` |
| `EventState` | `NotPublished`, `Public`, `DeadlineReached`, `Finished` _(berechnet, nicht persistiert)_ |
| `RegistrationType` | `Makler`, `Guest`, `CompanyParticipant` |
| `InvitationStatus` | `Draft`, `Sent`, `Booked`, `Cancelled` |

---

## Cascade-Delete-Verhalten

| Beziehung | Verhalten |
|---|---|
| Event → EventAgendaItem | Cascade |
| Event → EventCompany | Cascade |
| Event → Registration | Cascade |
| Event → EventOption | Cascade |
| EventCompany → EventCompanyAgendaItemPrice | Cascade |
| EventCompany → Registration | Restrict _(Buchungen schützen)_ |
| EventAgendaItem → EventCompanyAgendaItemPrice | NoAction _(kein Cascade-Zyklus)_ |
| EventAgendaItem → RegistrationAgendaItem | NoAction _(kein Cascade-Zyklus)_ |
| Registration → RegistrationAgendaItem | Cascade |
| Registration → Registration (Begleitperson) | Restrict _(Begleitpersonen schützen)_ |

---

## Schlüsselbeziehungen in Prosa

### Veranstaltung (`Event`)
Zentrale Entität. Hat Agendapunkte, Zusatzoptionen, Firmeneinladungen und direkte Makler-Anmeldungen. `EventType` unterscheidet Präsenzveranstaltungen von Webinaren. Webinar-spezifische Felder (`ExternalRegistrationUrl`, `WeiterbildungsstundenWebinar`) sind nullable und werden nur bei `EventType = Webinar` befüllt. Die drei Hinweisfelder (`Anmeldehinweis`, `StornohinweisMakler`, `StornohinweisUnternehmen`) sind für beide Veranstaltungstypen optional und werden rollenspezifisch angezeigt.

### Firmeneinladung (`EventCompany`)
Verbindet eine Veranstaltung mit einem Unternehmen. Enthält den gesamten Einladungslebenszyklus (`Draft → Sent → Booked / Cancelled`). Der nullable `CompanyId`-FK verknüpft optional mit dem Firmenstammdaten-Adressbuch (`Company`). Individuelle Preise pro Agendapunkt werden in `EventCompanyAgendaItemPrice` gespeichert.

### Anmeldung (`Registration`)
Wird sowohl für Makler-Direktanmeldungen (`EventCompanyId = null`) als auch für Firmenbuchungsteilnehmer (`EventCompanyId = X`) verwendet. Begleitpersonen (`RegistrationType = Guest`) referenzieren über `ParentRegistrationId` die Hauptanmeldung des Maklers. Die M:N-Beziehungen zu Agendapunkten und Optionen werden über `RegistrationAgendaItem` und `RegistrationEventOption` (Junction-Tabellen) abgebildet.

### Firmenstammdaten (`Company`)
Adressbuch-Entität. Wird beim Erstellen einer Firmeneinladung optional verknüpft. Die Verknüpfung ist nullable für Rückwärtskompatibilität mit manuell eingetragenen Firmen.

---

## Externe API-DTOs (nicht persistiert)

### `GuestooEventDto` / `GuestooEventLocation`
Read-only-Records für die Antwort der Guestoo-Event-API. Werden in `GuestooEventApiService` deserialisiert und **nicht** in der Datenbank gespeichert.

| Typ | Property | Typ | Beschreibung |
|---|---|---|---|
| `GuestooEventDto` | `Title` | `string?` | Veranstaltungstitel |
| | `Subtitle` | `string?` | Untertitel |
| | `Location` | `GuestooEventLocation?` | Verschachtelte Ortsangabe |
| | `StartDate` | `DateTimeOffset?` | Startzeit mit Zeitzone |
| | `EndDate` | `DateTimeOffset?` | Endzeit mit Zeitzone |
| | `AvailableSeats` | `long?` | Verfügbare Plätze |
| | `ImageUrl` | `string?` | Bild-URL |
| | `ShortDescription` | `string?` | Kurzbeschreibung |
| | `EventLink` | `string?` | Direktlink zur Veranstaltung |
| `GuestooEventLocation` | `Street` | `string?` | Straße |
| | `StreetNumber` | `string?` | Hausnummer |
| | `PostCode` | `string?` | PLZ |
| | `City` | `string?` | Stadt |
| | `Country` | `string?` | Land |

---

## Formularmodelle (nicht persistiert)

### `CompanyBookingFormModel` / `ParticipantModel`
View-Models für das Firmenbuchungsformular (`CompanyBooking.razor`).

| Typ | Property | Typ | Beschreibung |
|---|---|---|---|
| `CompanyBookingFormModel` | `EventCompanyId` | `int` | Referenz auf die Firmeneinladung |
| | `Participants` | `List<ParticipantModel>` | Teilnehmerliste (mind. 1 Eintrag) |
| | `SelectedExtraOptionIds` | `List<int>` | Ausgewählte Zusatzoptionen |
| `ParticipantModel` | `Salutation` | `string` | Anrede |
| | `FirstName` | `string` | Vorname |
| | `LastName` | `string` | Nachname |
| | `Email` | `string` | E-Mail |
| | `SelectedAgendaItemIds` | `List<int>` | Ausgewählte Agendapunkte |
| | `AgendaRequired` | `bool` | Berechnetes Flag: `true` wenn die Veranstaltung wählbare Agendapunkte hat → Pflichtauswahl im Formular |
