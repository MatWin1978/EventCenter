# Veranstaltungscenter – Domainmodell

## Klassendiagramm

```mermaid
classDiagram
    direction TB

    class IEvent {
        <<interface>>
        +Guid BusinessId
        +string Title
        +DateTime Start
        +DateTime End
        +DateTime RegistrationDeadline
        +double? ContinuingEducationHours
        +PublicationStates PublicationState
        +EventState State
        +EventType Type
        +string Location
        +string AvailableSeats
        +IList~IMemberGroup~ MemberGroups
        +Publish()
        +Unpublish()
    }

    class Event {
        <<Entity – tblEvents>>
        +string Title
        +string Description
        +string ContactName
        +string ContactMail
        +DateTime Start
        +DateTime End
        +string LocationName
        +string Street
        +string StreetNumber
        +string PostalCode
        +string City
        +int ParticipantsLimit
        +int? CompanionshipLimit
        +DateTime RegistrationDeadline
        +string RegistrationHint
        +string StornoHint
        +string StornoCompaniesHint
        +int RegistrationCount
        +PublicationStates PublicationState
        +EventState State
        +double? ContinuingEducationHours
        +Publish()
        +Unpublish()
        +AddMemberGroups()
        +RemoveMemberGroups()
    }

    class WebinarEvent {
        <<Entity – tblWebinarEvents>>
        +string Title
        +string Description
        +DateTime Start
        +DateTime End
        +DateTime RegistrationDeadline
        +string RegistrationLink
        +PublicationStates PublicationState
        +EventState State
        +double? ContinuingEducationHours
        +Publish()
        +Unpublish()
        +AddMemberGroups()
        +RemoveMemberGroups()
    }

    class EventAgendaItem {
        <<Entity – tblEventAgendaItems>>
        +string Name
        +int EventAgendaItemNumber
        +decimal MembersParticipationCost
        +bool MembersParticipationDisabled
        +decimal CompanionParticipationCost
        +bool CompanionParticipationDisabled
    }

    class EventAgendaItemsSpecial {
        <<Entity – tblEventAgendaItemsSpecial>>
        +decimal FixedPrice
        +int NumberParticipantsIncluded
        +decimal PriceAdditionalParticipants
        +int NumberOfParticipants
        +int NumberMaxParticipants
        +bool IsVisible
        +string Comment
    }

    class EventExtraOption {
        <<Entity – tblEventExtraOptions>>
        +string Name
        +decimal Price
    }

    class EventCompany {
        <<Entity – tblEventCompanies>>
        +decimal PriceFix
        +decimal PricePerParticipant
        +string Comment
        +bool IsBooked
        +DateTime? BookedOn
        +bool IsCanceled
        +DateTime? CanceledOn
        +bool IsParticipationCanceled
        +DateTime? ParticipationCanceledOn
        +string ParticipationCancelComment
        +string AlternateBillAddress
        +string CompanyComment
        +DateTime? SentOn
    }

    class EventCompanyParticipant {
        <<Entity – tblEventCompanyParticipants>>
        +string Firstname
        +string Lastname
        +string Email
        +string Phone
        +string GutBeratenId
        +DateTime RegisteredAt
    }

    class EventCompanyParticipantsAgendaItemSpecial {
        <<Entity – tblEventCompanyParticipantsAgendaItemSpecial>>
        %% Verknüpfungstabelle: Teilnehmer ↔ Sonder-Agendapunkt
    }

    class EventExtraOptionsEventCompany {
        <<Entity – tblEventExtraOptionsEventCompanies>>
        %% Verknüpfungstabelle: Firma ↔ Zusatzoption
    }

    class EventRegistration {
        <<abstract Entity>>
        +DateTime RegisteredAt
        +IList~EventAgendaItem~ ParticipatingIn
    }

    class MemberEventRegistration {
        <<Entity – tblEventMemberRegistrations>>
        +int MemberId
    }

    class GuestEventRegistration {
        <<Entity – tblEventGuestRegistrations>>
        +GuestSalutations Salutation
        +string Firstname
        +string Lastname
        +string Street
        +string PostalCode
        +string City
        +MemberGuestRelationTypes MemberGuestRelationType
        +int ResponsibleMemberId
    }

    class PublicationStates {
        <<enumeration>>
        NotPublic = 0
        Public = 1
    }

    class EventState {
        <<enumeration>>
        NotDefined = 0
        Public = 1
        NotPublic = 2
        DeadlineReached = 3
        Finished = 4
    }

    class EventType {
        <<enumeration>>
        Unspecified = 0
        PhysicalAttendance = 1
        Webinar = 2
    }

    class GuestSalutations {
        <<enumeration>>
        Mr
        Mrs
    }

    class MemberGuestRelationTypes {
        <<enumeration>>
    }

    %% Vererbung
    IEvent <|.. Event : implements
    IEvent <|.. WebinarEvent : implements
    EventRegistration <|-- MemberEventRegistration : extends
    EventRegistration <|-- GuestEventRegistration : extends

    %% Aggregation: Event → Unterelemente
    Event "1" *-- "0..*" EventAgendaItem : hat Agendapunkte
    Event "1" *-- "0..*" EventExtraOption : hat Zusatzoptionen
    Event "1" *-- "0..*" EventCompany : hat eingeladene Firmen
    Event "1" *-- "0..*" EventRegistration : hat Anmeldungen

    %% Agendapunkt → Sonderkonditionen
    EventAgendaItem "1" *-- "0..*" EventAgendaItemsSpecial : hat Firmen-Sonderkonditionen

    %% Firma → Teilnehmer & Optionen
    EventCompany "1" *-- "0..*" EventCompanyParticipant : hat Teilnehmer
    EventCompany "1" *-- "0..*" EventExtraOptionsEventCompany : bucht Zusatzoptionen

    %% Verknüpfungstabellen
    EventExtraOption "1" --o "0..*" EventExtraOptionsEventCompany : referenziert
    EventCompanyParticipant "1" *-- "0..*" EventCompanyParticipantsAgendaItemSpecial : nimmt teil an
    EventAgendaItemsSpecial "1" --o "0..*" EventCompanyParticipantsAgendaItemSpecial : referenziert

    %% Enums
    Event ..> PublicationStates : verwendet
    Event ..> EventState : verwendet
    Event ..> EventType : verwendet
    WebinarEvent ..> PublicationStates : verwendet
    WebinarEvent ..> EventState : verwendet
    WebinarEvent ..> EventType : verwendet
    GuestEventRegistration ..> GuestSalutations : verwendet
    GuestEventRegistration ..> MemberGuestRelationTypes : verwendet
```

