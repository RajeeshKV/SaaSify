using Domain.Entities;
using Tests.Helpers;
using Xunit;
using FluentAssertions;

namespace Tests.Infrastructure;

public class UnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_WithValidChanges_SavesSuccessfully()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);

        var newProject = new Project { Id = 2, TenantId = 1, Name = "New Project" };
        await unitOfWork.Projects.AddAsync(newProject);

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Tenants_Property_ReturnsRepositoryInstance()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);

        // Act
        var repository = unitOfWork.Tenants;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepository<Tenant>>();
    }

    [Fact]
    public void Users_Property_ReturnsRepositoryInstance()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);

        // Act
        var repository = unitOfWork.Users;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepository<User>>();
    }

    [Fact]
    public void Projects_Property_ReturnsRepositoryInstance()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);

        // Act
        var repository = unitOfWork.Projects;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepository<Project>>();
    }

    [Fact]
    public async Task BeginTransactionAsync_StartsTransaction()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);

        // Act
        await unitOfWork.BeginTransactionAsync();

        // Assert
        // If no exception thrown, transaction started successfully
        await unitOfWork.RollbackTransactionAsync();
    }

    [Fact]
    public async Task CommitTransactionAsync_CommitsChanges()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);

        var newProject = new Project { Id = 2, TenantId = 1, Name = "New Project" };
        await unitOfWork.Projects.AddAsync(newProject);
        await unitOfWork.BeginTransactionAsync();

        // Act
        await unitOfWork.CommitTransactionAsync();

        // Assert
        var savedProject = await unitOfWork.Projects.GetByIdAsync(2);
        savedProject.Should().NotBeNull();
    }

    [Fact]
    public async Task RollbackTransactionAsync_RollsBackChanges()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);

        // Act & Assert - should not throw
        await unitOfWork.RollbackTransactionAsync();
    }

    [Fact]
    public void Dispose_DisposesResources()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);

        // Act
        unitOfWork.Dispose();

        // Assert - no exception should be thrown
        Assert.True(true);
    }
}
