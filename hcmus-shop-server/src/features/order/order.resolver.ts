import { Prisma } from "@prisma/client";
import { Context, requireAdmin } from "../../common/context";
import {
  CreateOrderDto,
  OrderFilterDto,
  ProductInstanceFilterDto,
  UpdateOrderDto,
} from "./order.dto";
import { orderService } from "./order.service";

export const orderResolver = {
  Product: {
    instances: (parent: { productId: number }) =>
      orderService.findProductInstances(parent.productId),
  },

  Order: {
    subtotal: (parent: { subtotal: Prisma.Decimal }) => Number(parent.subtotal),
    discountAmount: (parent: { discountAmount: Prisma.Decimal }) =>
      Number(parent.discountAmount),
    finalAmount: (parent: { finalAmount: Prisma.Decimal }) =>
      Number(parent.finalAmount),
  },

  OrderItem: {
    unitSalePrice: (parent: { unitSalePrice: Prisma.Decimal }) =>
      Number(parent.unitSalePrice),
  },

  Query: {
    orders: (_: unknown, args: OrderFilterDto) => orderService.findAll(args),
    order: (_: unknown, { orderId }: { orderId: string }) =>
      orderService.findById(orderId),
    availableProductInstances: (_: unknown, args: ProductInstanceFilterDto) =>
      orderService.findAvailableProductInstances(args),
  },

  Mutation: {
    createOrder: (
      _: unknown,
      { input }: { input: CreateOrderDto },
      context: Context
    ) => orderService.create(input, context),
    updateOrder: (
      _: unknown,
      { orderId, input }: { orderId: string; input: UpdateOrderDto }
    ) => orderService.update(orderId, input),
    updateOrderStatus: (
      _: unknown,
      { orderId, status }: { orderId: string; status: string },
      context: Context
    ) => orderService.updateStatus(orderId, status, context),
    deleteOrder: (
      _: unknown,
      { orderId }: { orderId: string },
      context: Context
    ) => {
      requireAdmin(context);
      return orderService.delete(orderId);
    },
  },
};
