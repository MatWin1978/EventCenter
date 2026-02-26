# Architecture Research

**Domain:** Event Management System (Veranstaltungscenter)
**Researched:** 2026-02-26
**Confidence:** HIGH

## Standard Architecture for Blazor Server Event Management

### System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER (Blazor Server)            │
├─────────────────────────────────────────────────────────────────┤
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌──────────┐  │
│  │Admin Pages │  │Member Pages│  │Company     │  │ Shared   │  │
│  │(Auth:      │  │(Auth:      │  │Pages       │  │Components│  │
│  │ Keycloak)  │  │ Keycloak)  │  │(Anonymous) │  │          │  │
│  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘  └─────┬────┘  │
│        │                │                │                │       │
│        └────────────────┴────────────────┴────────────────┘       │
│                                │                                  │
├────────────────────────────────┴──────────────────────────────────┤
│                       APPLICATION LAYER                           │
├──────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                Service Layer (Business Logic)               │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │ │
│  │  │Event Mgmt│  │Booking   │  │Company   │  │Member    │   │ │
│  │  │Service   │  │Service   │  │Service   │  │Service   │   │ │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │ │
│  └─────────────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────────────┤
│                      INFRASTRUCTURE LAYER                         │
├──────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐  ┌──────────────────┐                  │
│  │ Data Access Layer   │  │ External Services│                  │
│  │  (EF Core DbContext)│  │  • Keycloak OIDC │                  │
│  │  • Repositories     │  │  • Email Service │                  │
│  │  • UnitOfWork       │  │  • Calendar      │                  │
│  └──────────┬──────────┘  └──────────────────┘                  │
│             │                                                     │
├─────────────┴─────────────────────────────────────────────────────┤
│                        DATA LAYER                                 │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │                    SQL Server Database                    │    │
│  │  • Events & Webinars    • Agenda Items                   │    │
│  │  • Registrations        • Companies                      │    │
│  │  • Extra Options        • Participants                   │    │
│  └──────────────────────────────────────────────────────────┘    │
└───────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| **Blazor Pages** | UI rendering, user input, state display | `.razor` files with @page directive, component parameters |
| **Blazor Components** | Reusable UI elements, form controls | `.razor` components without @page, emit events to parents |
| **Service Layer** | Business logic, validation, orchestration | Scoped services injected into pages/components |
| **Repositories** | Data access abstraction, query encapsulation | Generic Repository pattern with EF Core |
| **DbContext** | Entity Framework Core context, database mapping | Entities mapped to SQL Server tables |
| **Domain Entities** | Business objects with behavior | C# classes with properties, methods for state transitions |
| **Authentication** | Keycloak OIDC integration, role-based access | ASP.NET Core Authentication middleware |

## Recommended Project Structure

