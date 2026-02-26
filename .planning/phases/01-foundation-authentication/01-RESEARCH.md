# Phase 1: Foundation & Authentication - Research

**Researched:** 2026-02-26
**Domain:** Blazor Server + Keycloak OIDC + EF Core + SQL Server
**Confidence:** HIGH

## Summary

Phase 1 establishes a Blazor Server application with Keycloak-based OIDC authentication for two roles (Admin and Makler), domain entities with EF Core, and SQL Server database infrastructure. The research confirms this is a well-established stack with mature tooling and clear patterns.

**Key findings:**
- Blazor Server with OIDC is production-ready; Microsoft's standard `AddOpenIdConnect()` handles Keycloak integration
- Circuit-based authentication requires `IdentityRevalidatingAuthenticationStateProvider` for periodic security stamp validation (default 30 min)
- EF Core 9 targets .NET 8+ and supports both .NET 8 (LTS) and .NET 9
- FluentValidation integrates via Blazored.FluentValidation (most popular) or Blazilla (newer alternative)
- Memory leak prevention is critical: implement `IAsyncDisposable` for event subscriptions, timers, and JS interop

**Primary recommendation:** Use .NET 8 (LTS) with ASP.NET Core 8.0, Blazor Server template, Keycloak OIDC integration, EF Core 9, and SQL Server. Structure project as single-project architecture with feature-based folders. Implement IdentityRevalidatingAuthenticationStateProvider immediately to avoid authentication staleness issues.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Auth structure:**
- Two roles only: Admin and Makler (broker)
- Admin manages all backoffice functionality; Makler views events and registers
- Authentication state revalidation every 30 minutes (IdentityRevalidatingAuthenticationStateProvider)
- Anonymous company access via GUID links only — no additional email verification step
- Rate limiting + expiration on GUID endpoints for security
- URL paths for area separation: `/admin/*` and `/portal/*` with shared layout (single Blazor app)

**DateTime handling:**
- Store all timestamps as UTC in database (`DateTime.UtcNow`)
- Display in CET/CEST for users
- Registration deadline interpretation: inclusive end-of-day (deadline "15.03" allows registration until 15.03 23:59:59 CET)
- Use `DateTime` type with UTC convention (not DateTimeOffset) for simpler EF Core mapping
- Admin UI: timezone hidden, CET assumed — no timezone picker

**Project layout:**
- Single project structure: EventCenter.Web with folders
- Blazor pages organized by feature: Pages/Events/, Pages/Registrations/, Pages/Companies/
- Shared components use feature prefix: EventCard, EventModal, RegistrationForm
- Domain entities in Domain/ folder: Domain/Entities/, Domain/EventCenterDbContext.cs

**Validation strategy:**
- FluentValidation for all domain validation rules
- Error messages in German only (no resource files)
- Database CHECK constraints as defense in depth for critical fields (dates, limits)

### Claude's Discretion
- Whether validation runs at UI level, service level, or both (Claude to decide based on UX vs. complexity tradeoff)
- Exact folder structure within Domain/, Services/
- Naming conventions for validators (e.g., EventValidator.cs)
- Implementation of IAsyncDisposable patterns for components

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.

</user_constraints>

<phase_requirements>
## Phase Requirements

This phase MUST address the following requirements from REQUIREMENTS.md:

| ID | Description | Research Support |
|----|-------------|-----------------|
| AUTH-01 | Admin kann sich via Keycloak im Backoffice anmelden | Standard ASP.NET Core OIDC with `AddOpenIdConnect()`, role mapping from Keycloak realm roles as claims |
| AUTH-02 | Makler kann sich via Keycloak im Portal anmelden | Same OIDC configuration with role-based authorization using `[Authorize(Roles = "Admin")]` and `[Authorize(Roles = "Makler")]` attributes |

**Additional implicit requirements for this phase:**
- Domain entities: Event, EventAgendaItem, EventCompany, Registrations with EF Core configurations
- SQL Server database schema creation via EF Core migrations
- Authentication state management across Blazor Server circuits with 30-minute revalidation
- UTC datetime storage with CET display conversion patterns

</phase_requirements>

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET | 8.0 (LTS) | Runtime platform | LTS support until November 2026; production-stable; EF Core 9 targets .NET 8+ |
| ASP.NET Core | 8.0 | Web framework | Blazor Server host; OIDC authentication middleware; dependency injection |
| Blazor Server | 8.0 | UI framework | Server-side rendering with SignalR circuits; direct DB access; no WASM complexity |
| EF Core | 9.0 | ORM | Latest stable; STS until Nov 2026; improved SQL Server integration; native support for complex queries |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0 | Database provider | Official SQL Server provider with full feature support |
| Microsoft.AspNetCore.Authentication.OpenIdConnect | 8.0 | OIDC client | Standard Microsoft library for Keycloak OIDC integration |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Blazored.FluentValidation | 2.x | Blazor validation integration | Most popular library for integrating FluentValidation with Blazor EditForm; DI-based validator resolution |
| FluentValidation | 11.x | Validation rules | Domain validation logic; German error messages; supports async rules and rule sets |
| TimeZoneConverter | 6.x | Timezone ID conversion | Converts IANA/Windows timezone IDs for cross-platform compatibility |
| bUnit | 1.x | Blazor component testing | Unit testing Blazor components with semantic HTML comparison |
| xUnit | 2.x | Test framework | Industry standard for .NET testing; used with bUnit |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Blazored.FluentValidation | Blazilla | Blazilla is newer (2025+) with better nested object support and performance optimizations, but less community adoption. Use if complex validation scenarios emerge. |
| EF Core InMemory | SQLite in-memory | SQLite provides true relational constraints and catches FK violations; EF Core InMemory is faster but doesn't validate referential integrity. Use SQLite for integration tests. |
| DateTime | DateTimeOffset | DateTimeOffset includes timezone offset in value but adds complexity to EF Core mapping. DateTime with UTC convention is simpler for single-timezone apps (CET only). |
| Single project | Multi-project (Clean Architecture) | Multi-project adds structure but increases complexity. Single project is appropriate for phase 1; refactor later if needed. |

