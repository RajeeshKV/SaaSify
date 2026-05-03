using Application.Common.Pagination;
using Domain.Entities;
using Tests.Helpers;
using Xunit;
using FluentAssertions;

namespace Tests.Application.Projects.Queries;

public class GetProjectByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithValidId_ReturnsProject()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);
        var handler = new GetProjectByIdQueryHandler(unitOfWork);
        var query = new GetProjectByIdQuery { Id = 1 };

        // Act
        var result = await handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test Project");
    }

    [Fact]
    public async Task Handle_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);
        var handler = new GetProjectByIdQueryHandler(unitOfWork);
        var query = new GetProjectByIdQuery { Id = 999 };

        // Act
        var result = await handler.Handle(query);

        // Assert
        result.Should().BeNull();
    }
}

public class GetAllProjectsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllProjects()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateInMemoryDbContextWithData();
        context.Projects.Add(new Project { Id = 2, TenantId = 1, Name = "Project 2" });
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var handler = new GetAllProjectsQueryHandler(unitOfWork);
        var query = new GetAllProjectsQuery();

        // Act
        var result = await handler.Handle(query);

        // Assert
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNoProjects_ReturnsEmptyList()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        var mockTenant = context.Tenants.Add(new Tenant { Id = 2, Name = "Empty Tenant" });
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var handler = new GetAllProjectsQueryHandler(unitOfWork);
        var query = new GetAllProjectsQuery();

        // Act
        var result = await handler.Handle(query);

        // Assert
        result.Data.Should().BeEmpty();
    }
}