```
EventCenter/
├── EventCenter.Web/                    # Blazor Server Application
│   ├── Program.cs                      # App startup, service configuration
│   ├── Pages/                          # Blazor pages (@page directive)
│   │   ├── Admin/                      # Admin area (event management)
│   │   │   ├── Events/
│   │   │   │   ├── CreateEvent.razor
│   │   │   │   ├── EditEvent.razor
│   │   │   │   └── EventList.razor
│   │   │   ├── Companies/
│   │   │   └── Participants/
│   │   ├── Member/                     # Member area (event registration)
│   │   │   ├── EventOverview.razor
│   │   │   ├── EventDetails.razor
│   │   │   └── MyRegistrations.razor
│   │   └── Company/                    # Anonymous company portal
│   │       └── CompanyBooking.razor
│   ├── Components/                     # Shared Blazor components
│   │   ├── EventCard.razor
│   │   ├── AgendaItemSelector.razor
│   │   ├── RegistrationForm.razor
│   │   └── Shared/
│   │       ├── MainLayout.razor
│   │       └── NavMenu.razor
│   └── wwwroot/                        # Static files, CSS, JS
│
├── EventCenter.Application/            # Application Layer
│   ├── Services/                       # Business logic services
│   │   ├── EventManagementService.cs   # Event CRUD, publication
│   │   ├── BookingService.cs           # Member/guest registration
│   │   ├── CompanyService.cs           # Company invitations, bookings
│   │   └── ValidationService.cs        # Business rule validation
│   ├── Interfaces/                     # Service contracts
│   │   └── IEventManagementService.cs
│   ├── DTOs/                           # Data Transfer Objects
│   │   ├── EventDto.cs
│   │   ├── RegistrationDto.cs
│   │   └── CompanyBookingDto.cs
│   └── Validators/                     # FluentValidation rules
│       └── EventValidator.cs
│
├── EventCenter.Domain/                 # Domain Layer
│   ├── Entities/                       # Domain entities
│   │   ├── Event.cs
│   │   ├── WebinarEvent.cs
│   │   ├── EventAgendaItem.cs
│   │   ├── EventCompany.cs
│   │   ├── EventRegistration.cs
│   │   ├── MemberEventRegistration.cs
│   │   └── GuestEventRegistration.cs
│   ├── Interfaces/                     # Domain contracts
│   │   └── IEvent.cs
│   ├── Enums/                          # Domain enumerations
│   │   ├── PublicationStates.cs
│   │   ├── EventState.cs
│   │   └── EventType.cs
│   └── Exceptions/                     # Domain-specific exceptions
│       └── RegistrationDeadlineException.cs
│
├── EventCenter.Infrastructure/         # Infrastructure Layer
│   ├── Data/                           # EF Core implementation
│   │   ├── EventCenterDbContext.cs     # DbContext
│   │   ├── Configurations/             # Fluent API entity configs
│   │   │   ├── EventConfiguration.cs
│   │   │   └── RegistrationConfiguration.cs
│   │   └── Migrations/                 # EF Core migrations
│   ├── Repositories/                   # Repository implementations
│   │   ├── EventRepository.cs
│   │   ├── RegistrationRepository.cs
│   │   └── CompanyRepository.cs
│   ├── Identity/                       # Keycloak integration
│   │   ├── KeycloakAuthenticationSetup.cs
│   │   └── RoleAuthorizationHandler.cs
│   └── Services/                       # External service adapters
│       ├── EmailService.cs
│       └── CalendarService.cs
│
└── EventCenter.Tests/                  # Test projects
    ├── UnitTests/
    ├── IntegrationTests/
    └── FunctionalTests/
```

### Structure Rationale

- **EventCenter.Web/:** Presentation layer contains only UI concerns - Blazor pages, components, and static assets. No business logic.
- **EventCenter.Application/:** Houses business logic in services, keeping it testable and independent of UI framework. DTOs prevent domain entities from leaking into UI.
- **EventCenter.Domain/:** Pure domain model with entities, interfaces, and enums. No dependencies on infrastructure or UI.
- **EventCenter.Infrastructure/:** All infrastructure concerns - EF Core, repositories, external service integrations. Implements interfaces from Application/Domain layers.
- **Separation by user role:** Pages organized by Admin/Member/Company aligns with three distinct user journeys and authorization boundaries.

## Architectural Patterns

### Pattern 1: Clean Architecture with Dependency Inversion

**What:** Layers depend on abstractions, not concrete implementations. Inner layers (Domain) have no knowledge of outer layers (Infrastructure, Presentation).

**When to use:** Enterprise applications requiring maintainability, testability, and flexibility to swap infrastructure.

**Trade-offs:**
- **Pros:** Testable business logic, swappable infrastructure, clear boundaries
- **Cons:** More files/projects, learning curve for team, potential over-engineering for simple CRUD

**Example:**
```csharp
// Application layer defines interface
public interface IEventRepository
{
    Task<Event> GetByIdAsync(Guid id);
    Task<IEnumerable<Event>> GetPublishedEventsAsync();
    Task AddAsync(Event evt);
}

// Infrastructure implements it
public class EventRepository : IEventRepository
{
    private readonly EventCenterDbContext _context;

    public async Task<Event> GetByIdAsync(Guid id)
        => await _context.Events.FindAsync(id);
}

// Application service depends on abstraction
public class EventManagementService
{
    private readonly IEventRepository _eventRepository;

    public EventManagementService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }
}
```

### Pattern 2: Repository Pattern with Unit of Work

**What:** Repositories abstract data access, Unit of Work coordinates changes across multiple repositories within a transaction.

**When to use:** When you need transaction boundaries across multiple entities, or want to abstract away EF Core specifics.

