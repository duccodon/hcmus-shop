import { Prisma, Promotion } from "@prisma/client";
import { promotionRepository } from "../src/features/promotion/promotion.repository";
import { promotionService } from "../src/features/promotion/promotion.service";

jest.mock("../src/features/promotion/promotion.repository", () => ({
  promotionRepository: {
    findByCode: jest.fn(),
    findById: jest.fn(),
    ensureUniqueCode: jest.fn(),
    create: jest.fn(),
    update: jest.fn(),
  },
}));

const mockPromotionRepository = promotionRepository as unknown as jest.Mocked<
  Pick<typeof promotionRepository, "findByCode" | "findById" | "ensureUniqueCode" | "create" | "update">
>;

function promotionFixture(overrides: Partial<Promotion> = {}): Promotion {
  const now = new Date("2026-05-01T00:00:00.000Z");

  return {
    promotionId: 1,
    code: "PROMO",
    discountPercent: null,
    discountAmount: null,
    minimumCustomerRank: null,
    startDate: new Date("2026-01-01T00:00:00.000Z"),
    endDate: new Date("2026-12-31T23:59:59.999Z"),
    isActive: true,
    createdAt: now,
    updatedAt: now,
    ...overrides,
  } as Promotion;
}

describe("PromotionService Dev B logic", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    jest.spyOn(console, "log").mockImplementation(() => undefined);
    jest.spyOn(console, "error").mockImplementation(() => undefined);
    mockPromotionRepository.ensureUniqueCode.mockResolvedValue(true);
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it("calculates percent discount", () => {
    const result = promotionService.calculateDiscount(
      200,
      promotionFixture({ discountPercent: new Prisma.Decimal(10) })
    );

    expect(result).toEqual({
      subtotal: 200,
      discountAmount: 20,
      finalAmount: 180,
    });
  });

  it("calculates fixed discount", () => {
    const result = promotionService.calculateDiscount(
      200,
      promotionFixture({ discountAmount: new Prisma.Decimal(35) })
    );

    expect(result).toEqual({
      subtotal: 200,
      discountAmount: 35,
      finalAmount: 165,
    });
  });

  it("caps discount at subtotal", () => {
    const result = promotionService.calculateDiscount(
      50,
      promotionFixture({ discountAmount: new Prisma.Decimal(200) })
    );

    expect(result).toEqual({
      subtotal: 50,
      discountAmount: 50,
      finalAmount: 0,
    });
  });

  it("rejects invalid discount config where both percent and amount are supplied", async () => {
    await expect(
      promotionService.create({
        code: "BOTH",
        discountPercent: 10,
        discountAmount: 20,
        startDate: "2026-05-01",
        endDate: "2026-05-31",
      })
    ).rejects.toThrow("Provide either discount percent or discount amount.");

    expect(mockPromotionRepository.create).not.toHaveBeenCalled();
  });

  it("rejects invalid discount config where neither percent nor amount is supplied", async () => {
    await expect(
      promotionService.create({
        code: "NEITHER",
        startDate: "2026-05-01",
        endDate: "2026-05-31",
      })
    ).rejects.toThrow("Provide either discount percent or discount amount.");

    expect(mockPromotionRepository.create).not.toHaveBeenCalled();
  });

  it("enforces Bronze/Silver/Gold/Diamond rank order", async () => {
    mockPromotionRepository.findByCode.mockResolvedValue(
      promotionFixture({ code: "GOLDONLY", minimumCustomerRank: "Gold" })
    );

    await expect(promotionService.validatePromotion("GOLDONLY", "Bronze")).resolves.toMatchObject({
      isValid: false,
    });
    await expect(promotionService.validatePromotion("GOLDONLY", "Silver")).resolves.toMatchObject({
      isValid: false,
    });
    await expect(promotionService.validatePromotion("GOLDONLY", "Gold")).resolves.toMatchObject({
      isValid: true,
    });
    await expect(promotionService.validatePromotion("GOLDONLY", "Diamond")).resolves.toMatchObject({
      isValid: true,
    });
  });

  it("trims promotion code before validation lookup", async () => {
    mockPromotionRepository.findByCode.mockResolvedValue(
      promotionFixture({ code: "WELCOME10", discountPercent: new Prisma.Decimal(10) })
    );

    await expect(promotionService.validatePromotion(" welcome10 ")).resolves.toMatchObject({
      isValid: true,
    });
    expect(mockPromotionRepository.findByCode).toHaveBeenCalledWith("welcome10");
  });

  it("clears minimum customer rank when update receives explicit null", async () => {
    const existing = promotionFixture({
      discountPercent: new Prisma.Decimal(10),
      minimumCustomerRank: "Gold",
    });
    const updated = promotionFixture({
      discountPercent: new Prisma.Decimal(10),
      minimumCustomerRank: null,
    });
    mockPromotionRepository.findById.mockResolvedValue(existing);
    mockPromotionRepository.update.mockResolvedValue(updated);

    await expect(
      promotionService.update(existing.promotionId, { minimumCustomerRank: null })
    ).resolves.toMatchObject({ minimumCustomerRank: null });

    expect(mockPromotionRepository.update).toHaveBeenCalledWith(
      existing.promotionId,
      expect.objectContaining({ minimumCustomerRank: null })
    );
  });
});
