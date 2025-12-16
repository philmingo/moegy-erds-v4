# FullStackHero .NET Starter Kit - Project Documentation

## Overview

This is a production-ready .NET 10 starter kit based on the [FullStackHero dotnet-starter-kit](https://github.com/fullstackhero/dotnet-starter-kit). It provides a modular, multi-tenant SaaS architecture with clean architecture principles and vertical slice architecture.

**Key Benefits:**
- Saves ~200+ development hours with batteries-included features
- Production-first design with cloud-ready infrastructure
- Multi-tenancy support out of the box
- Modern stack: .NET 10, Blazor, PostgreSQL, Redis, Hangfire

---

## Architecture

### Project Structure

```
src/
??? BuildingBlocks/          # Shared infrastructure components
?   ??? Blazor.UI/          # Blazor UI components and theme
?   ??? Caching/            # Distributed caching abstractions (Redis)
?   ??? Core/               # Domain primitives, exceptions, interfaces
?   ??? Eventing/           # Domain events and integration events
?   ??? Jobs/               # Background job processing (Hangfire)
?   ??? Mailing/            # Email service abstractions
?   ??? Persistence/        # EF Core abstractions, specifications
?   ??? Shared/             # Cross-cutting concerns
?   ??? Storage/            # File storage abstractions
?   ??? Web/                # Web host configuration, modules, middleware
??? Modules/                # Feature modules (vertical slices)
?   ??? Identity/           # Authentication, authorization, user management
?   ??? Multitenancy/       # Tenant provisioning, isolation
?   ??? Auditing/           # Security/exception/activity auditing
??? Playground/             # Reference implementation
?   ??? Playground.Api/     # API host
?   ??? Playground.Blazor/  # Blazor WebAssembly client
?   ??? FSH.Playground.AppHost/ # .NET Aspire orchestration
?   ??? Migrations.PostgreSQL/  # Database migrations
??? Tests/
    ??? Architecture.Tests/ # Architecture guardrails
    ??? Multitenacy.Tests/  # Multi-tenancy tests
```

---

## Core Concepts

### 1. Modular Architecture

The project uses a **module-based architecture** where each feature is a self-contained module.

#### Module Structure

Each module consists of:
- **Contracts**: DTOs, interfaces, events (shared)
- **Features**: Use cases organized by version (e.g., v1/Users, v1/Roles)
- **Infrastructure**: Persistence, services, middleware
- **Module Class**: Implements `IModule` interface

**Example: Identity Module**

```csharp
[assembly: FshModule(typeof(IdentityModule), Order = 1)]

public class IdentityModule : IModule
{
    // Register services during startup
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddIdentity<FshUser, FshRole>();
        builder.Services.AddHeroDbContext<IdentityDbContext>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        // ... more services
    }

    // Map HTTP endpoints
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/identity")
            .WithTags("Identity");
        
        group.MapGenerateTokenEndpoint();
        group.MapRefreshTokenEndpoint();
        // ... more endpoints
    }
}
```

### 2. Hero Platform Extensions

The framework provides two main extension methods to wire up the entire platform:

#### `AddHeroPlatform` - Service Registration

```csharp
builder.AddHeroPlatform(o =>
{
    o.EnableCaching = true;      // Redis distributed cache
    o.EnableMailing = true;      // Email services
    o.EnableJobs = true;         // Hangfire background jobs
    o.EnableOpenTelemetry = true; // Observability
});
```

**What it configures:**
- Logging (Serilog with structured logging)
- OpenTelemetry (tracing, metrics, logs)
- Database options and connection management
- Rate limiting
- CORS (if enabled)
- API versioning
- OpenAPI/Swagger (if enabled)
- Health checks
- Global exception handling
- Validation pipeline behavior
- Security headers

#### `UseHeroPlatform` - Middleware Pipeline

```csharp
app.UseHeroPlatform(p =>
{
    p.MapModules = true;        // Auto-discover and map module endpoints
    p.ServeStaticFiles = true;  // Enable wwwroot
});
```

**Pipeline order:**
1. Exception handling
2. HTTPS redirection
3. Security headers
4. Static files
5. Hangfire dashboard
6. Routing
7. CORS
8. OpenAPI
9. Authentication
10. Audit middleware (if Auditing module present)
11. Rate limiting
12. Authorization
13. Module endpoints
14. Health checks
15. Current user middleware

### 3. Module Loading

Modules are auto-discovered and loaded via reflection:

```csharp
var moduleAssemblies = new Assembly[]
{
    typeof(IdentityModule).Assembly,
    typeof(MultitenancyModule).Assembly,
    typeof(AuditingModule).Assembly
};

builder.AddModules(moduleAssemblies);
```

**Module Loading Process:**
1. Scan assemblies for `[FshModule]` attributes
2. Order modules by `Order` property
3. Instantiate module classes
4. Call `ConfigureServices` on each module
5. Call `MapEndpoints` on each module during pipeline configuration

---

## Key Features

### 1. Multi-Tenancy

**Implementation:** Finbuckle.MultiTenant library
- Tenant isolation at database level
- Automatic tenant context resolution
- Tenant-specific database migrations
- Health checks per tenant

**Usage:**
```csharp
// Tenant context is automatically injected
app.UseHeroMultiTenantDatabases(); // Applies migrations for all tenants

// In your code
public class MyService
{
    private readonly ITenantInfo _tenant;
    
    public MyService(ITenantInfo tenant)
    {
        _tenant = tenant; // Current tenant context
    }
}
```

### 2. Identity & Authentication

**Features:**
- ASP.NET Core Identity with custom user/role models
- JWT token generation and refresh
- Permission-based authorization
- Rate-limited auth endpoints
- Email confirmation
- Password reset
- User profile with image storage

**Endpoints:**
- `POST /api/v1/identity/tokens` - Generate JWT token
- `POST /api/v1/identity/tokens/refresh` - Refresh token
- `GET /api/v1/identity/users` - List users
- `POST /api/v1/identity/users/register` - Register user
- `GET /api/v1/identity/roles` - List roles
- And more...

### 3. Auditing

Tracks security events, exceptions, and user activities:
- Login/logout events
- Failed authentication attempts
- Exception logs
- HTTP request/response logging
- Queryable audit endpoints

### 4. Background Jobs (Hangfire)

**Configuration:**
```csharp
builder.AddHeroPlatform(o => o.EnableJobs = true);
```

**Usage:**
```csharp
// In module's MapEndpoints
var jobManager = endpoints.ServiceProvider.GetService<IRecurringJobManager>();
jobManager?.AddOrUpdate(
    "identity-outbox-dispatcher",
    Job.FromExpression<OutboxDispatcher>(d => d.DispatchAsync(CancellationToken.None)),
    Cron.Minutely(),
    new RecurringJobOptions());
```

### 5. Caching (Redis)

**Configuration:**
```json
{
  "CachingOptions": {
    "Redis": "localhost:6379"
  }
}
```

**Usage:**
```csharp
builder.AddHeroPlatform(o => o.EnableCaching = true);
```

### 6. Observability (OpenTelemetry)

**Features:**
- Distributed tracing
- Custom metrics
- Structured logging via Serilog
- OTLP export to collectors (e.g., Aspire Dashboard, Jaeger)

**Configuration:**
```json
{
  "OpenTelemetryOptions": {
    "Enabled": true,
    "Tracing": { "Enabled": true },
    "Metrics": { 
      "Enabled": true,
      "MeterNames": ["FSH.Modules.Identity"]
    },
    "Exporter": {
      "Otlp": {
        "Endpoint": "http://localhost:4317",
        "Protocol": "grpc"
      }
    }
  }
}
```

---

## Building Blocks Deep Dive

### Core Building Block

**Domain Primitives:**
- `Entity<TId>` - Base entity with domain events
- `AggregateRoot<TId>` - Aggregate root marker
- `DomainEvent` - Base domain event with correlation/tenant context
- `ValueObject` - Value object base class

**Example:**
```csharp
public record UserCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    string UserId,
    string Email,
    string? CorrelationId = null,
    string? TenantId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId);
```

### Persistence Building Block

**Features:**
- EF Core DbContext base classes
- Repository pattern
- Specification pattern
- Automatic audit fields (CreatedBy, ModifiedBy, etc.)
- Domain event dispatcher
- Multi-tenant DbContext support

**Usage:**
```csharp
public class IdentityDbContext : FshDbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) 
        : base(options) { }

    public DbSet<FshUser> Users => Set<FshUser>();
    public DbSet<FshRole> Roles => Set<FshRole>();
}

// Registration
builder.Services.AddHeroDbContext<IdentityDbContext>();
```

### Mediator Pattern (Mediator library)

**Request/Response:**
```csharp
// Command
public record GenerateTokenCommand(
    string Email,
    string Password
) : ICommand<GenerateTokenResponse>;

// Handler
public class GenerateTokenCommandHandler 
    : ICommandHandler<GenerateTokenCommand, GenerateTokenResponse>
{
    public async ValueTask<GenerateTokenResponse> Handle(
        GenerateTokenCommand command, 
        CancellationToken ct)
    {
        // Implementation
    }
}
```

**Behaviors:**
- `ValidationBehavior` - FluentValidation integration
- `MediatorTracingBehavior` - OpenTelemetry tracing

---

## Blazor UI

### Theme System

Custom theme with CSS variables:
- **Location:** `BuildingBlocks/Blazor.UI/wwwroot/css/fsh-theme.css`
- **Reference:** `_content/FSH.Framework.Blazor.UI/css/fsh-theme.css`

**CSS Variables:**
```css
:root {
  --fsh-radius: 10px;
  --fsh-shadow: 0 10px 30px rgba(15, 23, 42, 0.05);
}
```

**Components:**
- `FshComponentBase` - Base component with common services
- `FshSnackbar` - Toast notifications
- `FshBaseLayout` - Layout template

### Using Static Assets from Razor Class Library

Blazor.UI is a Razor Class Library. Static files are served under:
```
_content/{AssemblyName}/{path}
```

**Example:**
```html
<link href="_content/FSH.Framework.Blazor.UI/css/fsh-theme.css" rel="stylesheet" />
```

---

## Configuration Guide

### Database Configuration

```json
{
  "DatabaseOptions": {
    "Provider": "POSTGRESQL",
    "ConnectionString": "Server=localhost;Database=fsh;User Id=postgres;Password=password",
    "MigrationsAssembly": "FSH.Playground.Migrations.PostgreSQL"
  }
}
```

**Supported Providers:** PostgreSQL, SQL Server

### JWT Configuration

```json
{
  "JwtOptions": {
    "SigningKey": "your-super-secret-key-here-at-least-32-chars",
    "Issuer": "https://localhost:5285",
    "Audience": "https://localhost:7030",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### CORS Configuration

```json
{
  "CorsOptions": {
    "AllowAll": false,
    "AllowedOrigins": [
      "https://localhost:7030"
    ]
  }
}
```

### Rate Limiting

```json
{
  "RateLimitingOptions": {
    "Enabled": true,
    "FixedWindowPolicies": [
      {
        "PolicyName": "auth",
        "PermitLimit": 5,
        "Window": "00:01:00"
      }
    ]
  }
}
```

---

## Running the Project

### Prerequisites
- .NET 10 SDK
- .NET Aspire workload: `dotnet workload install aspire`
- Docker Desktop (for Postgres/Redis)

### Option 1: Using Aspire (Recommended)

```bash
# Start everything (API + Blazor + Postgres + Redis + Dashboard)
dotnet run --project src/Playground/FSH.Playground.AppHost

# Access:
# - API: https://localhost:5285
# - Blazor: https://localhost:7030
# - Aspire Dashboard: http://localhost:15283
```

### Option 2: Running API Only

1. Set up PostgreSQL and Redis
2. Configure connection strings in `appsettings.json`
3. Run:
```bash
dotnet run --project src/Playground/Playground.Api
```

### Option 3: Running with Docker

```bash
# Start infrastructure
docker-compose up -d

# Run API
dotnet run --project src/Playground/Playground.Api
```

---

## Extending the Framework

### Adding a New Module

1. **Create Module Structure:**
```
Modules/
??? YourModule/
?   ??? Modules.YourModule/          # Main module
?   ?   ??? Features/v1/             # Use cases
?   ?   ??? Data/                    # DbContext
?   ?   ??? YourModule.cs            # Module definition
?   ??? Modules.YourModule.Contracts/ # Contracts
```

2. **Define Module Class:**
```csharp
[assembly: FshModule(typeof(YourModule), Order = 10)]

namespace FSH.Modules.YourModule;

public class YourModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddHeroDbContext<YourModuleDbContext>();
        builder.Services.AddScoped<IYourService, YourService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/your-module")
            .WithTags("YourModule");
        
        // Map your endpoints
    }
}
```

3. **Register Module:**
```csharp
builder.AddModules([
    typeof(IdentityModule).Assembly,
    typeof(YourModule).Assembly  // Add here
]);
```

### Adding a New Endpoint

1. **Create Command/Query:**
```csharp
public record GetItemsQuery(int PageNumber, int PageSize) 
    : IQuery<PagedList<ItemDto>>;