**Trade-offs:**
- **Pros:** Testable data access, transaction management, can swap ORM
- **Cons:** Additional abstraction layer, debates about whether it's needed with EF Core

**Example:**
```csharp
// Unit of Work coordinates transaction
public interface IUnitOfWork : IDisposable
{
    IEventRepository Events { get; }
    IRegistrationRepository Registrations { get; }
    Task<int> SaveChangesAsync();
}

// Service uses UoW for atomic operations
public class BookingService
{
    private readonly IUnitOfWork _unitOfWork;

    public async Task RegisterMemberAsync(Guid eventId, int memberId, List<Guid> agendaItemIds)
    {
        var evt = await _unitOfWork.Events.GetByIdAsync(eventId);

        // Business rule validation
        if (evt.RegistrationCount >= evt.ParticipantsLimit)
            throw new EventFullException();

        var registration = new MemberEventRegistration
        {
            EventId = eventId,
            MemberId = memberId,
            RegisteredAt = DateTime.UtcNow
        };

        await _unitOfWork.Registrations.AddAsync(registration);
        evt.RegistrationCount++;

        // Atomic commit
        await _unitOfWork.SaveChangesAsync();
    }
}
```

### Pattern 3: Service Layer Pattern

**What:** Business logic encapsulated in service classes, orchestrating operations across repositories and enforcing business rules.

**When to use:** Always for non-trivial business logic. Keeps Blazor pages/components thin.

**Trade-offs:**
- **Pros:** Testable logic, reusable across different UI layers, clear separation of concerns
- **Cons:** Can become anemic if entities have rich behavior (prefer Domain-Driven Design in that case)

**Example:**
```csharp
public class CompanyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public async Task<CompanyBookingResult> SubmitBookingAsync(
        Guid companyGuid,
        List<CompanyParticipantDto> participants)
    {
        var company = await _unitOfWork.Companies.GetByGuidAsync(companyGuid);

        // Business rules
        if (company.IsBooked)
            return CompanyBookingResult.AlreadyBooked();

        if (participants.Count > company.NumberMaxParticipants)
            return CompanyBookingResult.TooManyParticipants();

        // Update aggregate
        company.IsBooked = true;
        company.BookedOn = DateTime.UtcNow;

        foreach (var dto in participants)
        {
            company.Participants.Add(new EventCompanyParticipant
            {
                Firstname = dto.Firstname,
                Lastname = dto.Lastname,
                Email = dto.Email,
                RegisteredAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();

        // Side effect
        await _emailService.SendBookingConfirmationAsync(company);

        return CompanyBookingResult.Success();
    }
}
```

### Pattern 4: CQRS (Command Query Responsibility Segregation) - OPTIONAL

**What:** Separate read models from write models. Commands modify state, Queries return data.

**When to use:** When read/write patterns differ significantly, or you need optimized read models.

**Trade-offs:**
- **Pros:** Optimized queries (raw SQL/Dapper), scalable reads, clear intent
- **Cons:** Additional complexity, potential data synchronization challenges

**Note:** For this event management system, CQRS is likely **overkill** unless you anticipate very high read traffic or complex reporting needs. Start with standard service layer and add CQRS later if needed.

**Example (if implemented):**
```csharp
// Command side - uses EF Core for writes
public record CreateEventCommand(string Title, DateTime Start, DateTime End);

public class CreateEventHandler
{
    private readonly EventCenterDbContext _context;

    public async Task<Guid> Handle(CreateEventCommand command)
    {
        var evt = new Event
        {
            BusinessId = Guid.NewGuid(),
            Title = command.Title,
            Start = command.Start,
            End = command.End,
            PublicationState = PublicationStates.NotPublic
        };

        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        return evt.BusinessId;
    }
}

// Query side - uses Dapper for optimized reads
public record EventListQuery(int Page, int PageSize);

public class EventListQueryHandler
{
    private readonly IDbConnection _connection;

    public async Task<List<EventListDto>> Handle(EventListQuery query)
    {
        var sql = @"
            SELECT e.BusinessId, e.Title, e.Start, e.RegistrationDeadline,
                   COUNT(r.Id) as RegistrationCount
            FROM tblEvents e
            LEFT JOIN tblEventMemberRegistrations r ON e.Id = r.EventId
            WHERE e.PublicationState = 1
            GROUP BY e.BusinessId, e.Title, e.Start, e.RegistrationDeadline
            ORDER BY e.Start
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        return await _connection.QueryAsync<EventListDto>(sql, new
        {
            Offset = query.Page * query.PageSize,
            PageSize = query.PageSize
        });
    }
}
```

