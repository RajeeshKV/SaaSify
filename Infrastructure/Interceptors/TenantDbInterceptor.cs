using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace Infrastructure.Interceptors
{
    public class TenantDbInterceptor : DbConnectionInterceptor
    {
        private readonly ITenantContext _tenantContext;

        public TenantDbInterceptor(ITenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            if (_tenantContext.TenantId > 0)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SET app.tenant_id = {_tenantContext.TenantId}";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            return result;
        }

        public override InterceptionResult ConnectionOpening(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result)
        {
            if (_tenantContext.TenantId > 0)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SET app.tenant_id = {_tenantContext.TenantId}";
                cmd.ExecuteNonQuery();
            }

            return result;
        }
    }
}
