import { Prisma } from "@prisma/client";
import {
  CreatePromotionDto,
  PromotionFilterDto,
  UpdatePromotionDto,
} from "./promotion.dto";
import { promotionService } from "./promotion.service";

export const promotionResolver = {
  Promotion: {
    discountPercent: (parent: { discountPercent: Prisma.Decimal | null }) =>
      parent.discountPercent != null ? Number(parent.discountPercent) : null,
    discountAmount: (parent: { discountAmount: Prisma.Decimal | null }) =>
      parent.discountAmount != null ? Number(parent.discountAmount) : null,
  },

  Query: {
    promotions: (_: unknown, args: PromotionFilterDto) =>
      promotionService.findAll(args),
    promotion: (_: unknown, { promotionId }: { promotionId: number }) =>
      promotionService.findById(promotionId),
    validatePromotion: (_: unknown, { code }: { code: string }) =>
      promotionService.validatePromotion(code),
  },

  Mutation: {
    createPromotion: (_: unknown, { input }: { input: CreatePromotionDto }) =>
      promotionService.create(input),
    updatePromotion: (
      _: unknown,
      { promotionId, input }: { promotionId: number; input: UpdatePromotionDto }
    ) => promotionService.update(promotionId, input),
    deletePromotion: (_: unknown, { promotionId }: { promotionId: number }) =>
      promotionService.delete(promotionId),
  },
};