### Pattern 5: Aggregate Root Pattern (Domain-Driven Design)

**What:** Event is an aggregate root that controls access to its child entities (AgendaItems, Companies, Registrations). All modifications go through the Event entity.

**When to use:** When entities have complex invariants and business rules that must be enforced.

**Trade-offs:**
- **Pros:** Enforces business rules, clear transaction boundaries, encapsulates complexity
- **Cons:** Can make queries more complex, requires discipline to maintain boundaries

**Example:**
```csharp
public class Event : IEvent
{
    private readonly List<EventAgendaItem> _agendaItems = new();
    private readonly List<EventRegistration> _registrations = new();

    public IReadOnlyCollection<EventAgendaItem> AgendaItems => _agendaItems.AsReadOnly();
    public IReadOnlyCollection<EventRegistration> Registrations => _registrations.AsReadOnly();

    // Business logic encapsulated in entity
    public void Publish()
    {
        if (Start <= DateTime.UtcNow)
            throw new InvalidOperationException("Cannot publish event in the past");

        if (_agendaItems.Count == 0)
            throw new InvalidOperationException("Cannot publish event without agenda items");

        PublicationState = PublicationStates.Public;
    }

    public void AddAgendaItem(string name, decimal membersCost, decimal companionCost)
    {
        var nextNumber = _agendaItems.Any()
            ? _agendaItems.Max(a => a.EventAgendaItemNumber) + 1
            : 1;

        _agendaItems.Add(new EventAgendaItem
        {
            Name = name,
            EventAgendaItemNumber = nextNumber,
            MembersParticipationCost = membersCost,
            CompanionParticipationCost = companionCost
        });
    }

    public bool CanRegisterMember(int memberId)
    {
        if (DateTime.UtcNow > RegistrationDeadline)
            return false;

        if (RegistrationCount >= ParticipantsLimit)
            return false;

        // One registration per member
        if (_registrations.OfType<MemberEventRegistration>().Any(r => r.MemberId == memberId))
            return false;

        return true;
    }
}
```

## Data Flow

### Request Flow - Member Registration Example

```
[Member clicks "Register" button]
    ↓
[EventDetails.razor page] → @onclick="HandleRegisterAsync"
    ↓
[Inject IBookingService] → await BookingService.RegisterMemberAsync(eventId, memberId, agendaItemIds)
    ↓
[BookingService validates] → Business rules checked
    ↓
[Service calls Repository] → await _unitOfWork.Registrations.AddAsync(registration)
    ↓
[UnitOfWork commits] → await _unitOfWork.SaveChangesAsync()
    ↓
[EF Core SaveChanges] → SQL INSERT into tblEventMemberRegistrations
    ↓
[Service returns Result] → Success/Failure DTO
    ↓
[Page updates UI] → StateHasChanged(), show success message
```

### Request Flow - Admin Updates Event

```
[Admin edits event form]
    ↓
[EditEvent.razor] → @onsubmit="HandleSubmitAsync"
    ↓
[Validate form with FluentValidation]
    ↓
[Inject IEventManagementService] → await EventManagementService.UpdateEventAsync(eventId, dto)
    ↓
[Service loads entity] → var evt = await _unitOfWork.Events.GetByIdAsync(eventId)
    ↓
[Service applies changes] → evt.Title = dto.Title; evt.Start = dto.Start; ...
    ↓
[Business rule check] → if (evt.Registrations.Any()) { /* warning logic */ }
    ↓
[UnitOfWork commits] → await _unitOfWork.SaveChangesAsync()
    ↓
[EF Core SaveChanges] → SQL UPDATE tblEvents
    ↓
[Page refreshes] → NavigationManager.Refresh(), show success toast
```

### State Management in Blazor Server

