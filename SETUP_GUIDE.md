# Multi-Tenant SaaS Backend - Setup Guide

## Quick Start (5 minutes)

### Prerequisites
- .NET 8 SDK installed
- PostgreSQL 12+ installed and running
- Visual Studio Code or Visual Studio (optional)

### Step 1: Database Setup

```bash
# Connect to PostgreSQL
psql -U postgres

# Create database
CREATE DATABASE multitenantdb;

# Exit psql
\q
```

### Step 2: Configure Connection String

Edit `WebAPI/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=multitenantdb;Username=postgres;Password=yourpassword"
  },
  ...
}
```

Replace `yourpassword` with your PostgreSQL password.

### Step 3: Restore Dependencies

```bash
cd c:\Personal\MultiTenantSaaS
dotnet restore
```

### Step 4: Apply Database Migrations

```bash
# Create initial migration from current DbContext
dotnet ef migrations add InitialCreate --project Infrastructure --startup-project WebAPI

# Apply migration to database
dotnet ef database update --project Infrastructure --startup-project WebAPI
```

### Step 5: Run the Application

```bash
dotnet run --project WebAPI
```

Output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
```

### Step 6: Test the Application

Open browser and navigate to: `https://localhost:5001/swagger`

Or use curl:
```bash
curl https://localhost:5001/api/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:45.1234567Z",
  "version": "1.0"
}
```

## Detailed Setup in VS Code

### 1. Open the Workspace

```bash
code c:\Personal\MultiTenantSaaS
```

### 2. Install C# Extension

- Open Extensions (Ctrl+Shift+X)
- Search for "C# Dev Kit"
- Click Install

### 3. Verify Build

```bash
# Open integrated terminal (Ctrl+`)
dotnet build
```

Should see: "Build succeeded."

### 4. Run Application

```bash
# Create launch configuration if needed
# VS Code typically handles this automatically

# Run with debugging
F5
```

## Configuring JWT Settings

In `WebAPI/appsettings.json`, update JWT settings:

```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-minimum-32-characters-for-production",
    "Issuer": "MultiTenantSaaS",
    "Audience": "MultiTenantSaaS",
    "ExpiryMinutes": 60
  }
}
```

⚠️ **Production Security**: Generate strong secret keys and use environment variables.

## Testing the API

### 1. Login and Get Token

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123",
    "tenantId": 1
  }'
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### 2. Use Token to Access Protected Endpoints

```bash
# Save token
$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Get projects
curl -H "Authorization: Bearer $token" \
     -H "X-Tenant-Id: 1" \
     https://localhost:5001/api/projects

# Create project
curl -X POST \
     -H "Authorization: Bearer $token" \
     -H "X-Tenant-Id: 1" \
     -H "Content-Type: application/json" \
     -d '{"name":"My First Project"}' \
     https://localhost:5001/api/projects
```

## Project Structure Overview

- **Domain/** - Core business entities (no dependencies)
- **Application/** - Business logic and handlers (depends on Domain)
- **Infrastructure/** - Data access and external services (depends on Application)
- **WebAPI/** - REST API controllers and middleware (depends on Infrastructure)

## Database Schema

The application creates the following tables:

### Tenants
```sql
CREATE TABLE tenants (
  id INT PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(255)
);
```

### Users
```sql
CREATE TABLE users (
  id INT PRIMARY KEY AUTO_INCREMENT,
  tenant_id INT NOT NULL,
  email VARCHAR(255),
  password_hash VARCHAR(255),
  role VARCHAR(50),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id)
);
```

### Projects
```sql
CREATE TABLE projects (
  id INT PRIMARY KEY AUTO_INCREMENT,
  tenant_id INT NOT NULL,
  name VARCHAR(255),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id)
);
```

## Common Issues and Solutions

### Issue: Database connection refused

**Solution:**
```bash
# Check PostgreSQL is running
psql -U postgres -c "SELECT version();"

# If not running, start PostgreSQL
# Windows: Start service from Services
# Linux: sudo systemctl start postgresql
# Mac: brew services start postgresql
```

### Issue: Cannot find type 'Domain'

**Solution:** Ensure you run migrations first:
```bash
dotnet ef database update --project Infrastructure --startup-project WebAPI
```

### Issue: Port 5001 already in use

**Solution:** Change port in `Properties/launchSettings.json`:
```json
"applicationUrl": "https://localhost:5002;http://localhost:5003"
```

### Issue: JWT token validation fails

**Solution:** Check that:
- Secret key matches between generation and validation
- Token hasn't expired (check ExpiryMinutes setting)
- TenantId is included in JWT claims

## Next Steps

1. **Create Tenant and User**: Implement endpoints to add users to your database
2. **Add Validations**: Implement FluentValidation for command/query validation
3. **Add Logging**: Monitor logs in `logs/` directory
4. **Test Multi-Tenancy**: Create multiple tenants and verify data isolation
5. **Implement Microservices**: Add event-based processing (Order Service example in roadmap)

## Configuration Files Reference

### appsettings.json
- Connection strings
- JWT settings
- Logging configuration

### Properties/launchSettings.json
- Application URLs
- Environment variables
- Launch profiles

### .csproj files
- NuGet packages
- Project references
- Build configuration

## Useful Commands

```bash
# Build the solution
dotnet build

# Run tests (when added)
dotnet test

# Clean build artifacts
dotnet clean

# Format code
dotnet format

# Create migration
dotnet ef migrations add MigrationName --project Infrastructure --startup-project WebAPI

# Revert migration
dotnet ef migrations remove --project Infrastructure --startup-project WebAPI

# View pending migrations
dotnet ef migrations list --project Infrastructure --startup-project WebAPI
```

## Development Workflow

1. Create new feature branch
2. Add domain entity (if needed) → Domain/Entities/
3. Add application handler → Application/Projects/ or respective folder
4. Add/update infrastructure components → Infrastructure/
5. Create or update controller → WebAPI/Controllers/
6. Test with API examples
7. Commit and push

## Performance Considerations

1. **Caching**: Use ICacheService for frequently accessed data
2. **Query Optimization**: Add indexes on TenantId and frequently filtered columns
3. **Pagination**: Consider implementing pagination for large data sets
4. **Lazy Loading**: Be cautious with entity relationships
5. **Connection Pooling**: Already configured in Entity Framework Core

## Security Checklist

- [ ] Change JWT secret key for production
- [ ] Use HTTPS in production
- [ ] Implement rate limiting
- [ ] Add input validation and sanitization
- [ ] Implement audit logging
- [ ] Use environment variables for sensitive data
- [ ] Enable CORS only for trusted domains
- [ ] Implement proper authorization roles

## Support and Documentation

- See `API_DOCUMENTATION.md` for complete API reference
- See `API_EXAMPLES.http` for example requests
- Check the main `multitenant_saas_architecture.md` for architecture overview

Happy coding! 🚀
