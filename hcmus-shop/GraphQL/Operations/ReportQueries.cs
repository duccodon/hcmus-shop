namespace hcmus_shop.GraphQL.Operations
{
    public static class ReportQueries
    {
        public const string SalesReport = @"
            query SalesReport($fromDate: String!, $toDate: String!, $groupBy: String!) {
                salesReport(fromDate: $fromDate, toDate: $toDate, groupBy: $groupBy) {
                    period
                    totalQuantity
                    totalRevenue
                    totalProfit
                }
            }";

        public const string TopProducts = @"
            query TopProducts($fromDate: String!, $toDate: String!, $limit: Int) {
                topProducts(fromDate: $fromDate, toDate: $toDate, limit: $limit) {
                    productId
                    name
                    totalSold
                    totalRevenue
                }
            }";
    }
}
