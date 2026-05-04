namespace hcmus_shop.GraphQL.Operations
{
    public static class DashboardQueries
    {
        public const string GetStats = @"
            query DashboardStats {
                dashboardStats {
                    totalProducts
                    totalOrdersToday
                    totalRevenueToday
                    lowStockProducts {
                        productId
                        name
                        sku
                        stockQuantity
                    }
                    topSellingProducts {
                        productId
                        name
                        sku
                        totalSold
                        totalRevenue
                    }
                    recentOrders {
                        orderId
                        customerName
                        finalAmount
                        status
                        createdAt
                    }
                    dailyRevenue {
                        date
                        revenue
                    }
                }
            }";
    }
}
