using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using FluentAssertions;
using Domain.Entities;
using Tests.Helpers;

namespace Tests.WebAPI.Controllers;

public class ProjectsControllerTests
{
    [Fact]
    public async Task GetById_WithValidId_ReturnsOkWithProject()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);
        var getByIdHandler = new GetProjectByIdQueryHandler(unitOfWork);
        var getAllHandler = new GetAllProjectsQueryHandler(unitOfWork);
        var mockTenantContext = new MockTenantContext();
        var createHandler = new CreateProjectCommandHandler(unitOfWork, mockTenantContext);
        var updateHandler = new UpdateProjectCommandHandler(unitOfWork);
        var deleteHandler = new DeleteProjectCommandHandler(unitOfWork);

        var controller = new ProjectsController(
            getByIdHandler, getAllHandler, createHandler, updateHandler, deleteHandler);

        // Act
        var result = await controller.GetById(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (result as OkObjectResult)!;
        okResult.Value.Should().BeOfType<Project>();
        var project = (okResult.Value as Project)!;
        project.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);
        var getByIdHandler = new GetProjectByIdQueryHandler(unitOfWork);
        var getAllHandler = new GetAllProjectsQueryHandler(unitOfWork);
        var mockTenantContext = new MockTenantContext();
        var createHandler = new CreateProjectCommandHandler(unitOfWork, mockTenantContext);
        var updateHandler = new UpdateProjectCommandHandler(unitOfWork);
        var deleteHandler = new DeleteProjectCommandHandler(unitOfWork);

        var controller = new ProjectsController(
            getByIdHandler, getAllHandler, createHandler, updateHandler, deleteHandler);

        // Act
        var result = await controller.GetById(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithProjects()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);
        var getByIdHandler = new GetProjectByIdQueryHandler(unitOfWork);
        var getAllHandler = new GetAllProjectsQueryHandler(unitOfWork);
        var mockTenantContext = new MockTenantContext();
        var createHandler = new CreateProjectCommandHandler(unitOfWork, mockTenantContext);
        var updateHandler = new UpdateProjectCommandHandler(unitOfWork);
        var deleteHandler = new DeleteProjectCommandHandler(unitOfWork);

        var controller = new ProjectsController(
            getByIdHandler, getAllHandler, createHandler, updateHandler, deleteHandler);

        // Act
        var result = await controller.GetAll();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (result as OkObjectResult)!;
        var projects = (okResult.Value as IEnumerable<Project>)!;
        projects.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_WithValidCommand_ReturnsCreatedAtAction()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);
        var getByIdHandler = new GetProjectByIdQueryHandler(unitOfWork);
        var getAllHandler = new GetAllProjectsQueryHandler(unitOfWork);
        var mockTenantContext = new MockTenantContext();
        var createHandler = new CreateProjectCommandHandler(unitOfWork, mockTenantContext);
        var updateHandler = new UpdateProjectCommandHandler(unitOfWork);
        var deleteHandler = new DeleteProjectCommandHandler(unitOfWork);

        var controller = new ProjectsController(
            getByIdHandler, getAllHandler, createHandler, updateHandler, deleteHandler);

        var command = new CreateProjectCommand { Name = "New Project" };

        // Act
        var result = await controller.Create(command);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (result as CreatedAtActionResult)!;
        createdResult.ActionName.Should().Be(nameof(ProjectsController.GetById));
    }

    [Fact]
    public async Task Update_WithValidCommand_ReturnsOk()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);
        var getByIdHandler = new GetProjectByIdQueryHandler(unitOfWork);
        var getAllHandler = new GetAllProjectsQueryHandler(unitOfWork);
        var mockTenantContext = new MockTenantContext();
        var createHandler = new CreateProjectCommandHandler(unitOfWork, mockTenantContext);
        var updateHandler = new UpdateProjectCommandHandler(unitOfWork);
        var deleteHandler = new DeleteProjectCommandHandler(unitOfWork);

        var controller = new ProjectsController(
            getByIdHandler, getAllHandler, createHandler, updateHandler, deleteHandler);

        var command = new UpdateProjectCommand { Name = "Updated" };

        // Act
        var result = await controller.Update(1, command);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        command.Id.Should().Be(1);
    }

    [Fact]
    public async Task Update_WithRouteId_OverridesBodyId()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);
        var getByIdHandler = new GetProjectByIdQueryHandler(unitOfWork);
        var getAllHandler = new GetAllProjectsQueryHandler(unitOfWork);
        var mockTenantContext = new MockTenantContext();
        var createHandler = new CreateProjectCommandHandler(unitOfWork, mockTenantContext);
        var updateHandler = new UpdateProjectCommandHandler(unitOfWork);
        var deleteHandler = new DeleteProjectCommandHandler(unitOfWork);

        var controller = new ProjectsController(
            getByIdHandler, getAllHandler, createHandler, updateHandler, deleteHandler);

        var command = new UpdateProjectCommand { Id = 999, Name = "Updated From Route" };

        // Act
        var result = await controller.Update(1, command);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        command.Id.Should().Be(1);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var unitOfWork = new UnitOfWork(context);
        var getByIdHandler = new GetProjectByIdQueryHandler(unitOfWork);
        var getAllHandler = new GetAllProjectsQueryHandler(unitOfWork);
        var mockTenantContext = new MockTenantContext();
        var createHandler = new CreateProjectCommandHandler(unitOfWork, mockTenantContext);
        var updateHandler = new UpdateProjectCommandHandler(unitOfWork);
        var deleteHandler = new DeleteProjectCommandHandler(unitOfWork);

        var controller = new ProjectsController(
            getByIdHandler, getAllHandler, createHandler, updateHandler, deleteHandler);

        // Act
        var result = await controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}

public class AuthControllerTests
{
    [Fact]
    public void Login_WithValidRequest_ReturnsOkWithToken()
    {
        // Arrange
        var mockConfiguration = new Mock<IConfiguration>();
        var mockJwtSettings = new Mock<IConfigurationSection>();

        mockConfiguration
            .Setup(c => c.GetSection("JwtSettings"))
            .Returns(mockJwtSettings.Object);

        mockJwtSettings
            .Setup(s => s["SecretKey"])
            .Returns("test-secret-key-that-is-at-least-32-characters-long");

        mockJwtSettings
            .Setup(s => s["Issuer"])
            .Returns("TestIssuer");

        mockJwtSettings
            .Setup(s => s["Audience"])
            .Returns("TestAudience");

        mockJwtSettings
            .Setup(s => s["ExpiryMinutes"])
            .Returns("60");

        var controller = new AuthController(mockConfiguration.Object);
        var request = new LoginRequest { Email = "user@example.com", Password = "password123", TenantId = 1 };

        // Act
        var result = controller.Login(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (result as OkObjectResult)!;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public void Login_WithEmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var mockConfiguration = new Mock<IConfiguration>();
        var controller = new AuthController(mockConfiguration.Object);
        var request = new LoginRequest { Email = "", Password = "password123", TenantId = 1 };

        // Act
        var result = controller.Login(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Login_WithEmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var mockConfiguration = new Mock<IConfiguration>();
        var controller = new AuthController(mockConfiguration.Object);
        var request = new LoginRequest { Email = "user@example.com", Password = "", TenantId = 1 };

        // Act
        var result = controller.Login(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
