using hcmus_shop.Models.Common;
using hcmus_shop.Services.Products.Dto;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface IProductImportService
    {
        Task<Result<ProductImportSummary>> ImportAsync(string filePath);
    }
}
