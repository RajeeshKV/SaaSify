using Application.Common.Configuration;

namespace Application.Common.Interfaces;

public interface ITenantSettingsService
{
    Task<TenantSettingsDto> GetTenantSettingsAsync(int tenantId);
    Task<TenantSettingsDto> UpdateTenantSettingsAsync(int tenantId, UpdateTenantSettingsDto settings);
    Task<bool> HasFeatureEnabledAsync(int tenantId, string feature);
    Task<bool> IsWithinLimitAsync(int tenantId, string resource, int currentUsage);
}

public record TenantSettingsDto(
    int TenantId,
    int MaxProjects,
    int MaxUsers,
    bool EnableAdvancedFeatures,
    bool EnableApiAccess,
    bool EnableExport,
    bool EnableIntegrations,
    long MaxStorageMB,
    int MaxApiCallsPerDay,
    DateTime UpdatedAt,
    DateTime CreatedAt
);

public record UpdateTenantSettingsDto(
    int MaxProjects,
    int MaxUsers,
    bool EnableAdvancedFeatures,
    bool EnableApiAccess,
    bool EnableExport,
    bool EnableIntegrations,
    long MaxStorageMB,
    int MaxApiCallsPerDay
);
