namespace hcmus_shop.Contracts.Services
{
    /// <summary>
    /// Pre-login configuration that determines how to reach the server.
    /// Distinct from ISettingsService (post-login user preferences).
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// Returns the saved server URL or the appsettings default.
        /// </summary>
        string GetServerUrl();

        /// <summary>
        /// Persists the server URL to LocalSettings.
        /// </summary>
        void SetServerUrl(string url);

        /// <summary>
        /// Default URL from appsettings.json (used if nothing saved).
        /// </summary>
        string GetDefaultServerUrl();
    }
}
