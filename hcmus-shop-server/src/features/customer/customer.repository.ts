import { Prisma } from "@prisma/client";
import { prisma } from "../../prisma";
import { CustomerFilterDto } from "./customer.dto";

const customerOrderBy: Prisma.CustomerOrderByWithRelationInput[] = [
  { createdAt: "desc" },
];

export class CustomerRepository {
  async findAll(filter: CustomerFilterDto) {
    const page = Math.max(1, filter.page ?? 1);
    const pageSize = Math.max(1, Math.min(filter.pageSize ?? 10, 100));
    const skip = (page - 1) * pageSize;

    const where: Prisma.CustomerWhereInput = {};
    if (filter.search?.trim()) {
      const search = filter.search.trim();
      where.OR = [
        { name: { contains: search, mode: "insensitive" } },
        { phone: { contains: search, mode: "insensitive" } },
        { email: { contains: search, mode: "insensitive" } },
      ];
    }

    const [items, totalCount] = await Promise.all([
      prisma.customer.findMany({
        where,
        orderBy: customerOrderBy,
        skip,
        take: pageSize,
      }),
      prisma.customer.count({ where }),
    ]);

    return { items, totalCount, page, pageSize };
  }

  findById(customerId: string) {
    return prisma.customer.findUnique({ where: { customerId } });
  }

  create(data: Prisma.CustomerCreateInput) {
    return prisma.customer.create({ data });
  }

  update(customerId: string, data: Prisma.CustomerUpdateInput) {
    return prisma.customer.update({ where: { customerId }, data });
  }

  async delete(customerId: string) {
    await prisma.customer.delete({ where: { customerId } });
    return true;
  }

  countOrders(customerId: string) {
    return prisma.order.count({ where: { customerId } });
  }
}

export const customerRepository = new CustomerRepository();
