using Domain.Entities;

public interface IUnitOfWork : IDisposable
{
    IRepository<Tenant> Tenants { get; }
    IRepository<User> Users { get; }
    IRepository<Project> Projects { get; }
    IRepository<Role> Roles { get; }
    IRepository<UserRole> UserRoles { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
