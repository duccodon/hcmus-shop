import { reportRepository } from "./report.repository";

const VALID_GROUPS = new Set(["day", "week", "month", "year"]);

export class ReportService {
  async getSalesReport(fromDate: string, toDate: string, groupBy: string) {
    const parsed = this.normalizeInput(fromDate, toDate, groupBy);
    return reportRepository.getSalesReport(parsed.fromDate, parsed.toDate, parsed.groupBy);
  }

  async getTopProducts(fromDate: string, toDate: string, limit?: number) {
    const parsed = this.normalizeInput(fromDate, toDate, "day");
    return reportRepository.getTopProducts(
      parsed.fromDate,
      parsed.toDate,
      limit ?? 5
    );
  }

  private normalizeInput(fromDate: string, toDate: string, groupBy: string) {
    const parsedFrom = parseDate(fromDate, "fromDate");
    const parsedTo = parseDate(toDate, "toDate");
    parsedTo.setHours(23, 59, 59, 999);

    if (parsedFrom > parsedTo) {
      throw new Error("fromDate must be before or equal to toDate.");
    }

    if (!VALID_GROUPS.has(groupBy)) {
      throw new Error("groupBy must be day, week, month, or year.");
    }

    return {
      fromDate: parsedFrom,
      toDate: parsedTo,
      groupBy: groupBy as "day" | "week" | "month" | "year",
    };
  }
}

function parseDate(value: string, fieldName: string) {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    throw new Error(`${fieldName} is invalid.`);
  }
  return parsed;
}

export const reportService = new ReportService();
