import { dashboardRepository } from "./dashboard.repository";

export const dashboardService = {
  /**
   * Aggregate everything in parallel. Each piece is independent, so we
   * don't pay sequential round-trips. Total response time ≈ slowest query.
   */
  async getStats() {
    const [
      totalProducts,
      totalOrdersToday,
      totalRevenueToday,
      lowStockProducts,
      topSellingProducts,
      recentOrders,
      dailyRevenue,
    ] = await Promise.all([
      dashboardRepository.countActiveProducts(),
      dashboardRepository.countOrdersToday(),
      dashboardRepository.sumRevenueToday(),
      dashboardRepository.findLowStock(5),
      dashboardRepository.findTopSelling(5),
      dashboardRepository.findRecentOrders(3),
      dashboardRepository.findDailyRevenueThisMonth(),
    ]);

    return {
      totalProducts,
      totalOrdersToday,
      totalRevenueToday,
      lowStockProducts,
      topSellingProducts,
      recentOrders,
      dailyRevenue,
    };
  },
};
