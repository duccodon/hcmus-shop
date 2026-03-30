namespace hcmus_shop.Services
{
    public interface IFeatureFlagService
    {
        bool IsFeatureEnabledForRole(string? role, string featureName);
    }
}
