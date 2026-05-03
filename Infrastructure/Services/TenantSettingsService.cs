using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class TenantSettingsService : ITenantSettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionService _subscriptionService;

        public TenantSettingsService(ApplicationDbContext context, ISubscriptionService subscriptionService)
        {
            _context = context;
            _subscriptionService = subscriptionService;
        }

        public async Task<TenantSettingsDto> GetTenantSettingsAsync(int tenantId)
        {
            var settings = await _context.TenantSettings
                .FirstOrDefaultAsync(ts => ts.TenantId == tenantId);

            if (settings == null)
            {
                // Create default settings based on subscription plan
                var subscription = await _subscriptionService.GetCurrentSubscriptionAsync(tenantId);
                settings = await CreateDefaultSettingsAsync(tenantId, subscription?.Plan ?? "Free");
            }

            return new TenantSettingsDto(
                settings.TenantId,
                settings.MaxProjects,
                settings.MaxUsers,
                settings.EnableAdvancedFeatures,
                settings.EnableApiAccess,
                settings.EnableExport,
                settings.EnableIntegrations,
                settings.MaxStorageMB,
                settings.MaxApiCallsPerDay,
                settings.UpdatedAt,
                settings.CreatedAt
            );
        }

        public async Task<TenantSettingsDto> UpdateTenantSettingsAsync(int tenantId, UpdateTenantSettingsDto settings)
        {
            var existingSettings = await _context.TenantSettings
                .FirstOrDefaultAsync(ts => ts.TenantId == tenantId);

            if (existingSettings == null)
            {
                existingSettings = new TenantSettings
                {
                    TenantId = tenantId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.TenantSettings.Add(existingSettings);
            }

            // Update settings
            existingSettings.MaxProjects = settings.MaxProjects;
            existingSettings.MaxUsers = settings.MaxUsers;
            existingSettings.EnableAdvancedFeatures = settings.EnableAdvancedFeatures;
            existingSettings.EnableApiAccess = settings.EnableApiAccess;
            existingSettings.EnableExport = settings.EnableExport;
            existingSettings.EnableIntegrations = settings.EnableIntegrations;
            existingSettings.MaxStorageMB = settings.MaxStorageMB;
            existingSettings.MaxApiCallsPerDay = settings.MaxApiCallsPerDay;
            existingSettings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new TenantSettingsDto(
                existingSettings.TenantId,
                existingSettings.MaxProjects,
                existingSettings.MaxUsers,
                existingSettings.EnableAdvancedFeatures,
                existingSettings.EnableApiAccess,
                existingSettings.EnableExport,
                existingSettings.EnableIntegrations,
                existingSettings.MaxStorageMB,
                existingSettings.MaxApiCallsPerDay,
                existingSettings.UpdatedAt,
                existingSettings.CreatedAt
            );
        }

        public async Task<bool> HasFeatureEnabledAsync(int tenantId, string feature)
        {
            var settings = await GetTenantSettingsAsync(tenantId);
            
            return feature.ToLower() switch
            {
                "advanced" => settings.EnableAdvancedFeatures,
                "api" => settings.EnableApiAccess,
                "export" => settings.EnableExport,
                "integrations" => settings.EnableIntegrations,
                _ => false
            };
        }

        public async Task<bool> IsWithinLimitAsync(int tenantId, string resource, int currentUsage)
        {
            var settings = await GetTenantSettingsAsync(tenantId);
            
            return resource.ToLower() switch
            {
                "projects" => currentUsage < settings.MaxProjects,
                "users" => currentUsage < settings.MaxUsers,
                "storage" => currentUsage < settings.MaxStorageMB,
                "api_calls" => currentUsage < settings.MaxApiCallsPerDay,
                _ => true
            };
        }

        private async Task<TenantSettings> CreateDefaultSettingsAsync(int tenantId, string plan)
        {
            var defaultSettings = plan.ToLower() switch
            {
                "free" => new TenantSettings
                {
                    TenantId = tenantId,
                    MaxProjects = 5,
                    MaxUsers = 3,
                    EnableAdvancedFeatures = false,
                    EnableApiAccess = false,
                    EnableExport = false,
                    EnableIntegrations = false,
                    MaxStorageMB = 100,
                    MaxApiCallsPerDay = 100,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                "professional" => new TenantSettings
                {
                    TenantId = tenantId,
                    MaxProjects = 50,
                    MaxUsers = 10,
                    EnableAdvancedFeatures = true,
                    EnableApiAccess = true,
                    EnableExport = true,
                    EnableIntegrations = false,
                    MaxStorageMB = 1000,
                    MaxApiCallsPerDay = 1000,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                "enterprise" => new TenantSettings
                {
                    TenantId = tenantId,
                    MaxProjects = -1, // Unlimited
                    MaxUsers = 50,
                    EnableAdvancedFeatures = true,
                    EnableApiAccess = true,
                    EnableExport = true,
                    EnableIntegrations = true,
                    MaxStorageMB = 10000,
                    MaxApiCallsPerDay = 5000,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                _ => throw new ArgumentException($"Unknown plan: {plan}")
            };

            _context.TenantSettings.Add(defaultSettings);
            await _context.SaveChangesAsync();
            
            return defaultSettings;
        }
    }
}