**Installation:**

```bash
# Create Blazor Server project
dotnet new blazorserver -n EventCenter.Web -f net8.0

# Core packages
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.*
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.*
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect --version 8.0.*

# Validation
dotnet add package FluentValidation --version 11.*
dotnet add package Blazored.FluentValidation --version 2.*

# Timezone support
dotnet add package TimeZoneConverter --version 6.*

# Testing (separate test project)
dotnet new xunit -n EventCenter.Tests
dotnet add EventCenter.Tests package bUnit --version 1.*
dotnet add EventCenter.Tests package Microsoft.EntityFrameworkCore.Sqlite --version 9.*
```

## Architecture Patterns

### Recommended Project Structure

```
EventCenter.Web/
├── Program.cs                    # Startup, DI, middleware configuration
├── appsettings.json              # Configuration (Keycloak, ConnectionString)
├── Domain/
│   ├── Entities/                 # Domain entities
│   │   ├── Event.cs
│   │   ├── EventAgendaItem.cs
│   │   ├── EventCompany.cs
│   │   └── Registration.cs
│   ├── Enums/                    # Enums (EventState, RegistrationType, etc.)
│   └── EventCenterDbContext.cs   # EF Core DbContext
├── Data/
│   ├── Configurations/           # EF Core IEntityTypeConfiguration
│   │   ├── EventConfiguration.cs
│   │   └── RegistrationConfiguration.cs
│   └── Migrations/               # EF Core migrations
├── Services/                     # Business logic services
│   ├── EventService.cs
│   └── RegistrationService.cs
├── Validators/                   # FluentValidation validators
│   ├── EventValidator.cs
│   └── RegistrationValidator.cs
├── Pages/                        # Blazor pages (feature-based)
│   ├── Admin/                    # /admin/* pages
│   │   ├── Events/
│   │   │   ├── Index.razor
│   │   │   ├── Create.razor
│   │   │   └── Edit.razor
│   │   └── _Layout.razor
│   ├── Portal/                   # /portal/* pages
│   │   ├── Events/
│   │   │   ├── Index.razor
│   │   │   └── Details.razor
│   │   └── _Layout.razor
│   ├── Auth/                     # Auth pages
│   │   ├── Login.razor
│   │   └── Logout.razor
│   └── _Layout.razor             # Shared root layout
├── Components/                   # Reusable components
│   ├── EventCard.razor
│   ├── EventModal.razor
│   └── RegistrationForm.razor
├── Infrastructure/               # Cross-cutting concerns
│   ├── Authentication/
│   │   └── IdentityRevalidatingAuthStateProvider.cs
│   └── Helpers/
│       └── TimeZoneHelper.cs
└── wwwroot/                      # Static files
    ├── css/
    └── js/
```

