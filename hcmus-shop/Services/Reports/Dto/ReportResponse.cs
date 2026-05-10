using hcmus_shop.Models.DTOs;
using System.Collections.Generic;

namespace hcmus_shop.Services.Reports.Dto
{
    public class SalesReportResponse
    {
        public List<SalesReportEntryDto> SalesReport { get; set; } = new();
    }

    public class TopProductsResponse
    {
        public List<TopProductEntryDto> TopProducts { get; set; } = new();
    }
}
