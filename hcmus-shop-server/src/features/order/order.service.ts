import { Prisma } from "@prisma/client";
import { requireAuth, Context } from "../../common/context";
import { prisma } from "../../prisma";
import { getCustomerRank } from "../customer/customer.service";
import { promotionService } from "../promotion/promotion.service";
import {
  CreateOrderDto,
  OrderFilterDto,
  OrderItemInputDto,
  ProductInstanceFilterDto,
  UpdateOrderDto,
} from "./order.dto";
import { orderInclude, orderRepository, productInstanceInclude } from "./order.repository";

const ORDER_STATUS_CREATED = "Created";
const ORDER_STATUS_PAID = "Paid";
const ORDER_STATUS_CANCELLED = "Cancelled";
const VALID_ORDER_STATUSES = new Set([
  ORDER_STATUS_CREATED,
  ORDER_STATUS_PAID,
  ORDER_STATUS_CANCELLED,
]);

export class OrderService {
  findAll(filter: OrderFilterDto) {
    return orderRepository.findAll(filter);
  }

  findById(orderId: string) {
    return orderRepository.findById(orderId);
  }

  findAvailableProductInstances(filter: ProductInstanceFilterDto) {
    return orderRepository.findAvailableProductInstances(filter);
  }

  findProductInstances(productId: number) {
    return orderRepository.findInstancesByProduct(productId);
  }

  async create(input: CreateOrderDto, context: Context) {
    const user = requireAuth(context);
    const payload = await this.prepareOrderPayload(input);

    return prisma.order.create({
      data: {
        customer: { connect: { customerId: payload.customerId } },
        user: { connect: { userId: user.userId } },
        promotion:
          payload.promotionId != null
            ? { connect: { promotionId: payload.promotionId } }
            : undefined,
        subtotal: new Prisma.Decimal(payload.subtotal),
        discountAmount: new Prisma.Decimal(payload.discountAmount),
        finalAmount: new Prisma.Decimal(payload.finalAmount),
        status: ORDER_STATUS_CREATED,
        notes: payload.notes,
        orderItems: {
          create: payload.items.map((item) => ({
            instance: { connect: { instanceId: item.instanceId } },
            unitSalePrice: new Prisma.Decimal(item.unitSalePrice),
            quantity: item.quantity,
          })),
        },
      },
      include: orderInclude,
    });
  }

  async update(orderId: string, input: UpdateOrderDto) {
    const existing = await prisma.order.findUnique({
      where: { orderId },
      include: {
        orderItems: true,
      },
    });

    if (!existing) {
      throw new Error("Order not found.");
    }

    if (existing.status !== ORDER_STATUS_CREATED) {
      throw new Error("Only orders in Created status can be updated.");
    }

    const payload = await this.prepareOrderPayload({
      customerId: input.customerId ?? existing.customerId,
      promotionCode: input.promotionCode ?? undefined,
      items:
        input.items && input.items.length > 0
          ? input.items
          : existing.orderItems.map((item) => ({
              instanceId: item.instanceId,
              quantity: item.quantity,
            })),
      notes: input.notes ?? existing.notes ?? undefined,
    });

    return prisma.$transaction(async (tx) => {
      await tx.orderItem.deleteMany({ where: { orderId } });

      await tx.order.update({
        where: { orderId },
        data: {
          customer: { connect: { customerId: payload.customerId } },
          promotion:
            payload.promotionId != null
              ? { connect: { promotionId: payload.promotionId } }
              : { disconnect: true },
          subtotal: new Prisma.Decimal(payload.subtotal),
          discountAmount: new Prisma.Decimal(payload.discountAmount),
          finalAmount: new Prisma.Decimal(payload.finalAmount),
          notes: payload.notes,
          orderItems: {
            create: payload.items.map((item) => ({
              instance: { connect: { instanceId: item.instanceId } },
              unitSalePrice: new Prisma.Decimal(item.unitSalePrice),
              quantity: item.quantity,
            })),
          },
        },
      });

      return tx.order.findUniqueOrThrow({
        where: { orderId },
        include: orderInclude,
      });
    });
  }