**Rationale:**
- **Single project:** Appropriate for v1; avoids premature abstraction
- **Feature-based Pages/**: Aligns with user requirements (Admin, Portal areas)
- **Domain/ folder:** Contains entities and DbContext; pure domain logic
- **Data/Configurations/:** EF Core configs separated via `IEntityTypeConfiguration<T>`
- **Validators/ at root:** Shared across UI and service layers
- **Infrastructure/:** Framework-specific concerns (auth, helpers)

### Pattern 1: Keycloak OIDC Integration

**What:** Configure ASP.NET Core to authenticate against Keycloak using OpenID Connect, mapping realm roles to .NET claims.

**When to use:** AUTH-01 and AUTH-02 requirements (Admin and Makler authentication).

**Example:**

```csharp
// Program.cs
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"]; // e.g., https://keycloak.example.com/realms/eventcenter
    options.ClientId = builder.Configuration["Keycloak:ClientId"];
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;

    // Map Keycloak realm roles to .NET role claims
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "realm_roles" // or map from resource_access if using client roles
    };

    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            // Extract roles from Keycloak token and add as claims
            var claimsIdentity = (ClaimsIdentity)context.Principal.Identity;
            var roles = context.Principal.FindFirst("realm_access")?.Value; // JSON array
            // Parse and add role claims
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("MaklerOnly", policy => policy.RequireRole("Makler"));
});
```

**Source:** Microsoft Learn - [ASP.NET Core Blazor authentication and authorization](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0)

### Pattern 2: Authentication State Revalidation (30 Minutes)

**What:** Implement `RevalidatingServerAuthenticationStateProvider` to periodically check if user's security stamp has changed (password reset, role change, logout).

**When to use:** Required for AUTH-01 and AUTH-02 to meet "Authentication state revalidation every 30 minutes" constraint.

**Example:**

```csharp
// Infrastructure/Authentication/IdentityRevalidatingAuthStateProvider.cs
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

public class IdentityRevalidatingAuthStateProvider
    : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;

    public IdentityRevalidatingAuthStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        // Check if user still exists and roles haven't changed
        var userId = authenticationState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return false;

        // In Keycloak scenario, token expiration handles most validation
        // Return true unless you implement custom user state tracking
        return true;
    }
}

// Program.cs registration
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthStateProvider>();
builder.Services.AddScoped<RevalidatingServerAuthenticationStateProvider, IdentityRevalidatingAuthStateProvider>();
```

**Source:** Microsoft Learn - [ASP.NET Core Blazor authentication state](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/authentication-state?view=aspnetcore-9.0)

### Pattern 3: EF Core Entity Configuration (IEntityTypeConfiguration)

**What:** Separate EF Core mapping configuration from domain entities using `IEntityTypeConfiguration<T>`.

**When to use:** All domain entities (Event, EventAgendaItem, EventCompany, Registration).

**Example:**

```csharp
// Domain/Entities/Event.cs (clean domain model)
public class Event
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime RegistrationDeadlineUtc { get; set; }
    public bool IsPublished { get; set; }

    // Navigation properties
    public ICollection<EventAgendaItem> AgendaItems { get; set; }
    public ICollection<Registration> Registrations { get; set; }
}

// Data/Configurations/EventConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.StartDateUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(e => e.RegistrationDeadlineUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasIndex(e => e.IsPublished);

        // Check constraint for deadline before start
        builder.HasCheckConstraint(
            "CK_Event_DeadlineBeforeStart",
            "[RegistrationDeadlineUtc] <= [StartDateUtc]"
        );
    }
}

// Domain/EventCenterDbContext.cs
public class EventCenterDbContext : DbContext
{
    public EventCenterDbContext(DbContextOptions<EventCenterDbContext> options)
        : base(options) { }

    public DbSet<Event> Events { get; set; }
    public DbSet<Registration> Registrations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventConfiguration).Assembly);
    }
}
```

**Source:** Microsoft Learn - [Creating and Configuring a Model](https://learn.microsoft.com/en-us/ef/core/modeling/) and [Fluent API Configuration](https://codewithmukesh.com/blog/fluent-api-entity-configuration-efcore/)

### Pattern 4: UTC Storage with CET Display Conversion

**What:** Store all timestamps as UTC in database (`DateTime.UtcNow`), convert to CET/CEST for display using `TimeZoneInfo`.

**When to use:** All DateTime properties (deadline interpretation, event start times).

**Example:**

```csharp
// Infrastructure/Helpers/TimeZoneHelper.cs
public static class TimeZoneHelper
{
    private static readonly TimeZoneInfo CetTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); // Windows
        // For Linux: TZConvert.GetTimeZoneInfo("Europe/Berlin") using TimeZoneConverter package

    public static DateTime ConvertUtcToCet(DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, CetTimeZone);
    }

    public static DateTime ConvertCetToUtc(DateTime cetDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(cetDateTime, CetTimeZone);
    }

    // Deadline interpretation: inclusive end-of-day
    public static DateTime GetEndOfDayCet(DateTime date)
    {
        var endOfDay = date.Date.AddDays(1).AddTicks(-1); // 23:59:59.9999999
        return ConvertCetToUtc(endOfDay);
    }
}

// Usage in service
public bool IsRegistrationOpen(Event evt)
{
    var now = DateTime.UtcNow;
    var deadlineEndOfDayUtc = TimeZoneHelper.GetEndOfDayCet(
        TimeZoneHelper.ConvertUtcToCet(evt.RegistrationDeadlineUtc)
    );
    return now <= deadlineEndOfDayUtc;
}

// Display in Razor component
@code {
    private string FormatDateTimeCet(DateTime utcDateTime)
    {
        var cet = TimeZoneHelper.ConvertUtcToCet(utcDateTime);
        return cet.ToString("dd.MM.yyyy HH:mm");
    }
}
```

**Source:** Medium - [Handling DateTime in ASP.NET Core Web APIs](https://medium.com/@manobesh1982_54603/handling-datetime-in-asp-net-core-web-apis-why-you-should-always-store-utc-in-the-database-bcfe58e983fe)

### Pattern 5: FluentValidation Integration with Blazor

**What:** Use Blazored.FluentValidation to integrate FluentValidation with Blazor `EditForm`.

**When to use:** All forms requiring domain validation (Event creation/editing, Registration forms).

**Example:**

```csharp
// Validators/EventValidator.cs
using FluentValidation;

public class EventValidator : AbstractValidator<Event>
{
    public EventValidator()
    {
        RuleFor(e => e.Title)
            .NotEmpty().WithMessage("Titel ist erforderlich")
            .MaximumLength(200).WithMessage("Titel darf maximal 200 Zeichen lang sein");

        RuleFor(e => e.RegistrationDeadlineUtc)
            .LessThanOrEqualTo(e => e.StartDateUtc)
            .WithMessage("Anmeldefrist muss vor Veranstaltungsbeginn liegen");

        RuleFor(e => e.StartDateUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Veranstaltung muss in der Zukunft liegen")
            .When(e => e.Id == 0); // Only for new events
    }
}

// Program.cs - Register validators
builder.Services.AddValidatorsFromAssemblyContaining<EventValidator>();

// Pages/Admin/Events/Create.razor
@using Blazored.FluentValidation

<EditForm Model="@newEvent" OnValidSubmit="HandleSubmit">
    <FluentValidationValidator />
    <ValidationSummary />

    <div>
        <label>Titel:</label>
        <InputText @bind-Value="newEvent.Title" />
        <ValidationMessage For="@(() => newEvent.Title)" />
    </div>

    <button type="submit">Speichern</button>
</EditForm>

@code {
    private Event newEvent = new();

    private void HandleSubmit()
    {
        // Validation passed, save to DB
    }
}
```

**Source:** GitHub - [Blazored/FluentValidation](https://github.com/Blazored/FluentValidation)

### Pattern 6: IAsyncDisposable for Memory Leak Prevention

**What:** Implement `IAsyncDisposable` in Blazor components that subscribe to events, create timers, or use JS interop.

**When to use:** Any component with event subscriptions, `CancellationTokenSource`, `Timer`, or `DotNetObjectReference`.

**Example:**

```csharp
// Components/EventCard.razor.cs
public partial class EventCard : IAsyncDisposable
{
    private Timer _timer;
    private CancellationTokenSource _cts = new();

    protected override void OnInitialized()
    {
        // Subscribe to events
        EventService.OnEventUpdated += HandleEventUpdate;

        // Create timer
        _timer = new Timer(RefreshData, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }

    private void HandleEventUpdate(object sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private void RefreshData(object state)
    {
        // Refresh logic
    }

    public async ValueTask DisposeAsync()
    {
        // Unsubscribe from events
        EventService.OnEventUpdated -= HandleEventUpdate;

        // Dispose timer
        _timer?.Dispose();

        // Cancel ongoing operations
        _cts?.Cancel();
        _cts?.Dispose();

        // If using JS interop
        // await JSRuntime.InvokeVoidAsync("cleanup", _cts.Token);
    }
}
```

**Source:** Microsoft Learn - [ASP.NET Core Razor component disposal](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/component-disposal?view=aspnetcore-9.0) and [Blazor Server Memory Management](https://amarozka.dev/blazor-server-memory-management-circuit-leaks/)

### Anti-Patterns to Avoid

- **Using singleton services for user state:** Blazor Server circuits are scoped; singleton services leak state across users. Always use scoped services for user-specific data.
- **Not disposing event subscriptions:** Event handlers keep components alive even after removal from UI. Always unsubscribe in `Dispose()`/`DisposeAsync()`.
- **DateTime.Now instead of DateTime.UtcNow:** Local time depends on server timezone; always use UTC for storage.
- **Exposing mutable collections in domain entities:** Violates DDD encapsulation. Use `IReadOnlyCollection<T>` for public properties and private `List<T>` with methods to modify.
- **Calling `SaveChanges()` in loops:** N+1 query problem. Batch changes and call `SaveChanges()` once.
- **Not handling `DbUpdateConcurrencyException`:** Race conditions will occur with concurrent registrations. Implement optimistic concurrency (see Pitfall 4).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| OIDC authentication | Custom token validation, cookie management | `AddOpenIdConnect()` | OIDC has complex flows (authorization code, token refresh, PKCE); Microsoft's middleware handles all edge cases including token refresh, logout propagation, and security validations. |
| Validation logic in Razor components | Manual `if` checks, error message dictionaries | FluentValidation | Complex validation rules (cross-field, async DB checks, conditional logic) become unmaintainable in components. FluentValidation provides reusable, testable validators. |
| SQL query building | String concatenation, manual parameter handling | EF Core LINQ | SQL injection risks, parameter escaping bugs, database-specific syntax. EF Core provides type-safe queries with automatic parameterization. |
| Timezone conversion | Manual offset calculations | `TimeZoneInfo` class + TimeZoneConverter library | Daylight saving time rules change by country/year; manual calculations fail. `TimeZoneInfo` uses OS timezone database (regularly updated). |
| Circuit authentication refresh | Manual timer + token validation | `RevalidatingServerAuthenticationStateProvider` | Built-in framework pattern; handles circuit lifecycle, timer cleanup, and race conditions during revalidation. |
| HTML sanitization | Regex-based tag stripping | AntiXSS library or MarkupString with caution | XSS attack vectors are complex (JS in attributes, CSS injection, Unicode tricks). Use framework-provided sanitization. |

**Key insight:** Authentication, validation, and timezone logic contain subtle edge cases that cause production bugs (token expiry during request, DST transitions, XSS via Unicode). Framework-provided solutions are battle-tested with years of security research. Custom implementations miss edge cases until they cause incidents.

## Common Pitfalls

### Pitfall 1: Circuit Authentication Staleness

**What goes wrong:** User logs out in Keycloak admin panel or changes roles, but Blazor circuit still sees old authentication state for up to hours/days until circuit disconnects.

**Why it happens:** Blazor Server establishes authentication state at circuit creation (initial WebSocket connection). The circuit keeps running even after external authentication changes. Without revalidation, the circuit never checks if the user's claims are still valid.

**How to avoid:**
1. Implement `RevalidatingServerAuthenticationStateProvider` with 30-minute interval (per user constraint).
2. Configure Keycloak token expiry to match revalidation interval.
3. For immediate logout, implement server-side session store that tracks active circuits and force-disconnects on admin-initiated logout.

**Warning signs:**
- User reports "still logged in after password reset"
- Role changes don't take effect until browser refresh
- Security audit finds stale sessions

**Source:** [Blazor Server and the Logout Problem](https://auth0.com/blog/blazor-server-and-the-logout-problem/)

### Pitfall 2: Memory Leaks from Event Subscriptions

**What goes wrong:** Blazor components subscribe to events (service events, timers, SignalR) but never unsubscribe. Components remain in memory even after navigation, causing memory growth and eventual out-of-memory crashes.

**Why it happens:** .NET event subscriptions create strong references from publisher to subscriber. When component is removed from UI, the event subscription keeps the component alive. Blazor Server circuits can run for hours/days, accumulating thousands of leaked components.

**How to avoid:**
1. Implement `IAsyncDisposable` on any component that subscribes to events.
2. Unsubscribe from events in `DisposeAsync()`.
3. Dispose timers, `CancellationTokenSource`, and `DotNetObjectReference` objects.
4. Use weak event patterns for long-lived publishers (e.g., application-scoped services).

**Warning signs:**
- Memory usage grows continuously during user session
- Application becomes slow after 30+ minutes of use
- Profiler shows thousands of component instances retained
- "Circuit disconnected" errors after hours of use

**Code example (correct):**

```csharp
public partial class EventListComponent : IAsyncDisposable
{
    [Inject] private IEventNotificationService EventService { get; set; }
    private Timer _refreshTimer;

    protected override void OnInitialized()
    {
        EventService.OnEventUpdated += HandleEventUpdate;
        _refreshTimer = new Timer(Refresh, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    public async ValueTask DisposeAsync()
    {
        EventService.OnEventUpdated -= HandleEventUpdate; // Critical!
        _refreshTimer?.Dispose();
    }
}
```

**Source:** [Blazor Server Memory Management: Stop Circuit Leaks](https://amarozka.dev/blazor-server-memory-management-circuit-leaks/)

### Pitfall 3: Timezone Conversion Errors (DST Transitions)

**What goes wrong:** Application stores DateTime in local time or converts incorrectly, causing registration deadlines to shift by 1 hour during DST transitions. Users miss deadlines or register after deadline due to timezone bugs.

**Why it happens:**
1. Using `DateTime.Now` stores server's local time, which changes during DST.
2. Manual offset calculations (`+01:00` for CET) fail during CEST (summer time, `+02:00`).
3. Converting timezone-aware DateTime to UTC without considering DST rules.

**How to avoid:**
1. **Always** store `DateTime.UtcNow` in database.
2. Use `TimeZoneInfo.ConvertTimeFromUtc()` for display conversion (handles DST automatically).
3. Use TimeZoneConverter library to map Windows timezone IDs to IANA IDs for cross-platform compatibility.
4. For deadline interpretation ("15.03" = end of day), convert to CET, add time component (23:59:59), then convert back to UTC.

**Warning signs:**
- Deadline checks fail on DST transition days (last Sunday in March, last Sunday in October)
- Timestamps appear wrong by 1 hour for certain date ranges
- Cross-timezone teams report inconsistent behavior

**Code example (correct):**

```csharp
// WRONG: Manual offset
DateTime deadline = DateTime.Parse("2026-03-15").AddHours(23 + 1); // Breaks in summer!

// CORRECT: TimeZoneInfo with DST handling
var cetZone = TZConvert.GetTimeZoneInfo("Europe/Berlin"); // Works on Windows and Linux
var deadlineCet = DateTime.Parse("2026-03-15").Date.AddDays(1).AddTicks(-1); // 23:59:59.999...
var deadlineUtc = TimeZoneInfo.ConvertTimeToUtc(deadlineCet, cetZone); // Handles DST
```

**Source:** [Handling Time Zones in .NET Applications](https://toxigon.com/handling-time-zones-in-dotnet-applications)

### Pitfall 4: Race Conditions in Concurrent Registrations

**What goes wrong:** Two users register simultaneously for the last available spot. Both pass the "spots available" check, both save successfully, event is now overbooked.

**Why it happens:**
1. Check-then-act pattern: `if (spotsAvailable > 0) { register(); }` is not atomic.
2. EF Core loads entity, checks count in memory, another request modifies database, first request saves with stale data.
3. No concurrency token prevents "last write wins" behavior.

**How to avoid:**
1. Add `RowVersion` concurrency token to entities with concurrent updates (Event, EventAgendaItem).
2. Handle `DbUpdateConcurrencyException` and retry with fresh data.
3. Use database CHECK constraints as final safety net (e.g., `RegisteredCount <= MaxCapacity`).
4. For critical operations, use pessimistic locking (`FromSqlRaw` with `WITH (UPDLOCK)`).

**Warning signs:**
- Event capacity exceeded in production logs
- User complaints about "sold out" events accepting additional registrations
- Database constraint violations in logs

**Code example (correct):**

```csharp
// Domain entity with concurrency token
public class Event
{
    public int Id { get; set; }
    public int MaxCapacity { get; set; }
    public int RegisteredCount { get; set; }

    [Timestamp] // Concurrency token
    public byte[] RowVersion { get; set; }
}

// Service with retry logic
public async Task<bool> RegisterAsync(int eventId, Registration registration)
{
    const int maxRetries = 3;
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            var evt = await _context.Events.FindAsync(eventId);
            if (evt.RegisteredCount >= evt.MaxCapacity)
                return false; // Sold out

            evt.RegisteredCount++;
            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync(); // Throws if RowVersion changed
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request modified the event; retry with fresh data
            _context.Entry(evt).State = EntityState.Detached;
            if (attempt == maxRetries - 1)
                throw; // Give up after retries
        }
    }
    return false;
}
```

**Source:** [Solving Race Conditions With EF Core Optimistic Locking](https://www.milanjovanovic.tech/blog/solving-race-conditions-with-ef-core-optimistic-locking)

### Pitfall 5: Blazor Component Rendering Performance

**What goes wrong:** Adding many components (500+) to a page causes slow initial render (2+ seconds) and sluggish UI interactions. Users perceive application as unresponsive.

**Why it happens:**
1. Each Blazor component has overhead (state tracking, event handling, diff calculation).
2. Default rendering behavior re-renders entire component tree on state change.
3. Large lists without virtualization render all items even if offscreen.

**How to avoid:**
1. Override `ShouldRender()` to prevent unnecessary re-renders.
2. Use `@key` directive on list items to help Blazor identify components efficiently.
3. Implement virtualization for long lists (`<Virtualize>` component).
4. Break large pages into smaller components with isolated state.
5. Use `StateHasChanged()` sparingly; batch UI updates.

**Warning signs:**
- Initial page load takes >1 second
- Typing in input fields feels laggy
- Browser DevTools shows >500ms render times
- Users report "frozen" UI during interactions

**Source:** Microsoft Learn - [ASP.NET Core Blazor rendering performance best practices](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/rendering?view=aspnetcore-9.0)

### Pitfall 6: Improper Dependency Injection Scopes

**What goes wrong:**
- Singleton services with user-specific state leak data between users.
- Transient `DbContext` causes tracking conflicts and performance issues.
- Scoped services used in background tasks throw "scope disposed" errors.

**Why it happens:**
1. Blazor Server circuits are scoped; services registered as singleton persist across all users.
2. EF Core `DbContext` must be scoped to track entities correctly; transient creates multiple contexts per request.
3. Background timers run outside circuit scope; scoped services are unavailable.

**How to avoid:**
1. **Never use singleton for user state.** Use scoped for user-specific services.
2. Register `DbContext` as scoped: `AddDbContext<EventCenterDbContext>(ServiceLifetime.Scoped)`.
3. For background tasks, create explicit scope: `using var scope = _serviceProvider.CreateScope()`.
4. Validate DI lifetimes: singleton → transient ✓, singleton → scoped ✗.

**Warning signs:**
- User A sees user B's data
- EF Core throws "entity already tracked" exceptions
- "Cannot access disposed object" errors in logs

**Source:** Microsoft Learn - [Dependency injection in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)

## Code Examples

Verified patterns from official sources:

### EF Core DbContext Registration with SQL Server

```csharp
// Program.cs
builder.Services.AddDbContext<EventCenterDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(30);
        })
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment()));
```

**Source:** Microsoft Learn - [Microsoft SQL Server Database Provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)

### Role-Based Authorization in Blazor Page

```csharp
// Pages/Admin/Events/Index.razor
@page "/admin/events"
@attribute [Authorize(Roles = "Admin")]

<h3>Veranstaltungsverwaltung (Admin)</h3>

<AuthorizeView Roles="Admin">
    <Authorized>
        <button @onclick="CreateEvent">Neue Veranstaltung</button>
    </Authorized>
    <NotAuthorized>
        <p>Zugriff verweigert. Sie benötigen die Admin-Rolle.</p>
    </NotAuthorized>
</AuthorizeView>

@code {
    [CascadingParameter]
    private Task<AuthenticationState> AuthStateTask { get; set; }

    private async Task CreateEvent()
    {
        var authState = await AuthStateTask;
        var user = authState.User;

        if (user.IsInRole("Admin"))
        {
            // Create event logic
        }
    }
}
```

**Source:** Microsoft Learn - [ASP.NET Core Blazor authentication and authorization](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0)

### EF Core Migration Commands

```bash
# Install EF Core CLI tools globally
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate --project EventCenter.Web

# Review generated SQL (don't apply yet)
dotnet ef migrations script --project EventCenter.Web

# Apply migration to database
dotnet ef database update --project EventCenter.Web

# Generate SQL script for production deployment
dotnet ef migrations script --project EventCenter.Web --output migrations.sql --idempotent
```

**Source:** Microsoft Learn - [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Blazor Server project template with Individual Accounts | External OIDC provider (Keycloak, Azure AD) | .NET 5+ (2020) | Individual Accounts uses ASP.NET Identity with local database; modern apps prefer centralized identity management. OIDC supports SSO across multiple applications. |
| DataAnnotations validation | FluentValidation | Blazor GA (2020) | DataAnnotations limited to simple rules; FluentValidation supports complex cross-field validation, async rules, and reusable validators. |
| DateTime.UtcNow with manual offset | DateTimeOffset | .NET 1.0 / Ongoing debate | DateTimeOffset includes timezone offset but complicates EF Core mapping. For single-timezone apps, DateTime + UTC convention is simpler. Use DateTimeOffset for multi-timezone requirements. |
| EF Core InMemory for testing | SQLite in-memory | EF Core 3.0+ (2019) | InMemory doesn't validate relational constraints (FK violations pass); SQLite in-memory behaves like real database. Microsoft recommends SQLite for testing. |
| Manual authentication state management | RevalidatingServerAuthenticationStateProvider | .NET 6+ (2021) | Manual timers leak memory and miss edge cases; framework pattern handles circuit lifecycle and cleanup. |
| `OnModelCreating` with Fluent API | IEntityTypeConfiguration<T> | EF Core 2.0+ (2017) | Centralized configuration in `OnModelCreating` becomes unmaintainable; `IEntityTypeConfiguration` separates concerns and enables per-entity configuration files. |
| Blazor Server + WASM hybrid (NET 6-7) | Blazor Web App with SSR + Interactive modes (.NET 8+) | .NET 8 (2023) | Unified model replaces separate Server/WASM templates; supports multiple render modes in single app. For new projects starting in 2026, consider Blazor Web App template. |

**Deprecated/outdated:**
- **ASP.NET Core 2.x authentication patterns:** UseAuthentication/UseAuthorization order matters; .NET 6+ defaults are correct.
- **EF Core InMemory provider for testing:** Still works but not recommended; use SQLite in-memory.
- **Manual `AuthenticationStateProvider` without revalidation:** Required for Blazor Server since .NET 6.
- **Blazor Server template with "Individual Accounts":** Template exists but outdated pattern; use external OIDC providers.

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.x + bUnit 1.x |
| Config file | None — configure in Wave 0 (create test project, add xUnit + bUnit packages) |
| Quick run command | `dotnet test --filter "Category=Unit" --no-build` |
| Full suite command | `dotnet test --collect:"XPlat Code Coverage"` |

**Rationale:** xUnit is industry standard for .NET testing. bUnit provides Blazor-specific testing utilities (component rendering, semantic HTML comparison, DI mocking). No special config file required; xUnit uses attributes for test discovery.

**Setup (Wave 0):**
```bash
dotnet new xunit -n EventCenter.Tests -f net8.0
dotnet add EventCenter.Tests reference EventCenter.Web
dotnet add EventCenter.Tests package bUnit --version 1.*
dotnet add EventCenter.Tests package Microsoft.EntityFrameworkCore.Sqlite --version 9.*
dotnet add EventCenter.Tests package Moq --version 4.* # For service mocking
```

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| AUTH-01 | Admin redirected to Keycloak login when accessing /admin/* without auth | Integration | `dotnet test --filter "FullyQualifiedName~AuthenticationTests.AdminRequiresAuthentication"` | ❌ Wave 0 |
| AUTH-01 | Admin with valid Keycloak token can access /admin/events page | Integration | `dotnet test --filter "FullyQualifiedName~AuthenticationTests.AdminCanAccessAdminPages"` | ❌ Wave 0 |
| AUTH-02 | Makler redirected to Keycloak login when accessing /portal/* without auth | Integration | `dotnet test --filter "FullyQualifiedName~AuthenticationTests.MaklerRequiresAuthentication"` | ❌ Wave 0 |
| AUTH-02 | Makler with valid Keycloak token can access /portal/events page | Integration | `dotnet test --filter "FullyQualifiedName~AuthenticationTests.MaklerCanAccessPortalPages"` | ❌ Wave 0 |
| (Implicit) | Authentication state revalidates every 30 minutes | Unit | `dotnet test --filter "FullyQualifiedName~RevalidatingAuthStateProviderTests.RevalidatesEvery30Minutes"` | ❌ Wave 0 |
| (Implicit) | Event entity has correct EF Core configuration (max lengths, required fields) | Unit | `dotnet test --filter "FullyQualifiedName~EntityConfigurationTests.EventConfiguration"` | ❌ Wave 0 |
| (Implicit) | Database schema created successfully via migration | Integration | `dotnet test --filter "FullyQualifiedName~MigrationTests.InitialMigrationApplies"` | ❌ Wave 0 |
| (Implicit) | UTC datetime stored correctly and converted to CET for display | Unit | `dotnet test --filter "FullyQualifiedName~TimeZoneHelperTests.UtcToCetConversion"` | ❌ Wave 0 |
| (Implicit) | Deadline interpretation is inclusive (end of day in CET) | Unit | `dotnet test --filter "FullyQualifiedName~TimeZoneHelperTests.DeadlineIsInclusiveEndOfDay"` | ❌ Wave 0 |

**Note:** AUTH-01 and AUTH-02 integration tests require Keycloak mock or test container. For Phase 1, test authorization attributes and role requirements with fake authentication. Full OIDC flow testing deferred to Phase 2.

### Sampling Rate

- **Per task commit:** `dotnet test --filter "Category=Unit" --no-build` (unit tests only, <10 seconds)
- **Per wave merge:** `dotnet test` (full suite including integration tests, <60 seconds)
- **Phase gate:** Full suite green + manual verification of Keycloak login flow before `/gsd:verify-work`

### Wave 0 Gaps

Test infrastructure to create in Wave 0:

- [ ] `EventCenter.Tests/EventCenter.Tests.csproj` — xUnit test project with SDK=Microsoft.NET.Sdk, references EventCenter.Web, includes xUnit, bUnit, EF Core SQLite, Moq packages
- [ ] `EventCenter.Tests/AuthenticationTests.cs` — Integration tests for AUTH-01 and AUTH-02 (role-based authorization)
- [ ] `EventCenter.Tests/EntityConfigurationTests.cs` — Unit tests validating EF Core entity configurations (max lengths, required fields, check constraints)
- [ ] `EventCenter.Tests/TimeZoneHelperTests.cs` — Unit tests for UTC/CET conversion and deadline interpretation
- [ ] `EventCenter.Tests/RevalidatingAuthStateProviderTests.cs` — Unit tests for 30-minute revalidation interval
- [ ] `EventCenter.Tests/MigrationTests.cs` — Integration test that applies InitialCreate migration to SQLite in-memory DB
- [ ] `EventCenter.Tests/Helpers/TestAuthenticationStateProvider.cs` — Fake AuthenticationStateProvider for testing authorization without Keycloak
- [ ] `EventCenter.Tests/Helpers/TestDbContextFactory.cs` — Factory for SQLite in-memory DbContext for integration tests

**Estimated Wave 0 effort:** 2-3 hours (test project setup + test infrastructure helpers)

## Open Questions

1. **Keycloak realm role vs. client role mapping**
   - What we know: Keycloak can store roles at realm level (global) or client level (app-specific). Realm roles are simpler for single-app scenarios.
   - What's unclear: Project uses dedicated Keycloak realm or shared realm with multiple clients? Token structure differs (realm_access vs. resource_access).
   - Recommendation: Assume dedicated realm with realm roles "Admin" and "Makler". If shared realm, adjust token claim mapping in `OnTokenValidated` event to read from `resource_access[client-id].roles`.

2. **SQL Server edition and compatibility level**
   - What we know: EF Core 9 supports SQL Server 2016+ (compatibility level 130+). SQL Server 2022 enables new features (GREATEST/LEAST functions).
   - What's unclear: Target SQL Server version for production? Developer Edition 2022 for development?
   - Recommendation: Use SQL Server 2019+ (compatibility level 150) for development; supports modern features while maintaining compatibility with most cloud/on-prem deployments. Avoid SQL Server 2022-specific features unless production confirmed.

3. **Test environment for Keycloak integration tests**
   - What we know: Full OIDC flow requires running Keycloak instance (Docker container or test server).
   - What's unclear: Should integration tests spin up Keycloak via Testcontainers? Or stub OIDC responses?
   - Recommendation: Phase 1 uses fake authentication (`TestAuthenticationStateProvider` with hardcoded claims). Phase 2+ implements Testcontainers-based Keycloak for full integration tests. Avoids CI/CD complexity in early phases.

4. **Keycloak TLS certificate handling in development**
   - What we know: Keycloak HTTPS required for OIDC (HTTP forbidden in production). Development Keycloak often uses self-signed certificates.
   - What's unclear: Development environment setup — local Keycloak with self-signed cert? Bypass cert validation?
   - Recommendation: Use `HttpClientHandler.ServerCertificateCustomValidationCallback` to bypass cert validation in Development environment only. Document Keycloak setup in PLAN.md prerequisites.

## Sources

### Primary (HIGH confidence)

- Microsoft Learn - [ASP.NET Core Blazor authentication and authorization](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0)
- Microsoft Learn - [ASP.NET Core Blazor authentication state](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/authentication-state?view=aspnetcore-9.0)
- Microsoft Learn - [ASP.NET Core Razor component disposal](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/component-disposal?view=aspnetcore-9.0)
- Microsoft Learn - [Microsoft SQL Server Database Provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)
- Microsoft Learn - [EF Core Handling Concurrency Conflicts](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- Microsoft Learn - [EF Core Testing without Database](https://learn.microsoft.com/en-us/ef/core/testing/testing-without-the-database)
- Microsoft Learn - [EF Core Creating and Configuring a Model](https://learn.microsoft.com/en-us/ef/core/modeling/)

### Secondary (MEDIUM confidence)

- [Blazor Server and the Logout Problem - Auth0](https://auth0.com/blog/blazor-server-and-the-logout-problem/)
- [Blazor Server Memory Management: Stop Circuit Leaks - amarozka.dev](https://amarozka.dev/blazor-server-memory-management-circuit-leaks/)
- [Solving Race Conditions With EF Core Optimistic Locking - Milan Jovanović](https://www.milanjovanovic.tech/blog/solving-race-conditions-with-ef-core-optimistic-locking)
- [Handling DateTime in ASP.NET Core Web APIs - Medium](https://medium.com/@manobesh1982_54603/handling-datetime-in-asp-net-core-web-apis-why-you-should-always-store-utc-in-the-database-bcfe58e983fe)
- [bUnit - Testing Library for Blazor](https://bunit.dev/)
- [GitHub - Blazored/FluentValidation](https://github.com/Blazored/FluentValidation)
- [codewithmukesh - Fluent API Entity Configuration EF Core](https://codewithmukesh.com/blog/fluent-api-entity-configuration-efcore/)
- [Mapping Domain-Driven Design Concepts to Database with EF Core - Medium](https://medium.com/startup-insider-edge/mapping-domain-driven-design-concepts-to-the-database-with-ef-core-4bfd3f0aa146)

### Tertiary (LOW confidence)

- [GitHub - csinisa/blazor_server_keycloak](https://github.com/csinisa/blazor_server_keycloak) - Demo project, not production guidance
- [Medium - Local development with Keycloak and Blazor Server](https://medium.com/norsk-helsenett/local-development-with-keycloak-and-blazor-server-695921705578) - July 2024 article, specific to Docker setup
- Community forum discussions on Keycloak redirect loops and logout issues - Indicates common problems but solutions vary by configuration

## Metadata

**Confidence breakdown:**
- Standard stack: **HIGH** - Microsoft official packages with clear documentation and production usage
- Architecture patterns: **HIGH** - Patterns verified from Microsoft Learn and established community practices (Auth0, Milan Jovanović)
- Common pitfalls: **HIGH** - Documented in Microsoft Learn performance guides and confirmed by community experiences
- Keycloak OIDC integration: **MEDIUM** - Standard OIDC flow is well-known, but Keycloak-specific token claim mapping may require trial-and-error
- Testing infrastructure: **HIGH** - xUnit + bUnit is standard .NET testing approach with official documentation

**Research date:** 2026-02-26
**Valid until:** 2026-05-26 (90 days - stack is mature and stable)

**Framework versions locked for Phase 1:**
- .NET 8.0 (LTS until November 2026)
- ASP.NET Core 8.0
- EF Core 9.0 (STS until November 2026)

**Note:** EF Core 10 with .NET 9 available but not recommended for Phase 1 (bleeding edge). Stick to .NET 8 LTS for production stability.
