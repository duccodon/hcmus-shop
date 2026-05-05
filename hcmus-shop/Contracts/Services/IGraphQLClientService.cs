using hcmus_shop.Models.Common;
using System;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface IGraphQLClientService
    {
        Task<T> QueryAsync<T>(string query, object? variables = null);
        Task<T> MutateAsync<T>(string query, object? variables = null);
        Task<Result<T>> SafeExecuteAsync<T>(Func<Task<T>> action);
        void SetAuthToken(string? token);
        void SetServerUrl(string url);
        string ServerUrl { get; }
    }
}