## Beschreibung der Entitäten

### Kern-Entitäten

| Entität | Tabelle | Beschreibung |
|---|---|---|
| `IEvent` | – | Interface für alle Veranstaltungstypen |
| `Event` | `tblEvents` | Physische Präsenzveranstaltung mit Ort, Teilnehmerlimits und Begleitpersonenlimit |
| `WebinarEvent` | `tblWebinarEvents` | Online-Webinar (z. B. GoToWebinar) mit externem Registrierungslink |

### Agenda & Optionen

| Entität | Tabelle | Beschreibung |
|---|---|---|
| `EventAgendaItem` | `tblEventAgendaItems` | Einzelner Programmpunkt einer Veranstaltung mit Teilnahmekosten |
| `EventAgendaItemsSpecial` | `tblEventAgendaItemsSpecial` | Firmenseitige Sonderkonditionen für einen Agendapunkt (Fixpreis, inkludierte Teilnehmer) |
| `EventExtraOption` | `tblEventExtraOptions` | Buchbare Zusatzoption zu einer Veranstaltung (z. B. Unterkunft) |

### Firmenanmeldung

| Entität | Tabelle | Beschreibung |
|---|---|---|
| `EventCompany` | `tblEventCompanies` | Einladung einer Firma zu einer Veranstaltung inkl. Preis und Status |
| `EventCompanyParticipant` | `tblEventCompanyParticipants` | Einzelner Teilnehmer einer eingeladenen Firma |
| `EventExtraOptionsEventCompany` | `tblEventExtraOptionsEventCompanies` | Verknüpfung: Firma bucht Zusatzoption |
| `EventCompanyParticipantsAgendaItemSpecial` | `tblEventCompanyParticipantsAgendaItemSpecial` | Verknüpfung: Teilnehmer nimmt an Sonder-Agendapunkt teil |

### Mitglieder- und Gastanmeldung

| Entität | Tabelle | Beschreibung |
|---|---|---|
| `EventRegistration` | – | Abstrakte Basisklasse für alle Anmeldungen |
| `MemberEventRegistration` | `tblEventMemberRegistrations` | Anmeldung eines Maklerportal-Mitglieds |
| `GuestEventRegistration` | `tblEventGuestRegistrations` | Anmeldung einer Begleitperson (durch ein Mitglied verwaltet) |

### Enumerationen

| Enum | Werte | Verwendung |
|---|---|---|
| `PublicationStates` | `NotPublic`, `Public` | Veröffentlichungsstatus einer Veranstaltung |
| `EventState` | `NotDefined`, `Public`, `NotPublic`, `DeadlineReached`, `Finished` | Berechneter Zustand (aus Status + Datum) |
| `EventType` | `Unspecified`, `PhysicalAttendance`, `Webinar` | Art der Veranstaltung |
| `GuestSalutations` | `Mr`, `Mrs` | Anrede für Gastanmeldungen |
| `MemberGuestRelationTypes` | – | Beziehungstyp Mitglied–Gast |