```
[User logs in via Keycloak]
    ↓
[Circuit established] → SignalR connection to server
    ↓
[Scoped services created] → Per-user service instances
    ↓
[Component renders] → @inject IEventManagementService EventService
    ↓
[OnInitializedAsync] → await EventService.GetEventsAsync()
    ↓
[Service calls Repository] → Data from SQL Server
    ↓
[Component state updated] → private List<EventDto> _events;
    ↓
[UI updates automatically] → Blazor re-renders component
    ↓
[User navigates] → State persists in circuit memory
    ↓
[Circuit ends] → Browser tab closed, services disposed
```

### Authentication Flow with Keycloak

```
[User navigates to protected page]
    ↓
[ASP.NET Core Auth Middleware] → Check [Authorize] attribute
    ↓
[No valid token] → Redirect to Keycloak login
    ↓
[User authenticates at Keycloak]
    ↓
[Keycloak redirects back] → Authorization code in URL
    ↓
[OIDC middleware exchanges code] → Get access token + refresh token
    ↓
[Token validated & claims extracted] → User identity established
    ↓
[Circuit created with user context] → ClaimsPrincipal available
    ↓
[Blazor component accesses user] → @inject AuthenticationStateProvider
    ↓
[Role-based authorization] → [Authorize(Roles = "Admin")]
```

### Key Data Flows

1. **Event Publication Flow:** Admin creates event → Sets agenda items → Validates completeness → Publishes → Event visible to members
2. **Member Registration Flow:** Member views event list → Opens event details → Selects agenda items → Submits registration → Receives confirmation
3. **Company Booking Flow:** Admin invites company → Company receives GUID link → Opens anonymous portal → Enters participants → Submits booking
4. **Real-time Updates:** State changes trigger SignalR push → All connected circuits receive update → Components re-render automatically

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| **0-1k users** | Monolithic Blazor Server app is ideal. Single SQL Server instance. Circuit-scoped state. No caching needed. |
| **1k-10k users** | Add Redis for distributed caching (event lists, member lookups). Implement database connection pooling. Consider read replicas for reporting. Optimize SignalR with Azure SignalR Service if hosted in cloud. |
| **10k-100k users** | Separate read/write databases (CQRS). Add CDN for static assets. Implement background jobs with Hangfire for email sending. Use Azure SignalR Service (scales to 100k concurrent). Load balancer with sticky sessions. |
| **100k+ users** | Microservices architecture (Event Service, Booking Service, Company Service). Event-driven with message queue (Azure Service Bus, RabbitMQ). Separate Blazor Server frontend from API backend. Consider Blazor WebAssembly for member portal (offload interactivity). |

### Scaling Priorities

1. **First bottleneck:** Database queries (solved with indexes, caching, read replicas)
2. **Second bottleneck:** SignalR circuit memory (solved with Azure SignalR Service, optimize state)
3. **Third bottleneck:** Concurrent writes to registration table (solved with optimistic concurrency, queue-based processing)

### Realistic Assessment for This Project

Based on a broker portal event management system:
- **Expected concurrent users:** 50-500 (realistic for broker community)
- **Peak load:** Event registration opens, 100-200 concurrent registrations
- **Recommended architecture:** Standard Blazor Server monolith with SQL Server
- **Do NOT over-engineer:** CQRS, microservices, message queues are premature optimization

## Anti-Patterns

### Anti-Pattern 1: Business Logic in Blazor Components

**What people do:** Put validation, business rules, and data access directly in `.razor` files.

```csharp
// BAD - EventDetails.razor
@code {
    private async Task RegisterAsync()
    {
        // Business logic in UI layer!
        var registration = _context.Registrations
            .Where(r => r.EventId == EventId && r.MemberId == CurrentMemberId)
            .FirstOrDefault();

        if (registration != null)
        {
            ErrorMessage = "Already registered";
            return;
        }

        if (Event.RegistrationCount >= Event.ParticipantsLimit)
        {
            ErrorMessage = "Event is full";
            return;
        }

        _context.Registrations.Add(new MemberEventRegistration { ... });
        await _context.SaveChangesAsync();
    }
}
```

**Why it's wrong:** Untestable, duplicated across pages, hard to maintain, mixes concerns.

**Do this instead:** Inject service with business logic.

```csharp
// GOOD - EventDetails.razor
@inject IBookingService BookingService

@code {
    private async Task RegisterAsync()
    {
        var result = await BookingService.RegisterMemberAsync(EventId, CurrentMemberId, SelectedAgendaItemIds);

        if (result.Success)
            await ShowSuccessMessageAsync();
        else
            ErrorMessage = result.ErrorMessage;
    }
}
```

