namespace hcmus_shop.Contracts.Services
{
    /// <summary>
    /// Post-login user preferences. Distinct from IConfigService (server connection).
    /// All values persist to LocalSettings (per-user, per-machine).
    /// </summary>
    public interface ISettingsService
    {
        int PageSize { get; set; }
        bool RememberLastScreen { get; set; }
        string? LastScreen { get; set; }
    }
}
