using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace hcmus_shop.Services
{
    public class FeatureFlagService : IFeatureFlagService
    {
        private const string SectionName = "FeatureFlags";
        private readonly IConfiguration _configuration;

        public FeatureFlagService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsFeatureEnabledForRole(string? role, string featureName)
        {
            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(featureName))
            {
                return false;
            }

            var features = GetFeaturesForRole(role);
            return features.Contains(featureName);
        }

        private HashSet<string> GetFeaturesForRole(string role)
        {
            var key = GetRoleFeatureKey(role);
            var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            PopulateFeaturesFromSection(key, features);

            if (string.Equals(key, "SalesFeature", StringComparison.OrdinalIgnoreCase)
                && features.Count == 0)
            {
                PopulateFeaturesFromSection("SaleFeature", features);
            }

            return features;
        }

        private void PopulateFeaturesFromSection(string key, HashSet<string> features)
        {
            var section = _configuration.GetSection($"{SectionName}:{key}");
            foreach (var child in section.GetChildren())
            {
                if (!string.IsNullOrWhiteSpace(child.Value))
                {
                    features.Add(child.Value);
                }
            }
        }

        private string GetRoleFeatureKey(string role)
        {
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return "AdminFeature";
            }

            if (string.Equals(role, "Sales", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Sale", StringComparison.OrdinalIgnoreCase))
            {
                return "SalesFeature";
            }

            return $"{role}Feature";
        }
    }
}