### Anti-Pattern 2: Directly Exposing Domain Entities to UI

**What people do:** Pass `Event` entity directly to Blazor components, allow direct mutation.

**Why it's wrong:** UI concerns leak into domain model, EF change tracking issues, security risks (over-posting), breaks encapsulation.

**Do this instead:** Use DTOs for data transfer.

```csharp
// GOOD - Service returns DTO
public async Task<EventDetailsDto> GetEventDetailsAsync(Guid eventId)
{
    var evt = await _unitOfWork.Events.GetByIdAsync(eventId);

    return new EventDetailsDto
    {
        BusinessId = evt.BusinessId,
        Title = evt.Title,
        Start = evt.Start,
        // Map only what UI needs
    };
}
```

### Anti-Pattern 3: N+1 Query Problem

**What people do:** Lazy loading in Blazor Server leads to many round-trips.

```csharp
// BAD - Lazy loading triggers N+1 queries
var events = await _context.Events.ToListAsync();

foreach (var evt in events)
{
    // Lazy load triggers query per event!
    var registrationCount = evt.Registrations.Count;
}
```

**Why it's wrong:** Kills performance, multiplies database round-trips, slow UI rendering.

**Do this instead:** Use eager loading or projections.

```csharp
// GOOD - Single query with Include
var events = await _context.Events
    .Include(e => e.Registrations)
    .ToListAsync();

// BETTER - Projection for exactly what you need
var events = await _context.Events
    .Select(e => new EventListDto
    {
        BusinessId = e.BusinessId,
        Title = e.Title,
        RegistrationCount = e.Registrations.Count
    })
    .ToListAsync();
```

### Anti-Pattern 4: Singleton Services with Circuit State

**What people do:** Register services as Singleton and store user-specific state.

**Why it's wrong:** State leaks between users, concurrency issues, security vulnerabilities.

**Do this instead:** Use Scoped services for user-specific state.

```csharp
// GOOD - Program.cs
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IEventManagementService, EventManagementService>();

// Circuit-scoped = per-user instance
```

### Anti-Pattern 5: Ignoring Concurrency in Registrations

**What people do:** No optimistic concurrency control, last-write-wins.

**Why it's wrong:** Double-booking, registration count drift, exceeding participant limits.

**Do this instead:** Use EF Core concurrency tokens.

```csharp
public class Event
{
    [Timestamp]
    public byte[] RowVersion { get; set; }

    public int RegistrationCount { get; set; }
}

// Service handles concurrency
public async Task RegisterMemberAsync(Guid eventId, int memberId)
{
    try
    {
        var evt = await _unitOfWork.Events.GetByIdAsync(eventId);

        if (!evt.CanRegisterMember(memberId))
            throw new RegistrationException("Cannot register");

        evt.RegistrationCount++;
        await _unitOfWork.Registrations.AddAsync(new MemberEventRegistration { ... });
        await _unitOfWork.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        // Reload and retry, or return error
        throw new RegistrationException("Event was modified by another user. Please try again.");
    }
}
```

### Anti-Pattern 6: Not Using Authorization Attributes

**What people do:** Manually check roles in components/services.

**Why it's wrong:** Easy to forget checks, security gaps, not declarative.

**Do this instead:** Use `[Authorize]` attributes.

```csharp
// GOOD - Declarative authorization
@page "/admin/events"
@attribute [Authorize(Roles = "Admin")]

// Service layer can also use it
[Authorize(Roles = "Admin")]
public class EventManagementService
{
    // Only admins can call these methods
}
```

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| **Keycloak (OIDC)** | ASP.NET Core Authentication Middleware | AddAuthentication().AddOpenIdConnect() with Keycloak realm endpoints. Token validation, role mapping from claims. |
| **SQL Server** | Entity Framework Core | DbContext with connection string from appsettings.json. Use migrations for schema updates. |
| **Email Service** | SMTP or SendGrid API | Abstracted behind IEmailService interface. Send booking confirmations, admin notifications. |
| **iCalendar Export** | ICS file generation | Generate .ics files for event dates. Standard iCalendar format for Outlook/Google Calendar. |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| **Presentation ↔ Application** | Direct method calls via DI | Blazor pages/components inject services. Services return DTOs. |
| **Application ↔ Domain** | Domain entities, business logic | Services orchestrate domain entities, enforce rules via entity methods. |
| **Application ↔ Infrastructure** | Repository interfaces | Services depend on IRepository abstractions, implemented in Infrastructure. |
| **Blazor Components ↔ Pages** | EventCallback, Parameters | Parent pages pass data down via parameters, receive events up via EventCallback. |

