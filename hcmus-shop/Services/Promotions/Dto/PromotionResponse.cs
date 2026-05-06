using hcmus_shop.Models.DTOs;

namespace hcmus_shop.Services.Promotions.Dto
{
    public class PromotionsResponse
    {
        public PromotionPageDto Promotions { get; set; } = new();
    }

    public class PromotionResponse
    {
        public PromotionDto? Promotion { get; set; }
    }

    public class CreatePromotionResponse
    {
        public PromotionDto CreatePromotion { get; set; } = new();
    }

    public class UpdatePromotionResponse
    {
        public PromotionDto UpdatePromotion { get; set; } = new();
    }

    public class DeletePromotionResponse
    {
        public PromotionDto DeletePromotion { get; set; } = new();
    }

    public class ValidatePromotionResponse
    {
        public PromotionValidationDto ValidatePromotion { get; set; } = new();
    }
}
