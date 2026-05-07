namespace hcmus_shop.Services.Promotions.Dto
{
    public class PromotionFilterDto
    {
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class CreatePromotionInput
    {
        public string Code { get; set; } = string.Empty;
        public double? DiscountPercent { get; set; }
        public double? DiscountAmount { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public bool? IsActive { get; set; }
    }

    public class UpdatePromotionInput
    {
        public string? Code { get; set; }
        public double? DiscountPercent { get; set; }
        public double? DiscountAmount { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public bool? IsActive { get; set; }
    }
}
