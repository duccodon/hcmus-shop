namespace hcmus_shop.ViewModels.Products
{
    public class PageButtonItem
    {
        public string Label { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsCurrent { get; set; }
    }
}
