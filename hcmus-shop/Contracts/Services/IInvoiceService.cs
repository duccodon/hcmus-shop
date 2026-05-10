using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface IInvoiceService
    {
        Task<Result<string>> GenerateInvoicePdfAsync(OrderDto order, string outputPath);
    }
}
