import { Prisma } from "@prisma/client";
import { orderRepository } from "../src/features/order/order.repository";
import { orderService } from "../src/features/order/order.service";
import { promotionService } from "../src/features/promotion/promotion.service";
import { prisma } from "../src/prisma";

jest.mock("../src/prisma", () => ({
  prisma: {
    order: {
      create: jest.fn(),
      findUnique: jest.fn(),
      update: jest.fn(),
      findUniqueOrThrow: jest.fn(),
    },
    $transaction: jest.fn(),
  },
}));

jest.mock("../src/features/order/order.repository", () => ({
  orderInclude: {},
  productInstanceInclude: {},
  orderRepository: {
    findInstancesByIds: jest.fn(),
    findAvailableProductInstances: jest.fn(),
    findInstancesByProduct: jest.fn(),
    findAll: jest.fn(),
    findById: jest.fn(),
  },
}));

jest.mock("../src/features/promotion/promotion.service", () => ({
  promotionService: {
    getValidPromotionByCode: jest.fn(),
    calculateDiscount: jest.fn(),
  },
}));

const mockPrisma = prisma as unknown as {
  order: {
    create: jest.Mock;
    findUnique: jest.Mock;
    update: jest.Mock;
    findUniqueOrThrow: jest.Mock;
  };
  $transaction: jest.Mock;
};

const mockOrderRepository = orderRepository as unknown as jest.Mocked<
  Pick<
    typeof orderRepository,
    "findInstancesByIds" | "findAvailableProductInstances" | "findInstancesByProduct" | "findAll" | "findById"
  >
>;

const mockPromotionService = promotionService as unknown as jest.Mocked<
  Pick<typeof promotionService, "getValidPromotionByCode" | "calculateDiscount">
>;

const context = {
  user: {
    userId: "user-1",
    username: "sale",
    role: "Sale",
  },
};

function buildAvailableInstance(instanceId: number, productId: number) {
  return {
    instanceId,
    productId,
    serialNumber: `SKU-${instanceId}`,
    status: "Available",
    product: {
      productId,
      sellingPrice: new Prisma.Decimal(1000),
      importPrice: new Prisma.Decimal(700),
    },
  };
}

describe("OrderService transitions and walk-in support", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    jest.spyOn(console, "log").mockImplementation(() => undefined);
    jest.spyOn(console, "error").mockImplementation(() => undefined);
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it("creates a walk-in order without attaching a customer", async () => {
    mockOrderRepository.findInstancesByIds.mockResolvedValue([
      buildAvailableInstance(11, 99),
    ] as any);
    mockPrisma.order.create.mockResolvedValue({ orderId: "walk-in-order" });

    await orderService.create(
      {
        customerId: null,
        items: [{ instanceId: 11, quantity: 1 }],
        notes: "Walk-in sale",
      },
      context as any
    );

    expect(mockPrisma.order.create).toHaveBeenCalledWith(
      expect.objectContaining({
        data: expect.not.objectContaining({
          customer: expect.anything(),
        }),
      })
    );
  });

  it("allows Created to Cancelled transition", async () => {
    mockPrisma.order.findUnique.mockResolvedValue({
      orderId: "order-1",
      status: "Created",
      orderItems: [],
    });
    mockPrisma.order.update.mockResolvedValue({ orderId: "order-1", status: "Cancelled" });

    const result = await orderService.updateStatus("order-1", "Cancelled", context as any);

    expect(mockPrisma.order.update).toHaveBeenCalledWith(
      expect.objectContaining({
        where: { orderId: "order-1" },
        data: { status: "Cancelled" },
      })
    );
    expect(result).toMatchObject({ status: "Cancelled" });
  });

  it("blocks status changes once an order is Paid", async () => {
    mockPrisma.order.findUnique.mockResolvedValue({
      orderId: "order-2",
      status: "Paid",
      orderItems: [],
    });

    await expect(orderService.updateStatus("order-2", "Cancelled", context as any)).rejects.toThrow(
      "Only orders in Created status can change status."
    );
  });

  it("marks a Created order as Paid and increments loyalty points from final amount", async () => {
    const freshOrder = {
      orderId: "order-3",
      status: "Created",
      customerId: "customer-1",
      finalAmount: new Prisma.Decimal(35990000),
      orderItems: [
        {
          instanceId: 15,
          quantity: 1,
          instance: {
            serialNumber: "SKU-15",
            status: "Available",
            productId: 21,
            product: {
              productId: 21,
            },
          },
        },
      ],
    };

    mockPrisma.order.findUnique.mockResolvedValue(freshOrder);

    const tx = {
      order: {
        findUnique: jest.fn().mockResolvedValue(freshOrder),
        update: jest.fn().mockResolvedValue(undefined),
        findUniqueOrThrow: jest.fn().mockResolvedValue({ orderId: "order-3", status: "Paid" }),
      },
      productInstance: {
        update: jest.fn().mockResolvedValue(undefined),
        count: jest.fn().mockResolvedValue(3),
      },
      inventoryLog: {
        create: jest.fn().mockResolvedValue(undefined),
      },
      product: {
        update: jest.fn().mockResolvedValue(undefined),
      },
      customer: {
        update: jest.fn().mockResolvedValue(undefined),
      },
    };

    mockPrisma.$transaction.mockImplementation(async (callback: (tx: any) => unknown) => callback(tx));

    await orderService.updateStatus("order-3", "Paid", context as any);

    expect(tx.productInstance.update).toHaveBeenCalledWith(
      expect.objectContaining({
        where: { instanceId: 15 },
        data: { status: "Sold" },
      })
    );
    expect(tx.inventoryLog.create).toHaveBeenCalled();
    expect(tx.product.update).toHaveBeenCalledWith(
      expect.objectContaining({
        where: { productId: 21 },
        data: { stockQuantity: 3 },
      })
    );
    expect(tx.customer.update).toHaveBeenCalledWith(
      expect.objectContaining({
        where: { customerId: "customer-1" },
        data: {
          loyaltyPoints: {
            increment: 359900,
          },
        },
      })
    );
  });
});
