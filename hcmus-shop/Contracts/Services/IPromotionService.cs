using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Promotions.Dto;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface IPromotionService
    {
        Task<Result<PromotionPageDto>> GetAllAsync(PromotionFilterDto filter);
        Task<Result<PromotionDto?>> GetByIdAsync(int promotionId);
        Task<Result<PromotionDto>> CreateAsync(CreatePromotionInput input);
        Task<Result<PromotionDto>> UpdateAsync(int promotionId, UpdatePromotionInput input);
        Task<Result<bool>> DeleteAsync(int promotionId);
        Task<Result<PromotionValidationDto>> ValidateAsync(string code);
    }
}
