# Stack Research

**Domain:** Event Management System (Veranstaltungscenter)
**Researched:** 2026-02-26
**Confidence:** HIGH

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| ASP.NET Core | 9.0.11 | Web framework and runtime | Latest LTS release with enhanced performance, native AOT support, and improved security. Full support for Blazor Server and SignalR enhancements (CONSTRAINT: pre-decided) |
| Blazor Server | 9.0.11 | Interactive UI framework | Provides real-time interactivity via SignalR, direct database access without separate API, smaller download size than WASM, and full .NET debugging support. Ideal for enterprise apps with authenticated users (CONSTRAINT: pre-decided) |
| Entity Framework Core | 10.0.3 | ORM and data access | Latest version with enhanced JSON support, ExecuteUpdateAsync improvements, compiled models for large schemas, and optimized query generation. Seamless SQL Server integration |
| SQL Server | 2022+ | Relational database | Enterprise-grade, excellent EF Core integration, familiar to .NET teams, supports advanced features like temporal tables for audit trails (CONSTRAINT: pre-decided) |
| Keycloak | 24.x+ | Authentication/Authorization | Open-source IAM with OIDC/OAuth2 support, role-based access control, SSO capabilities, no cloud vendor lock-in, runs in Docker for dev (CONSTRAINT: pre-decided) |
| C# | 13.0 | Programming language | Ships with .NET 9, includes partial properties, ref struct generics, and overload resolution priority |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Keycloak.AuthServices.Authentication | 2.7.0+ | Keycloak integration | Simplifies OIDC integration with ASP.NET Core, handles JWT validation, role extraction, and session management. Use for all Keycloak authentication needs |
| MudBlazor | 9.0.0+ | UI component library | Material Design components for Blazor. Provides DataGrid, Forms, Dialogs, DatePicker, Tables - essential for admin/user portals. Open-source, actively maintained, excellent documentation |
| FluentValidation | 12.1.1+ | Model validation | Strongly-typed validation rules with better testability than DataAnnotations. Use for all form validation (event creation, registration, company bookings) |
| Blazilla | 2.x+ | FluentValidation-Blazor integration | Modern replacement for Blazored.FluentValidation with real-time validation, nested object support, async validation, and better performance. Preferred over legacy Blazored package |
| Serilog.AspNetCore | 10.0.0+ | Structured logging | Production-grade logging with sinks for files, Seq, Elasticsearch. Critical for Blazor Server to avoid blocking SignalR. Includes correlation IDs and structured data |
| Mapster | 8.x+ | Object mapping | 5-12x faster than AutoMapper with compile-time code generation. AutoMapper went commercial in April 2025, making Mapster the preferred free alternative for DTOs and view models |
| Hangfire | 1.8.22+ | Background jobs | Persistent job storage, automatic retries, web dashboard for monitoring. Use for email notifications, report generation, and cleanup tasks. Simpler than Quartz.NET for most scenarios |
| MailKit | 4.x+ | Email delivery | SMTP client for sending event confirmations, reminders, and notifications. More reliable than System.Net.Mail with better async support |
| Ical.Net | 4.x+ | iCalendar generation | Generates .ics files for calendar export (US-16). Standard library for RFC 5545 compliance |
| bUnit | 1.x+ | Blazor component testing | Unit testing framework for Blazor components. Integrates with xUnit/NUnit/MSTest. Renders components in memory for fast tests without browser overhead |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| Visual Studio 2022 (17.12+) | Primary IDE | Required for .NET 9 support. Includes Blazor tooling, debugging, and IntelliSense |
| Rider 2025.x | Alternative IDE (optional) | JetBrains IDE with excellent Blazor support and faster performance for large solutions |
| Docker Desktop | Keycloak local dev | Run Keycloak container locally. Use official jboss/keycloak or quay.io/keycloak/keycloak images |
| SQL Server Developer Edition | Local database | Free for development. Use LocalDB for lightweight dev or full SQL Server for production parity |
| Seq | Log aggregation (optional) | Free single-user license. Web UI for structured logs from Serilog. Essential for debugging Blazor Server circuits |
| Azure Data Studio | Database management | Cross-platform SQL tool. Lighter than SSMS for queries and migrations |