### Keycloak Integration Details

```csharp
// Program.cs - Keycloak OIDC setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"]; // https://keycloak.example.com/realms/broker-portal
    options.ClientId = builder.Configuration["Keycloak:ClientId"];
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;

    // Map Keycloak roles to claims
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "role"
    };
});

builder.Services.AddAuthorization();
```

### Entity Framework Core Configuration

```csharp
// EventCenterDbContext.cs
public class EventCenterDbContext : DbContext
{
    public DbSet<Event> Events { get; set; }
    public DbSet<WebinarEvent> WebinarEvents { get; set; }
    public DbSet<EventAgendaItem> AgendaItems { get; set; }
    public DbSet<MemberEventRegistration> MemberRegistrations { get; set; }
    public DbSet<GuestEventRegistration> GuestRegistrations { get; set; }
    public DbSet<EventCompany> Companies { get; set; }
    public DbSet<EventCompanyParticipant> CompanyParticipants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventCenterDbContext).Assembly);
    }
}

// EventConfiguration.cs - Fluent API
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("tblEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.BusinessId).IsRequired();
        builder.HasIndex(e => e.BusinessId).IsUnique();

        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.RowVersion).IsRowVersion();

        // Relationships
        builder.HasMany(e => e.AgendaItems)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Registrations)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete registrations
    }
}
```

## Build Order Recommendations

Based on dependencies and user value, suggested build order:

### Phase 1: Foundation (Core Infrastructure)
**Build:** Domain entities, DbContext, migrations, authentication setup
**Why first:** Everything depends on this
**Deliverable:** Database schema, Keycloak integration working
**Validation:** Can authenticate, database tables created

### Phase 2: Admin - Event Management
**Build:** Event CRUD (Create, Edit, Publish), Agenda item management, Basic event list
**Why second:** Admin must create events before anyone can register
**Deliverable:** Admin can create and publish events
**Validation:** Published event visible in database
**Dependencies:** Phase 1

### Phase 3: Member - Event Registration
**Build:** Event list for members, Event details view, Member self-registration with agenda selection
**Why third:** Core value - members registering for events
**Deliverable:** Members can browse and register for events
**Validation:** Registration appears in database, registration count increments
**Dependencies:** Phase 1, Phase 2 (needs published events)

### Phase 4: Admin - Company Invitations
**Build:** Company invitation management, Special agenda item pricing, GUID link generation
**Why fourth:** Prepares for company bookings
**Deliverable:** Admin can invite companies with special pricing
**Validation:** Company record created with GUID
**Dependencies:** Phase 1, Phase 2

### Phase 5: Company - Anonymous Booking Portal
**Build:** Company booking page (anonymous access via GUID), Participant entry form, Booking submission
**Why fifth:** Enables company participation
**Deliverable:** Company representatives can book via link
**Validation:** Company participants recorded, booking status updated
**Dependencies:** Phase 1, Phase 2, Phase 4

### Phase 6: Member - Guest Registration
**Build:** Guest registration form, Companion limit enforcement, Guest listing
**Why sixth:** Adds member value, not critical path
**Deliverable:** Members can register guests
**Validation:** Guest registrations appear alongside member registrations
**Dependencies:** Phase 1, Phase 3

### Phase 7: Registration Management
**Build:** Member cancellation, Guest cancellation, Company cancellation/non-participation, Cancellation notifications
**Why seventh:** Important but not MVP
**Deliverable:** All user types can cancel/modify registrations
**Validation:** Cancellation updates registration count, state transitions correctly
**Dependencies:** Phase 3, Phase 5, Phase 6

