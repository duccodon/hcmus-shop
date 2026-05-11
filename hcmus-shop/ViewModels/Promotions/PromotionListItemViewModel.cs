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
            string? minimumCustomerRank,
            DateTime startDate,
            DateTime endDate,
            bool isActive)
        {
            PromotionId = promotionId;
            Code = code;
            DiscountPercent = discountPercent;
            DiscountAmount = discountAmount;
            MinimumCustomerRank = minimumCustomerRank;
            StartDate = startDate;
            EndDate = endDate;
            IsActive = isActive;
        }

        public int PromotionId { get; }
        public string Code { get; }
        public double? DiscountPercent { get; }
        public double? DiscountAmount { get; }
        public string? MinimumCustomerRank { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public bool IsActive { get; }

        public string DiscountDisplay =>
            DiscountPercent.HasValue
                ? $"{DiscountPercent.Value:0.##}%"
                : DiscountAmount.HasValue
                    ? $"{DiscountAmount.Value:N0} VND"
                    : "N/A";

        public string StartDateDisplay => StartDate.ToString("dd/MM/yyyy HH:mm");
        public string EndDateDisplay => EndDate.ToString("dd/MM/yyyy HH:mm");
        public string DateRangeDisplay => $"{StartDate:dd/MM/yyyy HH:mm} - {EndDate:dd/MM/yyyy HH:mm}";
        public string EligibilityDisplay => string.IsNullOrWhiteSpace(MinimumCustomerRank) ? "All ranks" : $"{MinimumCustomerRank}+";
        public string StatusText => IsActive ? "Active" : "Inactive";
    }
}
