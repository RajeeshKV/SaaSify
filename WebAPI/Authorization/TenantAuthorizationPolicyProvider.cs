using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace WebAPI.Authorization
{
    public class TenantAuthorizationPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

        public TenantAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            // Handle tenant-specific permission policies
            if (policyName.StartsWith("tenant.", StringComparison.OrdinalIgnoreCase))
            {
                var policy = new AuthorizationPolicyBuilder();
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("TenantId"); // Ensure user has tenant claim
                policy.RequireClaim("permission", policyName.Substring(7)); // Extract permission after "tenant."
                return Task.FromResult(policy.Build());
            }

            // Fall back to the default provider for other policies
            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return _fallbackPolicyProvider.GetDefaultPolicyAsync();
        }

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
        {
            return _fallbackPolicyProvider.GetFallbackPolicyAsync();
        }
    }
}
