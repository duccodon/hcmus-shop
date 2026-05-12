import { Prisma, Promotion } from "@prisma/client";
import {
  CreatePromotionDto,
  PromotionFilterDto,
  PromotionValidationResultDto,
  UpdatePromotionDto,
} from "./promotion.dto";
import { promotionRepository } from "./promotion.repository";

type PromotionInputPayload = {
  code?: string;
  discountPercent?: number | null;
  discountAmount?: number | null;
  minimumCustomerRank?: string | null;
  startDate?: string;
  endDate?: string;
  isActive?: boolean;
};

const CUSTOMER_RANKS = ["Bronze", "Silver", "Gold", "Diamond"] as const;
type CustomerRank = (typeof CUSTOMER_RANKS)[number];

export class PromotionService {
  findAll(filter: PromotionFilterDto) {
    return promotionRepository.findAll(filter);
  }

  findById(promotionId: number) {
    return promotionRepository.findById(promotionId);
  }

  async create(dto: CreatePromotionDto) {
    console.log("[PromotionService] createPromotion input", {
      code: dto.code,
      hasPercent: dto.discountPercent != null,
      hasAmount: dto.discountAmount != null,
      startDate: dto.startDate,
      endDate: dto.endDate,
      isActive: dto.isActive ?? true,
    });

    try {
      const input = await this.buildCreateInput(dto);
      const promotion = await promotionRepository.create(input);
      console.log("[PromotionService] createPromotion success", {
        promotionId: promotion.promotionId,
        code: promotion.code,
      });
      return promotion;
    } catch (error) {
      console.error("[PromotionService] createPromotion failed", error);
      throw error;
    }
  }

  async update(promotionId: number, dto: UpdatePromotionDto) {
    console.log("[PromotionService] updatePromotion input", {
      promotionId,
      code: dto.code,
      hasPercent: dto.discountPercent != null,
      hasAmount: dto.discountAmount != null,
      startDate: dto.startDate,
      endDate: dto.endDate,
      isActive: dto.isActive,
    });

    try {
      const existing = await this.requirePromotion(promotionId);
      const input = await this.buildUpdateInput(dto, existing);
      const promotion = await promotionRepository.update(promotionId, input);
      console.log("[PromotionService] updatePromotion success", {
        promotionId: promotion.promotionId,
        code: promotion.code,
      });
      return promotion;
    } catch (error) {
      console.error("[PromotionService] updatePromotion failed", { promotionId, error });
      throw error;
    }
  }

  async delete(promotionId: number) {
    console.log("[PromotionService] deletePromotion input", { promotionId });

    try {
      const promotion = await promotionRepository.softDelete(promotionId);
      console.log("[PromotionService] deletePromotion success", {
        promotionId: promotion.promotionId,
        code: promotion.code,
      });
      return promotion;
    } catch (error) {
      console.error("[PromotionService] deletePromotion failed", { promotionId, error });
      throw error;
    }
  }

  async validatePromotion(
    code: string,
    customerRank?: string | null
  ): Promise<PromotionValidationResultDto> {
    try {
      const promotion = await this.getValidPromotionByCode(code, new Date(), customerRank);
      if (!promotion) {
        return {
          isValid: false,
          message: "Promotion code is invalid, inactive, or expired.",
          promotion: null,
        };
      }

      return {
        isValid: true,
        message: "Promotion is valid.",
        promotion,
      };
    } catch (error) {
      return {
        isValid: false,
        message:
          error instanceof Error ? error.message : "Failed to validate promotion.",
        promotion: null,
      };
    }
  }

  async getValidPromotionByCode(
    code: string,
    now = new Date(),
    customerRank?: string | null
  ) {
    const normalizedCode = code.trim();
    if (!normalizedCode) {
      throw new Error("Promotion code is required.");
    }

    const promotion = await promotionRepository.findByCode(normalizedCode);
    if (!promotion) {
      return null;
    }

    if (!promotion.isActive) {
      return null;
    }

    if (now < promotion.startDate) {
      return null;
    }

    if (now > promotion.endDate) {
      return null;
    }

    if (!this.isPromotionAccessibleToRank(promotion.minimumCustomerRank, customerRank)) {
      return null;
    }

    return promotion;
  }

  calculateDiscount(subtotal: number, promotion: Promotion) {
    const safeSubtotal = Number.isFinite(subtotal) ? Math.max(0, subtotal) : 0;
    const percent = promotion.discountPercent
      ? Number(promotion.discountPercent)
      : null;
    const amount = promotion.discountAmount
      ? Number(promotion.discountAmount)
      : null;

    const discountAmount = percent != null
      ? (safeSubtotal * percent) / 100
      : amount ?? 0;

    return {
      subtotal: safeSubtotal,
      discountAmount: Math.min(discountAmount, safeSubtotal),
      finalAmount: Math.max(safeSubtotal - discountAmount, 0),
    };
  }

  private async requirePromotion(promotionId: number) {
    const existing = await promotionRepository.findById(promotionId);
    if (!existing) {
      throw new Error("Promotion not found.");
    }

    return existing;
  }

