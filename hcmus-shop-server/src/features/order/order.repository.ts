import { Prisma } from "@prisma/client";
import { prisma } from "../../prisma";
import { OrderFilterDto, ProductInstanceFilterDto } from "./order.dto";

export const orderInclude = {
  customer: true,
  user: true,
  promotion: true,
  orderItems: {
    include: {
      instance: {
        include: {
          product: true,
        },
      },
    },
  },
} satisfies Prisma.OrderInclude;

export const productInstanceInclude = {
  product: true,
} satisfies Prisma.ProductInstanceInclude;

export class OrderRepository {
  async findAll(filter: OrderFilterDto) {
    const page = Math.max(1, filter.page ?? 1);
    const pageSize = Math.max(1, Math.min(filter.pageSize ?? 10, 500));
    const skip = (page - 1) * pageSize;

    const where = buildOrderWhere(filter);

    const [items, totalCount] = await Promise.all([
      prisma.order.findMany({
        where,
        orderBy: { createdAt: "desc" },
        skip,
        take: pageSize,
        include: orderInclude,
      }),
      prisma.order.count({ where }),
    ]);

    return { items, totalCount, page, pageSize };
  }

  findById(orderId: string) {
    return prisma.order.findUnique({
      where: { orderId },
      include: orderInclude,
    });
  }

  async findAvailableProductInstances(filter: ProductInstanceFilterDto) {
    const page = Math.max(1, filter.page ?? 1);
    const pageSize = Math.max(1, Math.min(filter.pageSize ?? 10, 100));
    const skip = (page - 1) * pageSize;

    const where: Prisma.ProductInstanceWhereInput = {
      status: "Available",
      ...(filter.productId ? { productId: filter.productId } : {}),
    };

    if (filter.search?.trim()) {
      const search = filter.search.trim();
      where.OR = [
        { serialNumber: { contains: search, mode: "insensitive" } },
        { product: { name: { contains: search, mode: "insensitive" } } },
        { product: { sku: { contains: search, mode: "insensitive" } } },
      ];
    }

    const [items, totalCount] = await Promise.all([
      prisma.productInstance.findMany({
        where,
        orderBy: [{ productId: "asc" }, { serialNumber: "asc" }],
        skip,
        take: pageSize,
        include: productInstanceInclude,
      }),
      prisma.productInstance.count({ where }),
    ]);

    return { items, totalCount, page, pageSize };
  }

  findInstancesByIds(instanceIds: number[]) {
    return prisma.productInstance.findMany({
      where: { instanceId: { in: instanceIds } },
      include: productInstanceInclude,
    });
  }

  findInstancesByProduct(productId: number) {
    return prisma.productInstance.findMany({
      where: { productId },
      orderBy: { serialNumber: "asc" },
      include: productInstanceInclude,
    });
  }
}

function buildOrderWhere(filter: OrderFilterDto): Prisma.OrderWhereInput {
  const where: Prisma.OrderWhereInput = {};

  if (filter.status?.trim()) {
    where.status = filter.status.trim();
  }

  const createdAt: Prisma.DateTimeFilter = {};
  if (filter.fromDate?.trim()) {
    createdAt.gte = parseDate(filter.fromDate, "fromDate");
  }
  if (filter.toDate?.trim()) {
    const toDate = parseDate(filter.toDate, "toDate");
    toDate.setHours(23, 59, 59, 999);
    createdAt.lte = toDate;
  }
  if (createdAt.gte || createdAt.lte) {
    where.createdAt = createdAt;
  }

  if (filter.search?.trim()) {
    const search = filter.search.trim();
    const orFilters: Prisma.OrderWhereInput[] = [
      { customer: { name: { contains: search, mode: "insensitive" } } },
      { customer: { phone: { contains: search, mode: "insensitive" } } },
      {
        orderItems: {
          some: {
            instance: {
              serialNumber: { contains: search, mode: "insensitive" },
            },
          },
        },
      },
    ];

    if (isUuidLike(search)) {
      orFilters.unshift({ orderId: search });
    }

    where.OR = orFilters;
  }

  return where;
}

function parseDate(value: string, fieldName: string) {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    throw new Error(`${fieldName} is invalid.`);
  }
  return parsed;
}

function isUuidLike(value: string) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
    value
  );
}

export const orderRepository = new OrderRepository();
