namespace hcmus_shop.Services.Auth
{
    public interface IFeatureFlagService
    {
        bool IsFeatureEnabledForRole(string? role, string featureName);
    }
}
