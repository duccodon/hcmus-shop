import { prisma } from "../../prisma";
import { Prisma } from "@prisma/client";

const LOW_STOCK_THRESHOLD = 5;

/**
 * Repository for dashboard aggregate queries. Each method is a single
 * Prisma query designed to be parallelizable. The service combines the
 * results into a single GraphQL response.
 *
 * IMPORTANT: this module deliberately does NOT depend on Dev C's Order
 * feature module. It queries Prisma directly, so the dashboard works
 * even before that module exists (returns empty arrays / zero counts).
 */
export const dashboardRepository = {
  countActiveProducts(): Promise<number> {
    return prisma.product.count({ where: { isActive: true } });
  },

  countOrdersToday(): Promise<number> {
    const start = startOfToday();
    const end = endOfToday();
    return prisma.order.count({
      where: { createdAt: { gte: start, lte: end } },
    });
  },

  async sumRevenueToday(): Promise<number> {
    const start = startOfToday();
    const end = endOfToday();
    const r = await prisma.order.aggregate({
      where: {
        status: "Paid",
        createdAt: { gte: start, lte: end },
      },
      _sum: { finalAmount: true },
    });
    return Number(r._sum.finalAmount ?? 0);
  },

  findLowStock(limit = 5) {
    return prisma.product.findMany({
      where: { isActive: true, stockQuantity: { lt: LOW_STOCK_THRESHOLD } },
      orderBy: { stockQuantity: "asc" },
      take: limit,
      select: { productId: true, name: true, sku: true, stockQuantity: true },
    });
  },

  /**
   * Top selling = sum of OrderItem.quantity grouped by Product.
   * Only counts items from Paid orders.
   */
  async findTopSelling(limit = 5) {
    const grouped = await prisma.orderItem.groupBy({
      by: ["instanceId"],
      where: { order: { status: "Paid" } },
      _sum: { quantity: true, unitSalePrice: true },
      orderBy: { _sum: { quantity: "desc" } },
      take: limit * 4, // overfetch since multiple instances may map to same product
    });

    if (grouped.length === 0) return [];

    // Resolve productId for each instance
    const instances = await prisma.productInstance.findMany({
      where: { instanceId: { in: grouped.map((g) => g.instanceId) } },
      select: { instanceId: true, productId: true },
    });
    const instanceToProduct = new Map(
      instances.map((i) => [i.instanceId, i.productId])
    );

    // Aggregate by product
    const byProduct = new Map<number, { qty: number; revenue: number }>();
    for (const g of grouped) {
      const productId = instanceToProduct.get(g.instanceId);
      if (productId == null) continue;
      const existing = byProduct.get(productId) ?? { qty: 0, revenue: 0 };
      existing.qty += g._sum.quantity ?? 0;
      existing.revenue += Number(g._sum.unitSalePrice ?? 0);
      byProduct.set(productId, existing);
    }

    const sortedIds = [...byProduct.entries()]
      .sort((a, b) => b[1].qty - a[1].qty)
      .slice(0, limit);

    if (sortedIds.length === 0) return [];

    const products = await prisma.product.findMany({
      where: { productId: { in: sortedIds.map(([id]) => id) } },
      select: { productId: true, name: true, sku: true },
    });
    const productMap = new Map(products.map((p) => [p.productId, p]));

    return sortedIds
      .map(([id, agg]) => {
        const p = productMap.get(id);
        if (!p) return null;
        return {
          productId: p.productId,
          name: p.name,
          sku: p.sku,
          totalSold: agg.qty,
          totalRevenue: agg.revenue,
        };
      })
      .filter((x): x is NonNullable<typeof x> => x !== null);
  },

  async findRecentOrders(limit = 3) {
    const orders = await prisma.order.findMany({
      orderBy: { createdAt: "desc" },
      take: limit,
      select: {
        orderId: true,
        finalAmount: true,
        status: true,
        createdAt: true,
        customer: { select: { name: true } },
      },
    });
    return orders.map((o) => ({
      orderId: o.orderId,
      customerName: o.customer?.name ?? null,
      finalAmount: Number(o.finalAmount),
      status: o.status,
      createdAt: o.createdAt.toISOString(),
    }));
  },

  /**
   * Returns daily revenue points for the current month.
   * Days with zero revenue are still included as zero (chart-friendly).
   */
  async findDailyRevenueThisMonth() {
    const now = new Date();
    const monthStart = new Date(now.getFullYear(), now.getMonth(), 1);
    const monthEnd = new Date(now.getFullYear(), now.getMonth() + 1, 1);

    const orders = await prisma.order.findMany({
      where: {
        status: "Paid",
        createdAt: { gte: monthStart, lt: monthEnd },
      },
      select: { createdAt: true, finalAmount: true },
    });

    // Bucket by yyyy-mm-dd
    const byDay = new Map<string, number>();
    for (const o of orders) {
      const key = o.createdAt.toISOString().slice(0, 10);
      byDay.set(key, (byDay.get(key) ?? 0) + Number(o.finalAmount));
    }

    // Build full month with zero days
    const result: { date: string; revenue: number }[] = [];
    const cursor = new Date(monthStart);
    while (cursor < monthEnd) {
      const key = cursor.toISOString().slice(0, 10);
      result.push({ date: key, revenue: byDay.get(key) ?? 0 });
      cursor.setDate(cursor.getDate() + 1);
    }
    return result;
  },
};

function startOfToday() {
  const d = new Date();
  d.setHours(0, 0, 0, 0);
  return d;
}

function endOfToday() {
  const d = new Date();
  d.setHours(23, 59, 59, 999);
  return d;
}
