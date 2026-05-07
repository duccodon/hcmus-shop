using hcmus_shop.Contracts.Services;
using Microsoft.Extensions.Configuration;
using Windows.Storage;

namespace hcmus_shop.Services.Config
{
    public class ConfigService : IConfigService
    {
        private const string ServerUrlKey = "config_server_url";
        private const string DefaultFallback = "http://localhost:4000/graphql";

        private readonly IConfiguration _configuration;

        public ConfigService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetServerUrl()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(ServerUrlKey, out var saved)
                && saved is string url
                && !string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
            return GetDefaultServerUrl();
        }

        public void SetServerUrl(string url)
        {
            ApplicationData.Current.LocalSettings.Values[ServerUrlKey] = url;
        }

        public string GetDefaultServerUrl()
        {
            return _configuration["GraphQL:Endpoint"] ?? DefaultFallback;
        }
    }
}
