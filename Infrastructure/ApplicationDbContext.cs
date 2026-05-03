using System;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filter for multi-tenancy
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<Project>()
            .HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<RefreshToken>()
            .HasQueryFilter(rt => rt.User.TenantId == _tenantContext.TenantId);

        // Add query filters for new tenant-specific entities
        modelBuilder.Entity<Role>()
            .HasQueryFilter(r => r.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<UserRole>()
            .HasQueryFilter(ur => ur.User.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(al => al.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<Subscription>()
            .HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<TenantSettings>()
            .HasQueryFilter(ts => ts.TenantId == _tenantContext.TenantId);

        // Configure relationships
        modelBuilder.Entity<User>()
            .HasOne(u => u.Tenant)
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.TokenHash)
            .IsUnique();

        // Configure RBAC relationships
        modelBuilder.Entity<Role>()
            .HasOne(r => r.Tenant)
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure AuditLog relationships
        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.Tenant)
            .WithMany()
            .HasForeignKey(al => al.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Subscription relationships
        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.Tenant)
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure TenantSettings
        modelBuilder.Entity<TenantSettings>()
            .HasKey(ts => ts.TenantId);

        modelBuilder.Entity<TenantSettings>()
            .HasOne(ts => ts.Tenant)
            .WithOne()
            .HasForeignKey<TenantSettings>(ts => ts.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
