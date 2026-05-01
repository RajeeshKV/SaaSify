# Multi-Tenant SaaS Backend API

A comprehensive multi-tenant SaaS backend built with .NET 8 using Clean Architecture, featuring JWT authentication, Serilog logging, CQRS-lite pattern, UnitOfWork with repositories, and multi-tenancy support.

## Features

✅ **Multi-Tenancy** - Complete tenant isolation with global query filters  
✅ **JWT Authentication** - Secure token-based authentication  
✅ **CQRS-Lite Pattern** - Commands and queries for business operations  
✅ **UnitOfWork Pattern** - Generic repository implementation for data access  
✅ **Structured Logging** - Serilog integration with CorrelationId tracking  
✅ **Caching** - In-memory caching with Memory/Redis support  
✅ **Exception Handling** - Centralized middleware for error handling  
✅ **Clean Architecture** - Organized layer separation (WebAPI → Application → Domain → Infrastructure)  

## Architecture

```
WebAPI (Controllers, Middleware, Auth)
    ↓
Application (Handlers, CQRS)
    ↓
Domain (Entities)
    ↓
Infrastructure (DbContext, Repositories, Services)
```

### Middleware Pipeline

1. **ExceptionMiddleware** - Catches and handles all exceptions
2. **CorrelationMiddleware** - Generates correlation IDs for request tracking
3. **TenantMiddleware** - Extracts and validates tenant ID from headers or JWT claims
4. **Authentication** - JWT bearer token validation

## Prerequisites

- .NET 8 SDK
- PostgreSQL 12+
- Visual Studio Code or Visual Studio

## Getting Started

### 1. Setup PostgreSQL Database

```bash
# Create database
createdb multitenantdb

# Connection string in appsettings.json:
# "Host=localhost;Port=5432;Database=multitenantdb;Username=postgres;Password=yourpassword"
```

### 2. Install Dependencies

```bash
dotnet restore
```

### 3. Run Database Migrations

```bash
dotnet ef database update --project Infrastructure --startup-project WebAPI
```

### 4. Run the Application

```bash
dotnet run --project WebAPI
```

The API will be available at: `https://localhost:5001`

## API Endpoints

### Authentication

**POST** `/api/auth/login` - Get JWT token
```json
{
  "email": "user@example.com",
  "password": "password123",
  "tenantId": 1
}
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### Projects (Requires Authentication)

**GET** `/api/projects` - Get all projects for current tenant
```bash
curl -H "Authorization: Bearer <token>" \
     -H "X-Tenant-Id: 1" \
     https://localhost:5001/api/projects
```

**GET** `/api/projects/{id}` - Get project by ID
```bash
curl -H "Authorization: Bearer <token>" \
     -H "X-Tenant-Id: 1" \
     https://localhost:5001/api/projects/1
```

**POST** `/api/projects` - Create new project
```bash
curl -X POST -H "Authorization: Bearer <token>" \
     -H "X-Tenant-Id: 1" \
     -H "Content-Type: application/json" \
     -d '{"name":"My Project"}' \
     https://localhost:5001/api/projects
```

**PUT** `/api/projects/{id}` - Update project
```bash
curl -X PUT -H "Authorization: Bearer <token>" \
     -H "X-Tenant-Id: 1" \
     -H "Content-Type: application/json" \
     -d '{"name":"Updated Project","id":1}' \
     https://localhost:5001/api/projects/1
```

**DELETE** `/api/projects/{id}` - Delete project
```bash
curl -X DELETE -H "Authorization: Bearer <token>" \
     -H "X-Tenant-Id: 1" \
     https://localhost:5001/api/projects/1
```

### Health Check

**GET** `/api/health` - Check API health
```bash
curl https://localhost:5001/api/health
```

## Multi-Tenancy

The system enforces tenant isolation in multiple ways:

1. **Header-based**: Send `X-Tenant-Id: <tenant-id>` header in requests
2. **JWT Claims**: Include `TenantId` claim in the JWT token
3. **Query Filters**: Automatic filtering of User and Project entities by TenantId

Example request:
```bash
curl -H "X-Tenant-Id: 1" \
     -H "Authorization: Bearer <token>" \
     https://localhost:5001/api/projects