  private async buildCreateInput(
    dto: CreatePromotionDto
  ): Promise<Prisma.PromotionCreateInput> {
    const normalized = await this.normalizeAndValidateInput(dto);
    return {
      code: normalized.code,
      startDate: normalized.startDate,
      endDate: normalized.endDate,
      isActive: normalized.isActive,
      ...(normalized.discountPercent != null
        ? { discountPercent: normalized.discountPercent }
        : {}),
      ...(normalized.discountAmount != null
        ? { discountAmount: normalized.discountAmount }
        : {}),
      minimumCustomerRank: normalized.minimumCustomerRank,
    } as Prisma.PromotionCreateInput;
  }

  private async buildUpdateInput(
    dto: UpdatePromotionDto,
    existing: Promotion
  ): Promise<Prisma.PromotionUpdateInput> {
    const normalized = await this.normalizeAndValidateInput(dto, existing);
    return {
      code: normalized.code,
      startDate: normalized.startDate,
      endDate: normalized.endDate,
      isActive: normalized.isActive,
      discountPercent: normalized.discountPercent,
      discountAmount: normalized.discountAmount,
      minimumCustomerRank: normalized.minimumCustomerRank,
    } as Prisma.PromotionUpdateInput;
  }

  private async normalizeAndValidateInput(
    dto: PromotionInputPayload,
    existing?: Promotion
  ) {
    const code = dto.code?.trim() ?? existing?.code ?? "";
    if (!code) {
      throw new Error("Promotion code is required.");
    }

    const isSameCodeAsExisting =
      existing != null &&
      code.localeCompare(existing.code, undefined, { sensitivity: "accent" }) === 0;

    if (!isSameCodeAsExisting) {
      const isUniqueCode = await promotionRepository.ensureUniqueCode(
        code,
        existing?.promotionId
      );
      if (!isUniqueCode) {
        throw new Error("Promotion code already exists.");
      }
    }

    const startDate = dto.startDate
      ? this.parseDate(dto.startDate, "startDate")
      : existing?.startDate;
    const endDate = dto.endDate
      ? this.parseDate(dto.endDate, "endDate")
      : existing?.endDate;

    if (!startDate || !endDate) {
      throw new Error("Start date and end date are required.");
    }

    if (startDate > endDate) {
      throw new Error("Start date must be before or equal to end date.");
    }

    const resolvedPercent =
      dto.discountPercent !== undefined
        ? dto.discountPercent
        : existing?.discountPercent != null
          ? Number(existing.discountPercent)
          : null;
    const resolvedAmount =
      dto.discountAmount !== undefined
        ? dto.discountAmount
        : existing?.discountAmount != null
          ? Number(existing.discountAmount)
          : null;

    const normalizedDiscount = this.normalizeDiscountValues(
      resolvedPercent ?? null,
      resolvedAmount ?? null
    );

    return {
      code,
      discountPercent: normalizedDiscount.discountPercent,
      discountAmount: normalizedDiscount.discountAmount,
      minimumCustomerRank: this.normalizeCustomerRank(
        dto.minimumCustomerRank !== undefined
          ? dto.minimumCustomerRank
          : existing?.minimumCustomerRank ?? null
      ),
      startDate,
      endDate,
      isActive: dto.isActive ?? existing?.isActive ?? true,
    };
  }

  private normalizeDiscountValues(
    rawPercent: number | null,
    rawAmount: number | null
  ) {
    const percent = rawPercent != null && Number.isFinite(rawPercent)
      ? rawPercent
      : null;
    const amount = rawAmount != null && Number.isFinite(rawAmount)
      ? rawAmount
      : null;

    const hasPercent = percent != null && percent > 0;
    const hasAmount = amount != null && amount > 0;

    if (hasPercent === hasAmount) {
      throw new Error("Provide either discount percent or discount amount.");
    }

    if (hasPercent) {
      if (percent! > 100) {
        throw new Error("Discount percent must be between 0 and 100.");
      }

      return {
        discountPercent: new Prisma.Decimal(percent!),
        discountAmount: null,
      };
    }

    return {
      discountPercent: null,
      discountAmount: new Prisma.Decimal(amount!),
    };
  }

  private parseDate(value: string, fieldName: string) {
    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
      throw new Error(`${fieldName} is invalid.`);
    }

    return parsed;
  }

  private normalizeCustomerRank(value: string | null): CustomerRank | null {
    const trimmed = value?.trim();
    if (!trimmed) {
      return null;
    }

    const match = CUSTOMER_RANKS.find(
      (rank) => rank.localeCompare(trimmed, undefined, { sensitivity: "accent" }) === 0
    );

    if (!match) {
      throw new Error("minimumCustomerRank must be Bronze, Silver, Gold, or Diamond.");
    }

    return match;
  }

  private isPromotionAccessibleToRank(
    requiredRank: string | null,
    currentRank?: string | null
  ) {
    if (!requiredRank) {
      return true;
    }

    if (!currentRank?.trim()) {
      return false;
    }

    const normalizedRequiredRank = this.normalizeCustomerRank(requiredRank);
    const normalizedCurrentRank = this.normalizeCustomerRank(currentRank ?? null);

    return CUSTOMER_RANKS.indexOf(normalizedCurrentRank!) >= CUSTOMER_RANKS.indexOf(normalizedRequiredRank!);
  }
}

export const promotionService = new PromotionService();
