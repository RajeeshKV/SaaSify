using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/tenantsettings")]
[Authorize]
public class TenantSettingsController : ControllerBase
{
    private readonly ITenantSettingsService _tenantSettingsService;

    public TenantSettingsController(ITenantSettingsService tenantSettingsService)
    {
        _tenantSettingsService = tenantSettingsService;
    }

    [HttpGet]
    public async Task<ActionResult<TenantSettingsDto>> GetSettings()
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid tenant ID");
        }

        var settings = await _tenantSettingsService.GetTenantSettingsAsync(tenantId);
        return Ok(settings);
    }

    [HttpPut]
    [Authorize("tenant.admin")]
    public async Task<ActionResult<TenantSettingsDto>> UpdateSettings([FromBody] UpdateTenantSettingsDto settings)
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid tenant ID");
        }

        var updatedSettings = await _tenantSettingsService.UpdateTenantSettingsAsync(tenantId, settings);
        return Ok(updatedSettings);
    }

    [HttpGet("features/{feature}")]
    public async Task<ActionResult<bool>> HasFeatureEnabled(string feature)
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid tenant ID");
        }

        var hasFeature = await _tenantSettingsService.HasFeatureEnabledAsync(tenantId, feature);
        return Ok(hasFeature);
    }

    [HttpGet("limits/{resource}")]
    public async Task<ActionResult<bool>> IsWithinLimit(string resource, [FromQuery] int currentUsage)
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid tenant ID");
        }

        var withinLimit = await _tenantSettingsService.IsWithinLimitAsync(tenantId, resource, currentUsage);
        return Ok(withinLimit);
    }
}
