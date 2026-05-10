using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Reports.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface IReportService
    {
        Task<Result<List<SalesReportEntryDto>>> GetSalesReportAsync(SalesReportRequest request);
        Task<Result<List<TopProductEntryDto>>> GetTopProductsAsync(TopProductsRequest request);
    }
}
