using hcmus_shop.Models.DTOs;

namespace hcmus_shop.Services.Dashboard.Dto
{
    /// <summary>
    /// GraphQL response envelope for the `dashboardStats` query.
    /// The outer object key matches the GraphQL field name (camelCase
    /// `dashboardStats` deserializes to PascalCase `DashboardStats`).
    /// </summary>
    public class DashboardStatsResponse
    {
        public DashboardStatsDto DashboardStats { get; set; } = new();
    }
}
