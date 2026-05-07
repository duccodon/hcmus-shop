using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface IDashboardService
    {
        Task<Result<DashboardStatsDto>> GetStatsAsync();
    }
}
