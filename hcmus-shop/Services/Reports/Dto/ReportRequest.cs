namespace hcmus_shop.Services.Reports.Dto
{
    public class SalesReportRequest
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string GroupBy { get; set; } = "day";
    }

    public class TopProductsRequest
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public int Limit { get; set; } = 5;
    }
}
