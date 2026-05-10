import { prisma } from "../../prisma";

type ReportGroupBy = "day" | "week" | "month" | "year";

export class ReportRepository {
  async getSalesReport(fromDate: Date, toDate: Date, groupBy: ReportGroupBy) {
    const orders = await prisma.order.findMany({
      where: {
        status: "Paid",
        createdAt: { gte: fromDate, lte: toDate },
      },
      include: {
        orderItems: {
          include: {
            instance: {
              include: {
                product: true,
              },
            },
          },
        },
      },
      orderBy: { createdAt: "asc" },
    });

    const grouped = new Map<
      string,
      { quantity: number; revenue: number; profit: number }
    >();

    for (const order of orders) {
      const period = getPeriodKey(order.createdAt, groupBy);
      const existing = grouped.get(period) ?? {
        quantity: 0,
        revenue: 0,
        profit: 0,
      };

      for (const item of order.orderItems) {
        const importPrice = Number(item.instance.product.importPrice);
        const unitSalePrice = Number(item.unitSalePrice);
        existing.quantity += item.quantity;
        existing.revenue += unitSalePrice * item.quantity;
        existing.profit += (unitSalePrice - importPrice) * item.quantity;
      }

      grouped.set(period, existing);
    }

    return [...grouped.entries()].map(([period, stats]) => ({
      period,
      totalQuantity: stats.quantity,
      totalRevenue: stats.revenue,
      totalProfit: stats.profit,
    }));
  }

  async getTopProducts(fromDate: Date, toDate: Date, limit = 5) {
    const orders = await prisma.order.findMany({
      where: {
        status: "Paid",
        createdAt: { gte: fromDate, lte: toDate },
      },
      include: {
        orderItems: {
          include: {
            instance: {
              include: {
                product: true,
              },
            },
          },
        },
      },
    });

    const grouped = new Map<
      number,
      { name: string; totalSold: number; totalRevenue: number }
    >();

    for (const order of orders) {
      for (const item of order.orderItems) {
        const product = item.instance.product;
        const existing = grouped.get(product.productId) ?? {
          name: product.name,
          totalSold: 0,
          totalRevenue: 0,
        };

        existing.totalSold += item.quantity;
        existing.totalRevenue += Number(item.unitSalePrice) * item.quantity;
        grouped.set(product.productId, existing);
      }
    }

    return [...grouped.entries()]
      .map(([productId, stats]) => ({
        productId,
        name: stats.name,
        totalSold: stats.totalSold,
        totalRevenue: stats.totalRevenue,
      }))
      .sort((left, right) => right.totalSold - left.totalSold)
      .slice(0, Math.max(1, limit));
  }
}

function getPeriodKey(date: Date, groupBy: ReportGroupBy) {
  const utcDate = new Date(date);
  if (groupBy === "day") {
    return utcDate.toISOString().slice(0, 10);
  }

  if (groupBy === "week") {
    return `${utcDate.getUTCFullYear()}-W${String(getIsoWeekNumber(utcDate)).padStart(2, "0")}`;
  }

  if (groupBy === "month") {
    return `${utcDate.getUTCFullYear()}-${String(utcDate.getUTCMonth() + 1).padStart(2, "0")}`;
  }

  return String(utcDate.getUTCFullYear());
}

function getIsoWeekNumber(date: Date) {
  const target = new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()));
  const dayNumber = target.getUTCDay() || 7;
  target.setUTCDate(target.getUTCDate() + 4 - dayNumber);
  const yearStart = new Date(Date.UTC(target.getUTCFullYear(), 0, 1));
  return Math.ceil((((target.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
}

export const reportRepository = new ReportRepository();