### Phase 8: Admin - Reporting & Export
**Build:** Participant list views, Excel export, Email notifications, iCalendar export
**Why eighth:** Admin convenience features
**Deliverable:** Admin can export participant data
**Validation:** Excel file downloads with correct data
**Dependencies:** Phase 1, Phase 2, Phase 3

### Phase 9: Webinar Support
**Build:** Webinar entity CRUD, External registration link handling, Webinar-specific UI
**Why ninth:** Feature parity with physical events, lower priority
**Deliverable:** Webinars can be created and managed
**Validation:** Webinar redirects to external registration link
**Dependencies:** Phase 1, Phase 2

### Phase 10: Polish & Optimization
**Build:** Performance tuning (caching, query optimization), Enhanced validation messages, Search/filter improvements, UI/UX refinements
**Why last:** Incremental improvements after core functionality works
**Deliverable:** Polished user experience
**Dependencies:** All previous phases

### Critical Path
**Must build in order:** Phase 1 → Phase 2 → Phase 3 (MVP achieved here)
**Parallel opportunities:** Phase 4-5 (Company) can be built in parallel with Phase 6-7 (Members) if team capacity allows
**Can defer:** Phase 8-10 can be deferred post-launch

## Sources

- [Building Blazor Server Apps with Clean Architecture](https://www.ezzylearning.net/tutorial/building-blazor-server-apps-with-clean-architecture)
- [ASP.NET Core Blazor | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/blazor/?view=aspnetcore-10.0)
- [Architectural Patterns in Blazor - Inspeerity](https://inspeerity.com/blog/architectural-patterns-in-blazor/)
- [Real-Time Web by leveraging Event Driven Architecture - CodeOpinion](https://codeopinion.com/real-time-web-by-leveraging-event-driven-architecture/)
- [Emerging Trends in Blazor Development for 2026 - Medium](https://medium.com/@reenbit/emerging-trends-in-blazor-development-for-2026-70d6a52e3d2a)
- [Blazor Best Practices for Architecture and Performance - Devart](https://blog.devart.com/asp-net-core-blazor-best-practices-architecture-and-performance-optimization.html)
- [GitHub: CleanArchitectureWithBlazorServer](https://github.com/neozhu/CleanArchitectureWithBlazorServer)
- [Project structure for Blazor apps - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/architecture/blazor-for-web-forms-developers/project-structure)
- [CQRS and MediatR in ASP.NET Core - CodeWithMukesh](https://codewithmukesh.com/blog/cqrs-and-mediatr-in-aspnet-core/)
- [GitHub: Sample .NET Core CQRS API](https://github.com/kgrzybek/sample-dotnet-core-cqrs-api)
- [Repository Pattern C# Ultimate Guide - Medium](https://medium.com/@codebob75/repository-pattern-c-ultimate-guide-entity-framework-core-clean-architecture-dtos-dependency-6a8d8b444dcb)
- [Secure ASP.NET Core Blazor Web App with OIDC - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-9.0)
- [GitHub: BlazorServerKeycloak](https://github.com/mattj23/BlazorServerKeycloak)
- [Keycloak Tutorial for .NET Developers - Julio Casal](https://juliocasal.com/blog/keycloak-tutorial-for-net-developers)
- [Local development with Keycloak and Blazor Server - Medium](https://medium.com/norsk-helsenett/local-development-with-keycloak-and-blazor-server-695921705578)
- [How to Design a Database for Event Management - GeeksforGeeks](https://www.geeksforgeeks.org/dbms/how-to-design-a-database-for-event-management/)
- [Building an Event Management System - Medium](https://medium.com/@tatibaevmurod/building-an-event-management-system-designing-the-blueprint-crafting-the-schema-and-executing-43ad2e45568e)
- [How to Design ER Diagrams for Online Ticketing and Event Management - GeeksforGeeks](https://www.geeksforgeeks.org/dbms/how-to-design-er-diagrams-for-online-ticketing-and-event-management/)
- [ASP.NET Core Blazor state management - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management/?view=aspnetcore-10.0)
- [Blazor State Management Best Practices - Infragistics](https://www.infragistics.com/blogs/blazor-state-management)
- [3+1 ways to manage state in your Blazor application - Jon Hilton](https://jonhilton.net/blazor-state-management/)

---
*Architecture research for: Event Management System (Veranstaltungscenter)*
*Researched: 2026-02-26*
