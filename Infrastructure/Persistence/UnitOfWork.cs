using Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction _transaction;
    private IRepository<Tenant> _tenantRepository;
    private IRepository<User> _userRepository;
    private IRepository<Project> _projectRepository;
    private IRepository<Role> _roleRepository;
    private IRepository<UserRole> _userRoleRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepository<Tenant> Tenants => _tenantRepository ??= new Repository<Tenant>(_context);
    public IRepository<User> Users => _userRepository ??= new Repository<User>(_context);
    public IRepository<Project> Projects => _projectRepository ??= new Repository<Project>(_context);
    public IRepository<Role> Roles => _roleRepository ??= new Repository<Role>(_context);
    public IRepository<UserRole> UserRoles => _userRoleRepository ??= new Repository<UserRole>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await SaveChangesAsync();
            await _transaction?.CommitAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            _transaction?.Dispose();
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            await _transaction?.RollbackAsync();
        }
        finally
        {
            _transaction?.Dispose();
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context?.Dispose();
    }
}