  async updateStatus(orderId: string, status: string, context: Context) {
    const normalizedStatus = status.trim();
    if (!VALID_ORDER_STATUSES.has(normalizedStatus)) {
      throw new Error("Unsupported order status.");
    }

    const user = requireAuth(context);
    const order = await prisma.order.findUnique({
      where: { orderId },
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

    if (!order) {
      throw new Error("Order not found.");
    }

    if (order.status === normalizedStatus) {
      return prisma.order.findUniqueOrThrow({
        where: { orderId },
        include: orderInclude,
      });
    }

    if (order.status !== ORDER_STATUS_CREATED) {
      throw new Error("Only orders in Created status can change status.");
    }

    if (normalizedStatus === ORDER_STATUS_CANCELLED) {
      return prisma.order.update({
        where: { orderId },
        data: { status: ORDER_STATUS_CANCELLED },
        include: orderInclude,
      });
    }

    if (normalizedStatus !== ORDER_STATUS_PAID) {
      throw new Error("Orders can only move from Created to Paid or Cancelled.");
    }

    return prisma.$transaction(async (tx) => {
      const freshOrder = await tx.order.findUnique({
        where: { orderId },
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

      if (!freshOrder) {
        throw new Error("Order not found.");
      }

      if (freshOrder.status !== ORDER_STATUS_CREATED) {
        throw new Error("Only orders in Created status can be paid.");
      }

      const invalidInstance = freshOrder.orderItems.find(
        (item) => item.instance.status !== "Available"
      );
      if (invalidInstance) {
        throw new Error(
          `Serial ${invalidInstance.instance.serialNumber} is no longer available.`
        );
      }

      const affectedProductIds = new Set<number>();
      for (const item of freshOrder.orderItems) {
        affectedProductIds.add(item.instance.productId);
        await tx.productInstance.update({
          where: { instanceId: item.instanceId },
          data: { status: "Sold" },
        });

        await tx.inventoryLog.create({
          data: {
            product: { connect: { productId: item.instance.productId } },
            instance: { connect: { instanceId: item.instanceId } },
            user: { connect: { userId: user.userId } },
            quantityChange: -item.quantity,
            changeType: "Export",
            reason: `Order ${freshOrder.orderId} marked as Paid`,
          },
        });
      }

      for (const productId of affectedProductIds) {
        const availableCount = await tx.productInstance.count({
          where: {
            productId,
            status: "Available",
          },
        });

        await tx.product.update({
          where: { productId },
          data: { stockQuantity: availableCount },
        });
      }

      await tx.order.update({
        where: { orderId },
        data: { status: ORDER_STATUS_PAID },
      });

      const earnedPoints = calculateEarnedPoints(Number(freshOrder.finalAmount));
      if (earnedPoints > 0) {
        await tx.customer.update({
          where: { customerId: freshOrder.customerId },
          data: {
            loyaltyPoints: {
              increment: earnedPoints,
            },
          },
        });
      }

      return tx.order.findUniqueOrThrow({
        where: { orderId },
        include: orderInclude,
      });
    });
  }

  async delete(orderId: string) {
    const order = await prisma.order.findUnique({
      where: { orderId },
      select: { orderId: true, status: true },
    });

    if (!order) {
      throw new Error("Order not found.");
    }

    if (order.status !== ORDER_STATUS_CREATED) {
      throw new Error("Only orders in Created status can be deleted.");
    }

    await prisma.$transaction(async (tx) => {
      await tx.orderItem.deleteMany({ where: { orderId } });
      await tx.order.delete({ where: { orderId } });
    });

    return true;
  }

  private async prepareOrderPayload(input: CreateOrderDto) {
    const customerId = input.customerId?.trim();
    if (!customerId) {
      throw new Error("Customer is required.");
    }

    const customer = await prisma.customer.findUnique({
      where: { customerId },
      select: { customerId: true, loyaltyPoints: true },
    });
    if (!customer) {
      throw new Error("Customer not found.");
    }

    const items = this.normalizeItems(input.items);
    const instances = await orderRepository.findInstancesByIds(
      items.map((item) => item.instanceId)
    );
    if (instances.length !== items.length) {
      throw new Error("One or more product serials do not exist.");
    }

    const instanceMap = new Map(instances.map((instance) => [instance.instanceId, instance]));
    for (const item of items) {
      const instance = instanceMap.get(item.instanceId);
      if (!instance) {
        throw new Error("One or more product serials do not exist.");
      }
      if (instance.status !== "Available") {
        throw new Error(`Serial ${instance.serialNumber} is not available.`);
      }
    }

    const resolvedItems = items.map((item) => {
      const instance = instanceMap.get(item.instanceId)!;
      return {
        instanceId: item.instanceId,
        quantity: item.quantity,
        unitSalePrice: Number(instance.product.sellingPrice),
      };
    });

    const subtotal = resolvedItems.reduce(
      (sum, item) => sum + item.unitSalePrice * item.quantity,
      0
    );

    let promotionId: number | null = null;
    let discountAmount = 0;
    let finalAmount = subtotal;

    const normalizedCode = input.promotionCode?.trim();
    if (normalizedCode) {
      const promotion = await promotionService.getValidPromotionByCode(
        normalizedCode,
        new Date(),
        getCustomerRank(customer.loyaltyPoints)
      );
      if (!promotion) {
        throw new Error("Promotion code is invalid, inactive, or expired.");
      }

      const calculated = promotionService.calculateDiscount(subtotal, promotion);
      promotionId = promotion.promotionId;
      discountAmount = calculated.discountAmount;
      finalAmount = calculated.finalAmount;
    }

    return {
      customerId,
      promotionId,
      notes: normalizeOptionalText(input.notes),
      subtotal,
      discountAmount,
      finalAmount,
      items: resolvedItems,
    };
  }

  private normalizeItems(items: OrderItemInputDto[]) {
    if (!items?.length) {
      throw new Error("At least one order item is required.");
    }

    const duplicateCheck = new Set<number>();
    return items.map((item) => {
      if (!Number.isInteger(item.instanceId) || item.instanceId <= 0) {
        throw new Error("Order item instance is invalid.");
      }
      if (!Number.isInteger(item.quantity) || item.quantity <= 0) {
        throw new Error("Order item quantity is invalid.");
      }
      if (item.quantity !== 1) {
        throw new Error("Serial-based sales only support quantity = 1 per item.");
      }
      if (duplicateCheck.has(item.instanceId)) {
        throw new Error("Duplicate product serials are not allowed in the same order.");
      }

      duplicateCheck.add(item.instanceId);
      return {
        instanceId: item.instanceId,
        quantity: item.quantity,
      };
    });
  }
}

function normalizeOptionalText(value?: string | null) {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}

function calculateEarnedPoints(finalAmount: number) {
  if (!Number.isFinite(finalAmount) || finalAmount <= 0) {
    return 0;
  }

  return Math.floor(finalAmount / 100);
}

export const orderService = new OrderService();
