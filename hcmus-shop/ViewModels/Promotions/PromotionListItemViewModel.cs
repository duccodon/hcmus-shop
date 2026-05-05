using System;

namespace hcmus_shop.ViewModels.Promotions
{
    public class PromotionListItemViewModel
    {
        public PromotionListItemViewModel(
            int promotionId,
            string code,
            double? discountPercent,
            double? discountAmount,
            DateTime startDate,
            DateTime endDate,
            bool isActive)
        {
            PromotionId = promotionId;
            Code = code;
            DiscountPercent = discountPercent;
            DiscountAmount = discountAmount;
            StartDate = startDate;
            EndDate = endDate;
            IsActive = isActive;
        }

        public int PromotionId { get; }
        public string Code { get; }
        public double? DiscountPercent { get; }
        public double? DiscountAmount { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public bool IsActive { get; }

        public string DiscountDisplay =>
            DiscountPercent.HasValue
                ? $"{DiscountPercent.Value:0.##}%"
                : DiscountAmount.HasValue
                    ? $"{DiscountAmount.Value:N0} VND"
                    : "N/A";

        public string DateRangeDisplay => $"{StartDate:dd/MM/yyyy} - {EndDate:dd/MM/yyyy}";
        public string StatusText => IsActive ? "Active" : "Inactive";
    }
}
