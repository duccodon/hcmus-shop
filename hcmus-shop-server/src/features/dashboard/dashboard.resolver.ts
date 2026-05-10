import { Context, requireAdmin } from "../../common/context";
import { dashboardService } from "./dashboard.service";

export const dashboardResolver = {
  Query: {
    dashboardStats: (_: unknown, __: unknown, context: Context) => {
      requireAdmin(context);
      return dashboardService.getStats();
    },
  },
};