```

2. **Create Handler:**
```csharp
public class GetItemsQueryHandler 
    : IQueryHandler<GetItemsQuery, PagedList<ItemDto>>
{
    private readonly YourModuleDbContext _db;

    public GetItemsQueryHandler(YourModuleDbContext db) 
        => _db = db;

    public async ValueTask<PagedList<ItemDto>> Handle(
        GetItemsQuery query, 
        CancellationToken ct)
    {
        // Implementation
    }
}
```

3. **Create Endpoint:**
```csharp
public static class GetItemsEndpoint
{
    public static IEndpointRouteBuilder MapGetItemsEndpoint(
        this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/items", async (
            IMediator mediator,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10) =>
        {
            var result = await mediator.Send(
                new GetItemsQuery(pageNumber, pageSize));
            return Results.Ok(result);
        })
        .WithName(nameof(GetItemsEndpoint))
        .RequireAuthorization();

        return builder;
    }
}
```

4. **Map in Module:**
```csharp
public void MapEndpoints(IEndpointRouteBuilder endpoints)
{
    var group = endpoints.MapGroup("api/v1/items");
    group.MapGetItemsEndpoint();
}
```

---

## Best Practices

### 1. Separation of Concerns
- Keep contracts separate from implementation
- Use feature folders for organization
- Follow vertical slice architecture

### 2. Security
- Always use `RequireAuthorization()` unless endpoint is public
- Apply rate limiting to auth endpoints
- Use permission-based authorization
- Validate all inputs with FluentValidation

### 3. Database
- Use migrations for schema changes
- Implement specifications for complex queries
- Leverage domain events for side effects
- Enable auditing for sensitive operations

### 4. Testing
- Write architecture tests to enforce boundaries
- Test business logic in handlers
- Use in-memory database for integration tests

### 5. Observability
- Add custom metrics for business events
- Use structured logging
- Leverage activity tags for tracing

---

## Troubleshooting

### CSS 404 Errors
**Problem:** `fsh-theme.css` not found

**Solution:** Ensure path includes subdirectory:
```html
<!-- Wrong -->
<link href="_content/FSH.Framework.Blazor.UI/fsh-theme.css" />

<!-- Correct -->
<link href="_content/FSH.Framework.Blazor.UI/css/fsh-theme.css" />
```

### Database Migration Issues
**Problem:** Migrations not applying

**Solution:**
```csharp
// Ensure this is called in Program.cs
app.UseHeroMultiTenantDatabases();
```

### Module Not Loading
**Problem:** Module endpoints not available

**Solution:**
1. Check `[FshModule]` attribute is present
2. Verify assembly is included in `AddModules()`
3. Ensure `MapModules = true` in `UseHeroPlatform()`

---

## Resources

- **GitHub Repository:** [fullstackhero/dotnet-starter-kit](https://github.com/fullstackhero/dotnet-starter-kit)
- **Documentation:** [fullstackhero.net/dotnet-webapi-boilerplate](https://fullstackhero.net/dotnet-webapi-boilerplate/)
- **Author:** Mukesh Murugan ([codewithmukesh.com](https://codewithmukesh.com))

---

## License

MIT License - See LICENSE file for details

---

**Last Updated:** 2025
**Framework Version:** .NET 10
**Project Version:** 2.0.4-rc
