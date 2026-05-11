using System;
using System.Linq;

namespace hcmus_shop.ViewModels.Common
{
    public static class UserErrorMessageFormatter
    {
        public static string Format(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            var normalized = message.Trim();
            if (Contains(normalized, "promotions.minimumCustomerRank"))
            {
                return "Promotion data is out of date. Apply the latest database migration, then refresh.";
            }

            if (Contains(normalized, "Invalid `prisma.") || Contains(normalized, "does not exist in the current database"))
            {
                return "Database schema is out of date. Apply the latest migration, then refresh.";
            }

            normalized = RemovePrefix(normalized, "GraphQL:");
            normalized = RemovePrefix(normalized, "Network:");
            normalized = RemovePrefix(normalized, "Unexpected:");

            if (Contains(normalized, "Cannot connect to server"))
            {
                return "Cannot connect to the server. Check that the backend is running and the Config server URL is correct.";
            }

            return normalized
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))
                ?? string.Empty;
        }

        private static bool Contains(string value, string pattern) =>
            value.Contains(pattern, StringComparison.OrdinalIgnoreCase);

        private static string RemovePrefix(string value, string prefix) =>
            value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? value[prefix.Length..].Trim()
                : value;
    }
}
