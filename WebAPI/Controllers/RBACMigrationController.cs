using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Services;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RBACMigrationController : ControllerBase
{
    private readonly RBACMigrationService _migrationService;
    private readonly ILogger<RBACMigrationController> _logger;

    public RBACMigrationController(RBACMigrationService migrationService, ILogger<RBACMigrationController> logger)
    {
        _migrationService = migrationService;
        _logger = logger;
    }

    [HttpPost("migrate")]
    public async Task<ActionResult<MigrationResult>> MigrateExistingTenants()
    {
        try
        {
            var success = await _migrationService.MigrateExistingTenantsAsync();
            
            return Ok(new MigrationResult(
                success,
                success ? "RBAC migration completed successfully" : "RBAC migration failed",
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RBAC migration failed");
            return StatusCode(500, new MigrationResult(
                false,
                $"RBAC migration failed: {ex.Message}",
                DateTime.UtcNow
            ));
        }
    }

    [HttpGet("status")]
    public async Task<ActionResult<MigrationStatusResult>> GetMigrationStatus()
    {
        try
        {
            var status = await _migrationService.GetMigrationStatusAsync();
            
            return Ok(new MigrationStatusResult(
                status,
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get migration status");
            return StatusCode(500, new MigrationStatusResult(
                new List<string> { $"Error: {ex.Message}" },
                DateTime.UtcNow
            ));
        }
    }
}

public record MigrationResult(
    bool Success,
    string Message,
    DateTime Timestamp
);

public record MigrationStatusResult(
    List<string> Status,
    DateTime Timestamp
);
