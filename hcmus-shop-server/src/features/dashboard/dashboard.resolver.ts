import { dashboardService } from "./dashboard.service";

export const dashboardResolver = {
  Query: {
    dashboardStats: () => dashboardService.getStats(),
  },
};