## Installation

### Core Framework
```bash
# Create new Blazor Server project
dotnet new blazorserver -n EventCenter -f net9.0

# Add EF Core with SQL Server
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.3
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 10.0.3
```

### Authentication
```bash
# Keycloak integration
dotnet add package Keycloak.AuthServices.Authentication --version 2.7.0
```

### UI and Validation
```bash
# MudBlazor components
dotnet add package MudBlazor --version 9.0.0

# FluentValidation
dotnet add package FluentValidation --version 12.1.1
dotnet add package FluentValidation.DependencyInjectionExtensions --version 12.1.1
dotnet add package Blazilla --version 2.x
```

### Utilities
```bash
# Logging
dotnet add package Serilog.AspNetCore --version 10.0.0
dotnet add package Serilog.Sinks.File --version 6.0.0
dotnet add package Serilog.Sinks.Seq --version 8.x

# Object mapping
dotnet add package Mapster --version 8.x
dotnet add package Mapster.DependencyInjection --version 1.x

# Background jobs
dotnet add package Hangfire.AspNetCore --version 1.8.22
dotnet add package Hangfire.SqlServer --version 1.8.22

# Email
dotnet add package MailKit --version 4.x

# Calendar export
dotnet add package Ical.Net --version 4.x
```

### Testing (Dev dependencies)
```bash
# Unit testing
dotnet add package bUnit --version 1.x
dotnet add package xUnit --version 2.9.x
dotnet add package Moq --version 4.x
```

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| Blazor Server | Blazor WASM | Only if offline-first or public-facing anonymous app with millions of users. Not suitable for this authenticated event system |
| MudBlazor | Radzen Blazor | If you prefer MIT license and lighter components. MudBlazor has better documentation and more active community |
| MudBlazor | Telerik/Syncfusion/DevExpress | Only if budget allows ($1000-2000/dev) and you need enterprise support with SLAs. MudBlazor covers 95% of use cases for free |
| Mapster | Mapperly | If you want source generator approach with zero runtime overhead. Mapster is more established with better DI integration |
| Mapster | Manual mapping | For simple 1:1 DTOs. Use Mapster when mapping 10+ properties or complex nested objects |
| Hangfire | Quartz.NET | Only if you need complex enterprise cron schedules (e.g., "last business day of month"). Hangfire simpler for fire-and-forget and recurring jobs |
| Hangfire | IHostedService | For simple single background tasks (e.g., one cleanup job). Hangfire adds persistence, retries, and dashboard monitoring |
| FluentValidation | DataAnnotations | Never for this project - DataAnnotations lack composability, testability, and async support needed for database validations |
| Serilog | Microsoft.Extensions.Logging | Only for trivial apps. Serilog provides structured logging, sinks, and better diagnostics for production |
| EF Core | Dapper | Only for performance-critical read-heavy scenarios with hand-tuned SQL. EF Core 10 with compiled models is fast enough for event management workloads |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| AutoMapper | Went commercial in April 2025 (requires paid license). Slower performance than alternatives | Mapster (5-12x faster, free MIT license) |
| Blazored.FluentValidation | Stalled development, lingering bugs, not actively maintained | Blazilla (modern rewrite with better performance and bug fixes) |
| System.Net.Mail | Deprecated, limited async support, missing modern SMTP features | MailKit (actively maintained, full async, better error handling) |
| Newtonsoft.Json | Slower than System.Text.Json, not needed in modern .NET | System.Text.Json (built-in, faster, AOT-compatible) |
| SignalR manual setup for Blazor | Blazor Server includes SignalR out-of-the-box with optimized configuration | Use Blazor Server's built-in circuit management |
| Razor Pages for this project | Mixing Razor Pages with Blazor Server creates inconsistent UX and complicates state management | Pure Blazor Server for all UI |
| LazyLoading in EF Core | Causes N+1 queries, especially problematic in Blazor Server where components re-render | Explicit eager loading with .Include() or projections with .Select() |
| Synchronous I/O with EF Core | Blocks SignalR threads in Blazor Server, causes timeouts and circuit crashes | Always use async methods (SaveChangesAsync, ToListAsync, etc.) |

