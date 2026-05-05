namespace hcmus_shop.Contracts.Services
{
    public interface IFeatureFlagService
    {
        bool IsFeatureEnabledForRole(string? role, string featureName);
    }
}
