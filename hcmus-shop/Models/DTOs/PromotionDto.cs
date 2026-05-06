using System.Collections.Generic;

namespace hcmus_shop.Models.DTOs
{
    public class PromotionDto
    {
        public int PromotionId { get; set; }
        public string Code { get; set; } = string.Empty;
        public double? DiscountPercent { get; set; }
        public double? DiscountAmount { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? UpdatedAt { get; set; }
    }

    public class PromotionPageDto
    {
        public List<PromotionDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class PromotionValidationDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public PromotionDto? Promotion { get; set; }
    }
}
