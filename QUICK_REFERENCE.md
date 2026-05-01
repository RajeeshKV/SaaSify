# Quick Reference Guide

## 🚀 Quick Start

```bash
# 1. Navigate to project
cd c:\Personal\MultiTenantSaaS

# 2. Build
dotnet build

# 3. Run
dotnet run --project WebAPI
```

API available at: `https://localhost:5001`

## 📝 Common Commands

```bash
# Restore packages
dotnet restore

# Create migration
dotnet ef migrations add MigrationName --project Infrastructure --startup-project WebAPI

# Apply migrations
dotnet ef database update --project Infrastructure --startup-project WebAPI

# Build specific project
dotnet build --project WebAPI

# Publish for production
dotnet publish -c Release --project WebAPI
```

## 🔑 API Quick Reference

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/auth/login` | POST | ❌ | Get JWT token |
| `/api/projects` | GET | ✅ | List all projects |
| `/api/projects/{id}` | GET | ✅ | Get project by ID |
| `/api/projects` | POST | ✅ | Create project |
| `/api/projects/{id}` | PUT | ✅ | Update project |
| `/api/projects/{id}` | DELETE | ✅ | Delete project |
| `/api/health` | GET | ❌ | Health check |

## 📦 Required Headers

All authenticated endpoints require:
```
Authorization: Bearer <JWT_TOKEN>
X-Tenant-Id: <TENANT_ID>
```

Optional:
```
X-Correlation-Id: <CORRELATION_ID>  # Auto-generated if not provided
```

## 🏗️ Project Structure

```
Domain/          → Entities only (no dependencies)
Application/     → Handlers, Commands, Queries
Infrastructure/  → DbContext, Repositories, Services
WebAPI/         → Controllers, Middleware, Auth
```

## 📚 Key Files

| File | Purpose |
|------|---------|
| `SETUP_GUIDE.md` | Step-by-step setup instructions |
| `API_DOCUMENTATION.md` | Complete API reference |
| `IMPLEMENTATION_SUMMARY.md` | What's implemented and what's next |
| `API_EXAMPLES.http` | Ready-to-use API requests |
| `multitenant_saas_architecture.md` | Architecture overview |

## 🔐 Default Configuration

- **Database**: PostgreSQL on localhost:5432
- **Database Name**: multitenantdb
- **JWT Expiry**: 60 minutes
- **API Port**: 5001 (HTTPS)

## 📂 Important Folders

- `logs/` - Application logs (created on first run)
- `Domain/Entities/` - Business entities
- `Application/Projects/` - Business logic
- `Infrastructure/` - Data and external services
- `WebAPI/Controllers/` - API endpoints
- `WebAPI/Middleware/` - HTTP middleware

## 💦 Sample API Requests

### Login
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password123","tenantId":1}'
```

### List Projects
```bash
curl -H "Authorization: Bearer YOUR_TOKEN" \
     -H "X-Tenant-Id: 1" \
     https://localhost:5001/api/projects
```

### Create Project
```bash
curl -X POST \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "X-Tenant-Id: 1" \
  -H "Content-Type: application/json" \
  -d '{"name":"My Project"}' \
  https://localhost:5001/api/projects
```

## 🎯 File Locations

| Component | File Path |
|-----------|-----------|
| API Controllers | `WebAPI/Controllers/` |
| Middleware | `WebAPI/Middleware/` |
| Domain Entities | `Domain/Entities/` |
| Handlers | `Application/Projects/Commands/` / `Queries/` |
| Repositories | `Infrastructure/Persistence/` |
| DbContext | `Infrastructure/ApplicationDbContext.cs` |
| Configuration | `WebAPI/appsettings.json` |

## ⚙️ Configuration Files

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=multitenantdb;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "SecretKey": "...",
    "Issuer": "MultiTenantSaaS",
    "Audience": "MultiTenantSaaS",
    "ExpiryMinutes": 60
  }
}
```

## 🛠️ Extending the Application

### Add New Entity
1. Create in `Domain/Entities/`
2. Add DbSet to `ApplicationDbContext`
3. Create repository interface in `Application/`
4. Implement repository in `Infrastructure/`

### Add New Endpoint
1. Create Command/Query in `Application/Projects/`
2. Create Handler
3. Register handler in `Program.cs`
4. Add Controller method in `WebAPI/Controllers/`

### Add New Middleware
1. Create in `WebAPI/Middleware/`
2. Register in `Program.cs`
3. Add to middleware pipeline

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| Connection refused | Check PostgreSQL is running |
| Port 5001 in use | Change port in launchSettings.json |
| Database error | Run migrations: `dotnet ef database update` |
| JWT fails | Check SecretKey matches in appsettings.json |
| TenantId required | Add `X-Tenant-Id` header or TenantId in JWT |

## 📊 Development Workflow

1. Create feature branch
2. Add to Application layer (handlers)
3. Update Infrastructure if needed (repositories)
4. Add Controller endpoints
5. Test with API examples
6. Commit & push

## 🚀 Deploy to Production

1. Set environment variables for secrets
2. Update connection string for production database
3. Enable HTTPS with real certificate
4. Configure CORS for your domain
5. Set JWT secret key securely
6. Enable logging and monitoring
7. Run `dotnet publish -c Release`

## 📞 Support

- Check logs in `logs/` folder
- Review `API_DOCUMENTATION.md` for API details
- See `SETUP_GUIDE.md` for setup issues
- Check `IMPLEMENTATION_SUMMARY.md` for architecture

---

**Need more help?** See the full documentation files:
- `SETUP_GUIDE.md` - Detailed setup
- `API_DOCUMENTATION.md` - Complete API reference
- `IMPLEMENTATION_SUMMARY.md` - Implementation details
