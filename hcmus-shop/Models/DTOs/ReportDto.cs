using System.Collections.Generic;

namespace hcmus_shop.Models.DTOs
{
    public class SalesReportEntryDto
    {
        public string Period { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public double TotalRevenue { get; set; }
        public double TotalProfit { get; set; }
    }

    public class TopProductEntryDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public double TotalRevenue { get; set; }
    }
}
