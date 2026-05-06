import { Prisma, Promotion } from "@prisma/client";
import { prisma } from "../../prisma";
import { PromotionFilterDto } from "./promotion.dto";

export class PromotionRepository {
  async findAll(filter: PromotionFilterDto) {
    const page = filter.page ?? 1;
    const pageSize = filter.pageSize ?? 10;
    const skip = (page - 1) * pageSize;

    const where: Prisma.PromotionWhereInput = {};

    if (filter.search) {
      where.code = { contains: filter.search, mode: "insensitive" };
    }

    const [items, totalCount] = await Promise.all([
      prisma.promotion.findMany({
        where,
        orderBy: [{ startDate: "desc" }, { promotionId: "desc" }],
        skip,
        take: pageSize,
      }),
      prisma.promotion.count({ where }),
    ]);

    return { items, totalCount, page, pageSize };
  }

  findById(promotionId: number) {
    return prisma.promotion.findUnique({ where: { promotionId } });
  }

  findByCode(code: string) {
    return prisma.promotion.findUnique({
      where: { code },
    });
  }

  create(data: Prisma.PromotionCreateInput) {
    return prisma.promotion.create({ data });
  }

  update(promotionId: number, data: Prisma.PromotionUpdateInput) {
    return prisma.promotion.update({
      where: { promotionId },
      data,
    });
  }

  softDelete(promotionId: number) {
    return prisma.promotion.update({
      where: { promotionId },
      data: { isActive: false },
    });
  }

  async ensureUniqueCode(code: string, excludePromotionId?: number) {
    const existing = await prisma.promotion.findFirst({
      where: {
        code: { equals: code, mode: "insensitive" },
      },
    });

    if (!existing) {
      return true;
    }

    return excludePromotionId !== existing.promotionId;
  }
}

export const promotionRepository = new PromotionRepository();
