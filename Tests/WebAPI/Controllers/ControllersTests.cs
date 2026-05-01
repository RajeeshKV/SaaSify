using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task Login_WithValidRequest_ReturnsOkWithToken()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var user = context.Users.IgnoreQueryFilters().Single();
        user.Email = "user@example.com";
        user.PasswordHash = PasswordHasher.HashPassword("password123");
        await context.SaveChangesAsync();

        var controller = CreateAuthController(context);
        var request = new LoginRequest { Email = "user@example.com", Password = "password123", TenantId = user.TenantId };

        // Act
        var result = await controller.Login(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (result as OkObjectResult)!;
        okResult.Value.Should().BeOfType<AuthResponse>();
        var response = (AuthResponse)okResult.Value!;
        response.Token.Should().NotBeNullOrWhiteSpace();
        response.RefreshToken.Should().NotBeNullOrWhiteSpace();
        response.TenantId.Should().Be(user.TenantId);
        context.RefreshTokens.IgnoreQueryFilters().Should().ContainSingle(rt => rt.UserId == user.Id);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var user = context.Users.IgnoreQueryFilters().Single();
        user.Email = "user@example.com";
        user.PasswordHash = PasswordHasher.HashPassword("password123");
        await context.SaveChangesAsync();

        var controller = CreateAuthController(context);
        var request = new LoginRequest { Email = "user@example.com", Password = "wrong", TenantId = user.TenantId };

        // Act
        var result = await controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithEmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        var controller = CreateAuthController(context);
        var request = new LoginRequest { Email = "", Password = "password123", TenantId = 1 };

        // Act
        var result = await controller.Login(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_WithValidRequest_CreatesTenantAndAdminUser()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        var controller = CreateAuthController(context);
        var request = new RegisterRequest
        {
            TenantName = "New Tenant",
            Email = "owner@example.com",
            Password = "password123"
        };

        // Act
        var result = await controller.Register(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        context.Tenants.Should().ContainSingle(t => t.Name == "New Tenant");
        context.Users.IgnoreQueryFilters().Should().ContainSingle(u =>
            u.Email == "owner@example.com" && u.Role == "Admin");
        context.RefreshTokens.IgnoreQueryFilters().Should().ContainSingle();
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var controller = CreateAuthController(context);
        var request = new RegisterRequest
        {
            TenantName = "New Tenant",
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var result = await controller.Register(request);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_RotatesToken()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var user = context.Users.IgnoreQueryFilters().Single();
        user.Email = "user@example.com";
        user.PasswordHash = PasswordHasher.HashPassword("password123");
        await context.SaveChangesAsync();

        var controller = CreateAuthController(context);
        var loginResult = await controller.Login(new LoginRequest
        {
            Email = "user@example.com",
            Password = "password123",
            TenantId = user.TenantId
        });
        var loginResponse = (AuthResponse)((OkObjectResult)loginResult).Value!;

        // Act
        var refreshResult = await controller.Refresh(new RefreshTokenRequest
        {
            RefreshToken = loginResponse.RefreshToken
        });

        // Assert
        refreshResult.Should().BeOfType<OkObjectResult>();
        var refreshResponse = (AuthResponse)((OkObjectResult)refreshResult).Value!;
        refreshResponse.Token.Should().NotBeNullOrWhiteSpace();
        refreshResponse.RefreshToken.Should().NotBe(loginResponse.RefreshToken);

        var refreshTokens = context.RefreshTokens.IgnoreQueryFilters().ToList();
        refreshTokens.Should().HaveCount(2);
        refreshTokens.Should().ContainSingle(rt => rt.RevokedAt != null && rt.ReplacedByTokenHash != null);
        refreshTokens.Should().ContainSingle(rt => rt.RevokedAt == null);
    }

    [Fact]
    public async Task Refresh_WithInvalidRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        var controller = CreateAuthController(context);

        // Act
        var result = await controller.Refresh(new RefreshTokenRequest { RefreshToken = "invalid" });

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Revoke_WithValidRefreshToken_RevokesToken()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContextWithData();
        var user = context.Users.IgnoreQueryFilters().Single();
        user.Email = "user@example.com";
        user.PasswordHash = PasswordHasher.HashPassword("password123");
        await context.SaveChangesAsync();

        var controller = CreateAuthController(context);
        var loginResult = await controller.Login(new LoginRequest
        {
            Email = "user@example.com",
            Password = "password123",
            TenantId = user.TenantId
        });
        var loginResponse = (AuthResponse)((OkObjectResult)loginResult).Value!;

        // Act
        var result = await controller.Revoke(new RefreshTokenRequest
        {
            RefreshToken = loginResponse.RefreshToken
        });

        // Assert
        result.Should().BeOfType<NoContentResult>();
        context.RefreshTokens.IgnoreQueryFilters().Single().RevokedAt.Should().NotBeNull();
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-key-that-is-at-least-32-characters-long",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:ExpiryMinutes"] = "60",
                ["JwtSettings:RefreshTokenExpiryDays"] = "7"
            })
            .Build();
    }

    private static AuthController CreateAuthController(ApplicationDbContext context)
    {
        return new AuthController(new AuthService(CreateConfiguration(), context));
    }
}
