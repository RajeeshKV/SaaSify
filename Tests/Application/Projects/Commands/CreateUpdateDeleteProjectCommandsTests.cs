using Domain.Entities;
using Tests.Helpers;
using Xunit;
using FluentAssertions;

namespace Tests.Application.Projects.Commands;

public class CreateProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_CreatesProject()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateInMemoryDbContextWithData();
        var mockTenantContext = new MockTenantContext();
        mockTenantContext.SetTenantId(1);

        var unitOfWork = new UnitOfWork(context);
        var handler = new CreateProjectCommandHandler(unitOfWork, mockTenantContext);
        var command = new CreateProjectCommand { Name = "New Project" };

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Project");
        result.TenantId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsProject()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateInMemoryDbContextWithData();
        var mockTenantContext = new MockTenantContext();
        mockTenantContext.SetTenantId(1);

        var unitOfWork = new UnitOfWork(context);
        var handler = new CreateProjectCommandHandler(unitOfWork, mockTenantContext);
        var command = new CreateProjectCommand { Name = "Persisted Project" };

        // Act
        var result = await handler.Handle(command);
        var savedProject = await unitOfWork.Projects.GetByIdAsync(result.Id);

        // Assert
        savedProject.Should().NotBeNull();
        savedProject.Name.Should().Be("Persisted Project");
    }
}

public class UpdateProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_UpdatesProject()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);
        var handler = new UpdateProjectCommandHandler(unitOfWork);
        var command = new UpdateProjectCommand { Id = 1, Name = "Updated Project" };

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Project");
    }

    [Fact]
    public async Task Handle_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);
        var handler = new UpdateProjectCommandHandler(unitOfWork);
        var command = new UpdateProjectCommand { Id = 999, Name = "Updated" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command));
    }
}

public class DeleteProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidId_DeletesProject()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);
        var handler = new DeleteProjectCommandHandler(unitOfWork);
        var command = new DeleteProjectCommand { Id = 1 };

        // Act
        await handler.Handle(command);

        // Assert
        var deletedProject = await unitOfWork.Projects.GetByIdAsync(1);
        deletedProject.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);
        var handler = new DeleteProjectCommandHandler(unitOfWork);
        var command = new DeleteProjectCommand { Id = 999 };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command));
    }
}
