using Domain.Entities;
using Tests.Helpers;
using Xunit;
using FluentAssertions;

namespace Tests.Infrastructure;

public class RepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsEntity()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var repository = new Repository<Project>(context);

        // Act
        var result = await repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test Project");
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var repository = new Repository<Project>(context);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        context.Projects.Add(new Project { Id = 2, TenantId = 1, Name = "Project 2" });
        context.SaveChanges();

        var repository = new Repository<Project>(context);

        // Act
        var results = await repository.GetAllAsync();

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindAsync_WithValidPredicate_ReturnsMatchingEntities()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        context.Projects.Add(new Project { Id = 2, TenantId = 1, Name = "Another Project" });
        context.SaveChanges();

        var repository = new Repository<Project>(context);

        // Act
        var results = await repository.FindAsync(p => p.Name.Contains("Test"));

        // Assert
        results.Should().HaveCount(1);
        results.First().Name.Should().Be("Test Project");
    }

    [Fact]
    public async Task AddAsync_WithValidEntity_AddsEntity()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var repository = new Repository<Project>(context);
        var newProject = new Project { Id = 2, TenantId = 1, Name = "New Project" };

        // Act
        await repository.AddAsync(newProject);
        await context.SaveChangesAsync();

        // Assert
        var savedProject = await repository.GetByIdAsync(2);
        savedProject.Should().NotBeNull();
        savedProject.Name.Should().Be("New Project");
    }

    [Fact]
    public async Task AddRangeAsync_WithValidEntities_AddsAllEntities()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var repository = new Repository<Project>(context);
        var projects = new[]
        {
            new Project { Id = 2, TenantId = 1, Name = "New Project 1" },
            new Project { Id = 3, TenantId = 1, Name = "New Project 2" }
        };

        // Act
        await repository.AddRangeAsync(projects);
        await context.SaveChangesAsync();

        // Assert
        var savedProjects = await repository.GetAllAsync();
        savedProjects.Should().HaveCount(3);
        savedProjects.Select(p => p.Name).Should().Contain(new[] { "New Project 1", "New Project 2" });
    }

    [Fact]
    public async Task Update_WithValidEntity_UpdatesEntity()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var repository = new Repository<Project>(context);
        var project = await repository.GetByIdAsync(1);

        // Act
        project.Name = "Updated Project";
        repository.Update(project);
        await context.SaveChangesAsync();

        // Assert
        var updated = await repository.GetByIdAsync(1);
        updated.Name.Should().Be("Updated Project");
    }

    [Fact]
    public async Task Delete_WithValidEntity_DeletesEntity()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var repository = new Repository<Project>(context);
        var project = await repository.GetByIdAsync(1);

        // Act
        repository.Delete(project);
        await context.SaveChangesAsync();

        // Assert
        var deleted = await repository.GetByIdAsync(1);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRange_WithValidEntities_DeletesAllEntities()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var repository = new Repository<Project>(context);
        await repository.AddRangeAsync(new[]
        {
            new Project { Id = 2, TenantId = 1, Name = "Project 2" },
            new Project { Id = 3, TenantId = 1, Name = "Project 3" }
        });
        await context.SaveChangesAsync();

        var projectsToDelete = await repository.FindAsync(p => p.Id > 1);

        // Act
        repository.DeleteRange(projectsToDelete);
        await context.SaveChangesAsync();

        // Assert
        var remainingProjects = await repository.GetAllAsync();
        remainingProjects.Should().ContainSingle();
        remainingProjects.Single().Id.Should().Be(1);
    }

    [Fact]
    public async Task AnyAsync_WithMatchingPredicate_ReturnsTrue()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var repository = new Repository<Project>(context);

        // Act
        var result = await repository.AnyAsync(p => p.Name == "Test Project");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_WithNonMatchingPredicate_ReturnsFalse()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var repository = new Repository<Project>(context);

        // Act
        var result = await repository.AnyAsync(p => p.Name == "Non-existent");

        // Assert
        result.Should().BeFalse();
    }
}