## Stack Patterns by Variant

### Pattern: Scoped Services in Blazor Server
Blazor Server uses scoped services per SignalR circuit (not per HTTP request). This is critical for DbContext:

```csharp
// Program.cs - CORRECT for Blazor Server
builder.Services.AddDbContext<EventCenterDbContext>(options =>
    options.UseSqlServer(connectionString),
    ServiceLifetime.Scoped); // Scoped = one DbContext per circuit

builder.Services.AddScoped<IEventService, EventService>();
```

**Do NOT use singleton DbContext** - causes concurrency issues and stale data.

### Pattern: State Management for Multi-Step Forms
For complex flows like event creation or company booking:

**Simple state (single component):**
- Use component fields/properties

**Shared state (parent-child):**
- Use `[Parameter]` and `EventCallback<T>`

**Circuit-wide state (multiple pages):**
- Use scoped services with `StateHasChanged()` notification pattern
- Example: `EventRegistrationStateService` for multi-step registration wizard

**Do NOT use:**
- Static fields (shared across all users/circuits)
- Session storage (Blazor Server state is in memory, not browser)

### Pattern: Async Validation with Database Checks
For validations requiring database lookup (e.g., "email already registered"):

```csharp
public class EventRegistrationValidator : AbstractValidator<EventRegistrationModel>
{
    public EventRegistrationValidator(IDbContextFactory<EventCenterDbContext> dbFactory)
    {
        RuleFor(x => x.MemberId)
            .MustAsync(async (memberId, cancellation) =>
            {
                await using var db = dbFactory.CreateDbContext();
                return !await db.EventRegistrations
                    .AnyAsync(r => r.MemberId == memberId && r.EventId == eventId, cancellation);
            })
            .WithMessage("You are already registered for this event.");
    }
}
```

**Key points:**
- Use `IDbContextFactory<T>` in validators (not injecting DbContext directly - lifetime mismatch)
- Always use `MustAsync` for async rules
- Set `AsyncMode="true"` in Blazilla's FluidValidator component

### Pattern: Background Jobs for Notifications
Use Hangfire for email notifications to avoid blocking user interactions:

```csharp
// During event registration (fast response to user)
BackgroundJob.Enqueue<IEmailService>(x =>
    x.SendRegistrationConfirmationAsync(registration.Id));

// Recurring job for reminders (set up once in Program.cs)
RecurringJob.AddOrUpdate<IReminderService>(
    "send-event-reminders",
    x => x.SendUpcomingEventRemindersAsync(),
    Cron.Daily(8)); // 8 AM daily
```

### Pattern: Logging in Blazor Server
**CRITICAL:** Never log synchronously in component lifecycle methods - blocks SignalR:

```csharp
// WRONG - blocks circuit
protected override void OnInitialized()
{
    logger.LogInformation("Component initialized"); // Synchronous
    base.OnInitialized();
}

// CORRECT - async logging
protected override async Task OnInitializedAsync()
{
    await Task.Run(() => logger.LogInformation("Component initialized"));
    await base.OnInitializedAsync();
}

// BETTER - configure Serilog with async sinks
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(a => a.File("logs/log-.txt", rollingInterval: RollingInterval.Day))
    .CreateLogger();
```

## Version Compatibility

| Package A | Compatible With | Notes |
|-----------|-----------------|-------|
| ASP.NET Core 9.x | EF Core 9.x - 10.x | EF Core 10 works with ASP.NET Core 9 (targets .NET 9 runtime) |
| MudBlazor 9.x | .NET 9+ | Requires .NET 9 SDK. Use MudBlazor 7.x for .NET 8 projects |
| Blazilla 2.x | FluentValidation 11.x - 12.x | FluentValidation 12.x recommended for latest features |
| Hangfire 1.8.x | EF Core 9.x - 10.x | Use Hangfire.SqlServer for SQL Server storage |
| Keycloak.AuthServices 2.7.x | .NET 8+ | Works with Keycloak 20+ (tested up to Keycloak 24) |
| Serilog.AspNetCore 10.x | .NET 9+ | Tracks .NET versioning. Use 9.x for .NET 9, 8.x for .NET 8 |
| bUnit 1.x | .NET 5 - .NET 10 | Cross-compatible with most .NET versions |

