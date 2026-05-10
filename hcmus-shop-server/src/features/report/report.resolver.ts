import { Context, requireAdmin } from "../../common/context";
import { reportService } from "./report.service";

export const reportResolver = {
  Query: {
    salesReport: (
      _: unknown,
      {
        fromDate,
        toDate,
        groupBy,
      }: { fromDate: string; toDate: string; groupBy: string },
      context: Context
    ) => {
      requireAdmin(context);
      return reportService.getSalesReport(fromDate, toDate, groupBy);
    },
    topProducts: (
      _: unknown,
      {
        fromDate,
        toDate,
        limit,
      }: { fromDate: string; toDate: string; limit?: number },
      context: Context
    ) => {
      requireAdmin(context);
      return reportService.getTopProducts(fromDate, toDate, limit);
    },
  },
};
