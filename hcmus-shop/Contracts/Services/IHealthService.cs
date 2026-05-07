using hcmus_shop.Models.Common;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    /// <summary>
    /// Pre-flight liveness probe. Used to verify the server is reachable
    /// BEFORE the user tries any GraphQL operation. Calls GET /health.
    /// </summary>
    public interface IHealthService
    {
        /// <summary>
        /// Pings the currently-configured server URL.
        /// Result.IsSuccess = true when the server responded with 200 and a healthy payload.
        /// Result.Error contains a human-readable reason on failure.
        /// </summary>
        Task<Result<bool>> PingAsync();

        /// <summary>
        /// Pings a specific URL (used by ConfigPage to test a URL before saving).
        /// </summary>
        Task<Result<bool>> PingAsync(string serverUrl);
    }
}