```

## Logging

Logs are stored in the `logs/` directory with daily rolling files.

**Example log entry:**
```
2024-01-15 10:30:45.123 +00:00 [INF] Tenant 1 set for request /api/projects
2024-01-15 10:30:46.456 +00:00 [INF] Created project "My Project" for tenant 1
```

**CorrelationId** tracking:
- Automatically generated for each request
- Can be passed via `X-Correlation-Id` header
- Returned in response headers
- Included in all logs for request tracing

## Project Structure

```
Application/
├── Common/Interfaces/          # Abstractions and interfaces
│   ├── IRepository.cs
│   ├── IUnitOfWork.cs
│   ├── ICacheService.cs
│   └── ICorrelationIdGenerator.cs
└── Projects/
    ├── Commands/               # CQRS Commands
    │   ├── CreateProjectCommand.cs
    │   ├── UpdateProjectCommand.cs
    │   └── DeleteProjectCommand.cs
    └── Queries/                # CQRS Queries
        ├── GetProjectByIdQuery.cs
        └── GetAllProjectsQuery.cs

Domain/
└── Entities/                   # Business entities
    ├── Tenant.cs
    ├── User.cs
    └── Project.cs

Infrastructure/
├── ApplicationDbContext.cs       # EF Core DbContext
├── DependencyInjection.cs        # Service registration
├── Persistence/                  # Repository implementations
│   ├── Repository.cs
│   └── UnitOfWork.cs
├── Caching/                      # Caching services
│   └── InMemoryCacheService.cs
├── MultiTenancy/                 # Tenant management
│   └── TenantContext.cs
└── Services/                     # Utility services
    └── CorrelationIdGenerator.cs

WebAPI/
├── Program.cs                    # Application startup
├── Controllers/                  # API endpoints
│   ├── ProjectsController.cs
│   ├── AuthController.cs
│   └── HealthController.cs
├── Middleware/                   # HTTP middleware
│   ├── TenantMiddleware.cs
│   ├── CorrelationMiddleware.cs
│   └── ExceptionMiddleware.cs
├── Auth/                         # JWT utilities
│   ├── JwtTokenGenerator.cs
│   └── AuthenticationExtensions.cs
└── appsettings*.json             # Configuration
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=multitenantdb;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-that-is-at-least-32-characters-long-for-production",
    "Issuer": "MultiTenantSaaS",
    "Audience": "MultiTenantSaaS",
    "ExpiryMinutes": 60
  }
}
```

## Key Classes and Interfaces

### IUnitOfWork
Manages repository operations and transactions:
```csharp
var user = await unitOfWork.Users.GetByIdAsync(userId);
await unitOfWork.Projects.AddAsync(newProject);
await unitOfWork.SaveChangesAsync();
```

### ICacheService
Abstracts caching operations:
```csharp
await cacheService.SetAsync("cache-key", value, TimeSpan.FromHours(1));
var cachedValue = await cacheService.GetAsync<T>("cache-key");
```

### TenantContext
Provides current tenant information:
```csharp
var currentTenantId = tenantContext.TenantId;
```

## Error Handling

All exceptions are caught by the ExceptionMiddleware and returned as:

```json
{
  "message": "Bad Request",
  "details": "Detailed error message"
}
```

HTTP Status Codes:
- `400` - Bad Request (ArgumentNullException)
- `401` - Unauthorized (UnauthorizedAccessException)
- `500` - Internal Server Error (other exceptions)

## Security

- **JWT Tokens**: Signed with HS256 algorithm
- **Nullable Reference Types**: Enabled for null-safety
- **Tenant Isolation**: Enforced at query and controller levels
- **Authentication**: Required for most endpoints

## Testing

For manual testing, use the included `WebAPI.http` file or use curl commands as shown in the API endpoints section.

## Roadmap Updates

- ✅ Base setup
- ✅ JWT + Query Filters
- ✅ Logging + Error Handling
- ✅ Caching
- ⏳ Microservices (Order Processing)
- ⏳ Redis Integration
- ⏳ Unit Tests
- ⏳ Integration Tests

## Troubleshooting

### "TenantId is required" error
- Ensure you're sending `X-Tenant-Id` header in your requests
- Or include `TenantId` in your JWT claims during token generation

### Database connection error
- Verify PostgreSQL is running
- Check connection string in appsettings.json
- Verify database credentials

### JWT token validation fails
- Ensure SecretKey matches between token generation and validation
- Check token hasn't expired
- Verify Issuer and Audience match configuration

## Contributing

1. Create a feature branch
2. Implement changes following the architecture
3. Add handlers/commands/queries to Application layer
4. Update controllers accordingly
5. Submit a pull request

## License

MIT License - See LICENSE file for details
