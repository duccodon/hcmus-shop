using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.Common;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Health
{
    public class HealthService : IHealthService
    {
        private readonly IGraphQLClientService _graphQL;

        public HealthService(IGraphQLClientService graphQL)
        {
            _graphQL = graphQL;
        }

        public Task<Result<bool>> PingAsync() => PingAsync(_graphQL.ServerUrl);

        public async Task<Result<bool>> PingAsync(string serverUrl)
        {
            // Convert ".../graphql" to ".../health"
            var healthUrl = ToHealthUrl(serverUrl);

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                using var resp = await http.GetAsync(healthUrl);
                if (!resp.IsSuccessStatusCode)
                    return Result<bool>.Failure($"Server responded with {(int)resp.StatusCode}.");

                return Result<bool>.Success(true);
            }
            catch (TaskCanceledException)
            {
                return Result<bool>.Failure("Connection timed out.");
            }
            catch (HttpRequestException ex)
            {
                return Result<bool>.Failure($"Cannot reach server: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }

        private static string ToHealthUrl(string graphQlUrl)
        {
            const string suffix = "/graphql";
            if (graphQlUrl.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return graphQlUrl.Substring(0, graphQlUrl.Length - suffix.Length) + "/health";
            // Fallback: append /health to whatever was given
            return graphQlUrl.TrimEnd('/') + "/health";
        }
    }
}