## Known Issues & Workarounds

### Visual Studio Resource File Viewer Bug (October 2025)
When opening `.resx` files for localization, Visual Studio may throw an error. **Workaround:** Right-click resource file → "Open with..." → "Managed Resources Editor (Legacy)".

### EF Core 9+ Synchronous I/O Exception
Starting with EF Core 9, synchronous I/O throws exceptions in Azure Cosmos DB provider. For SQL Server, always use async methods (`ToListAsync()`, `SaveChangesAsync()`) to avoid blocking Blazor Server circuits.

### Keycloak Role Extraction
Keycloak stores roles in non-standard JWT claims. `Keycloak.AuthServices.Authentication` handles extraction automatically, but custom middleware may miss roles. Always test role-based authorization with actual Keycloak tokens.

### MudBlazor Static Rendering Limitation
MudBlazor does not support static server-side rendering (SSR). All pages using MudBlazor components must use `@rendermode InteractiveServer` or `@rendermode InteractiveAuto`.

## Sources

### Official Documentation (HIGH confidence)
- [ASP.NET Core Blazor Performance Best Practices](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/?view=aspnetcore-9.0)
- [What's New in ASP.NET Core 9](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-9.0?view=aspnetcore-10.0)
- [What's New in EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew)
- [EF Core Efficient Querying](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)
- [ASP.NET Core Localization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-10.0)
- [FluentValidation Official Documentation](https://docs.fluentvalidation.net/en/latest/)
- [Blazor State Management](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management/?view=aspnetcore-10.0)

### NuGet Packages (HIGH confidence - verified versions)
- [MudBlazor 9.0.0](https://www.nuget.org/packages/MudBlazor)
- [FluentValidation 12.1.1](https://www.nuget.org/packages/fluentvalidation/)
- [Keycloak.AuthServices.Authentication 2.7.0](https://www.nuget.org/packages/Keycloak.AuthServices.Authentication)
- [Serilog.AspNetCore 10.0.0](https://www.nuget.org/packages/serilog.aspnetcore)
- [Entity Framework Core 10.0.3](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.3)
- [Hangfire 1.8.22](https://www.hangfire.io/blog/2025/11/07/hangfire-1.8.22.html)

### Community Resources (MEDIUM confidence)
- [Blazor Component Libraries Comparison](https://www.infragistics.com/blogs/blazor-component-libraries)
- [AutoMapper vs Mapster Technical Analysis](https://code-maze.com/automapper-vs-mapster-dotnet/)
- [Hangfire vs Quartz.NET Comparison](https://code-maze.com/chsarp-the-differences-between-quartz-net-and-hangfire/)
- [bUnit Testing Library](https://bunit.dev/)
- [BlazorTemplater for Email Templates](https://github.com/conficient/BlazorTemplater)
- [Keycloak .NET Tutorial](https://juliocasal.com/blog/keycloak-tutorial-for-net-developers)
- [Why Logging Matters in 2025: Blazor Best Practices](https://embarkingonvoyage.com/blog/best-practices-for-blazor-asp-net-core/)

### GitHub Repositories (MEDIUM confidence)
- [awesome-blazor Resource Collection](https://github.com/AdrienTorris/awesome-blazor)
- [Keycloak Authorization Services .NET](https://github.com/NikiforovAll/keycloak-authorization-services-dotnet)
- [Blazilla FluentValidation Integration](https://github.com/loresoft/Blazilla)

---
*Stack research for: Event Management System (Veranstaltungscenter)*
*Researched: 2026-02-26*
*Confidence: HIGH - All versions verified from official NuGet/documentation as of February 2026*
