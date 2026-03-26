using System;

namespace hcmus_shop.Models
{
    public class Promotion : BaseEntity
    {
        public int PromotionId { get; set; }
        public string Code { get; set; } = string.Empty;                // Unique
        public decimal DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
