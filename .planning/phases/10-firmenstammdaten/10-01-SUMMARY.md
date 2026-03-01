---
phase: 10-firmenstammdaten
plan: 01
subsystem: domain + service + ui
tags: [address-book, company, admin-ui, migration, autocomplete]
dependency_graph:
  requires:
    - EventCompany entity
    - CompanyInvitationFormModel
    - CompanyInvitationService
  provides:
    - Company entity mit FK auf EventCompany
    - CompanyService (CRUD + Search)
    - CompanyValidator (FluentValidation)
    - Admin-Seiten /admin/companies (List/Form)
    - Einladungsformular mit Adressbuch-Auswahl
  affects:
    - Phase 04 Einladungsworkflow (Freitext ersetzt durch Adressbuch)
tech_stack:
  patterns:
    - EF Core nullable FK für Rückwärtskompatibilität
    - Blazor @oninput Autocomplete (kein JS)
    - Service Layer Pattern
key_files:
  created:
    - EventCenter.Web/Domain/Entities/Company.cs
    - EventCenter.Web/Services/CompanyService.cs
    - EventCenter.Web/Validators/CompanyValidator.cs
    - EventCenter.Web/Components/Pages/Admin/Companies/CompanyList.razor
    - EventCenter.Web/Components/Pages/Admin/Companies/CompanyForm.razor
    - EventCenter.Web/Data/Migrations/20260301213356_AddCompanyAddressBook.cs
  modified:
    - EventCenter.Web/Domain/Entities/EventCompany.cs (CompanyId? + Company? nav)
    - EventCenter.Web/Domain/EventCenterDbContext.cs (Companies DbSet)
    - EventCenter.Web/Program.cs (CompanyService DI)
    - EventCenter.Web/Models/CompanyInvitationFormModel.cs (CompanyId?)
    - EventCenter.Web/Validators/CompanyInvitationValidator.cs (CompanyId required)
    - EventCenter.Web/Services/CompanyInvitationService.cs (CompanyId speichern)
    - EventCenter.Web/Components/Pages/Admin/Events/CompanyInvitationForm.razor (Adressbuch-UI)
    - EventCenter.Web/Components/Pages/Admin/Index.razor (Dashboard-Karte)
decisions:
  - key: CompanyId nullable FK
    rationale: Rückwärtskompatibilität — bestehende Einladungen ohne Adressbucheintrag bleiben gültig
    impact: EventCompany.CompanyId ist nullable; Validator erzwingt Pflichtfeld nur für neue Formulareingaben
  - key: Autocomplete per @oninput (kein Typeahead-Package)
    rationale: Kein zusätzliches JS-Paket nötig; CompanyService.SearchAsync liefert max. 20 Treffer
    impact: Suchergebnisse erscheinen als list-group-item unterhalb des Eingabefelds
  - key: Batch-Modus nutzt Select-Dropdown statt Autocomplete
    rationale: Bei mehreren Zeilen ist ein Dropdown übersichtlicher als je ein Suchfeld
    impact: allCompanies-Liste wird einmalig beim Init geladen
metrics:
  tests_added: 0
  tests_passing: 147
  commits: 2
  completed_date: "2026-03-01"
---

# Phase 10 Plan 01: Firmenstammdaten (Company Address Book)

**One-liner:** Zentrales Firmenadressbuch mit Admin-CRUD und Autocomplete-Auswahl im Einladungsformular — kein Freitext mehr für Firmendaten.

## Was wurde gebaut

### Company Entity & Migration

- Neue Entität `Company` (Id, Name, ContactEmail, ContactPhone, ContactFirstName, ContactLastName, Street, PostalCode, City, Notes)
- `EventCompany` erhält `CompanyId?` (nullable FK) + `Company?` Navigation
- EF Core Migration `AddCompanyAddressBook` erstellt `Companies`-Tabelle und FK-Spalte

### CompanyService

- `GetAllAsync()` — sortiert nach Name
- `GetByIdAsync(int id)`
- `SearchAsync(string term)` — LIKE-Suche auf Name, Email, City
- `CreateAsync` / `UpdateAsync`
- `DeleteAsync` — verweigert, wenn Firma bereits in Einladungen verknüpft ist

### CompanyValidator (FluentValidation)

- Name + ContactEmail Pflichtfelder
- E-Mail-Format, Längenprüfungen für alle Felder

### Admin-UI

**`/admin/companies`** — Tabelle mit Name, Ansprechpartner, E-Mail, Telefon, Ort; Bearbeiten/Löschen-Aktionen; Löschen-Modal
**`/admin/companies/create`** + **`/admin/companies/edit/{id}`** — Formular mit 4 Karten (Firmendaten, Ansprechpartner, Adresse, Notizen)
**Admin-Dashboard** (`/admin`) — neue Karte „Firmenstammdaten" → `/admin/companies`

### Einladungsformular-Umbau

**Einzelmodus:** „Firmendaten"-Sektion ersetzt durch Suchfeld mit Live-Autocomplete (`@oninput` → `CompanyService.SearchAsync`). Nach Auswahl: ausgewählte Firma als read-only-Box angezeigt; „Firma ändern"-Button zum Zurücksetzen.

**Batch-Modus:** Pro Zeile ein `<select>`-Dropdown mit allen Firmen statt Freitext-Inputs.

**FormModel:** `CompanyInvitationFormModel.CompanyId?` hinzugefügt; Validator erzwingt `CompanyId > 0`. CompanyName/ContactEmail/ContactPhone werden weiterhin auf `EventCompany` gespeichert (kopiert von der ausgewählten Firma).

**Edit-Modus:** Lädt `Company` per `CompanyId` und zeigt sie als bereits ausgewählt an.

**Service:** `CreateInvitationAsync` + `UpdateInvitationAsync` speichern `CompanyId` in `EventCompany`.

## Test-Fixes (pre-existing)

Neben der neuen Funktion wurden 3 vorbestehende Testfehler behoben:

1. **`ExportParticipantList_ReturnsValidExcel`** — Spalten-Assertions auf 8 Spalten aktualisiert (Agendapunkte + Zusatzoptionen waren im Vorcommit hinzugekommen)
2. **`RegisterGuestAsync_CreatesRegistrationAgendaItems`** — `await Task.Delay(100)` nach `RegisterMaklerAsync` eingefügt
3. **`GetGuestRegistrationsAsync_ReturnsGuestsWithDetails`** — `await Task.Delay(100)` nach Broker- und Guest1-Registrierung
   → Ursache: Fire-and-forget `Task.Run` in den Service-Methoden greift auf denselben `_context` zu; unter Last konkurrierte dies mit dem nächsten DB-Aufruf auf demselben Thread

## Commits

- `20e6a0c` — feat: add company address book (Firmenstammdaten)
- `f91ddab` — fix: resolve 3 pre-existing test failures

## Verifikation

✅ Build: 0 Errors
✅ Migration angewendet
✅ 147/147 Tests grün
✅ Neue Anforderungen FIRM-01 bis FIRM-05 erfüllt
