using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public interface IExternalApiClient
    {
        Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default) where T : class;
        Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;
        Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;
        Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
    }
}
