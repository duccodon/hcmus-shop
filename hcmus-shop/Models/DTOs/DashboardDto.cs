using System.Collections.Generic;

namespace hcmus_shop.Models.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalProducts { get; set; }
        public int TotalOrdersToday { get; set; }
        public double TotalRevenueToday { get; set; }
        public List<LowStockProductDto> LowStockProducts { get; set; } = new();
        public List<TopSellingProductDto> TopSellingProducts { get; set; } = new();
        public List<RecentOrderDto> RecentOrders { get; set; } = new();
        public List<DailyRevenuePointDto> DailyRevenue { get; set; } = new();
    }

    public class LowStockProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }

    public class TopSellingProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public double TotalRevenue { get; set; }
    }

    public class RecentOrderDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public double FinalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class DailyRevenuePointDto
    {
        public string Date { get; set; } = string.Empty;
        public double Revenue { get; set; }
    }
}
