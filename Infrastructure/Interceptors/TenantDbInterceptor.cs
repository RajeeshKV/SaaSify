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
            // Don't try to execute commands during connection opening
            // This will be handled in ConnectionOpenedAsync
            return result;
        }

        public override InterceptionResult ConnectionOpening(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result)
        {
            // Don't try to execute commands during connection opening
            // This will be handled in ConnectionOpened
            return result;
        }

        public override async Task ConnectionOpenedAsync(
            DbConnection connection,
            ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (_tenantContext.TenantId > 0)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SET app.tenant_id = {_tenantContext.TenantId}";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public override void ConnectionOpened(
            DbConnection connection,
            ConnectionEndEventData eventData)
        {
            if (_tenantContext.TenantId > 0)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SET app.tenant_id = {_tenantContext.TenantId}";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
